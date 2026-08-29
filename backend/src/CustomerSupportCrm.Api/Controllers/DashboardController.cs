using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

// Story 17: a narrow, staff-readable aggregation endpoint for the Home dashboard.
// GET /api/tickets and GET /api/customers are both unfiltered/unpaginated - composing
// dashboard KPIs from them client-side would over-fetch the entire table just to show a
// handful of numbers. This endpoint does one server-side aggregation pass instead.
// RequireStaff (no Admin restriction): every staff user needs their own "My Work" section,
// not just Admins. ReportsController itself is untouched - this is a distinct, smaller
// surface for Home, not a replacement for /reports/*.
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "RequireStaff")]
public class DashboardController : ControllerBase
{
    private sealed record TicketSlaProjection(
        Guid Id, string Subject, string Status, string Priority, DateTime CreatedAtUtc,
        DateTime? FirstRespondedAtUtc, DateTime? ResolvedAtUtc, Guid? AssignedToUserId);

    private Guid? GetActorUserId()
    {
        var sub = User.FindFirst("sub");
        return sub is not null && Guid.TryParse(sub.Value, out var parsed) ? parsed : null;
    }

    private static bool IsEscalated(
        TicketSlaProjection t, IReadOnlyDictionary<string, SlaTargetSetting> slaTargets, DateTime nowUtc)
    {
        var (responseDue, resolutionDue) = TicketsController.ComputeDueDates(t.CreatedAtUtc, t.Priority, slaTargets);
        return TicketsController.ComputeIsEscalated(
            t.Status, responseDue, resolutionDue, t.FirstRespondedAtUtc, t.ResolvedAtUtc, nowUtc);
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromServices] AppDbContext db, [FromServices] IRuntimeSettings runtimeSettings)
    {
        var actorId = GetActorUserId();
        if (actorId is null) return Unauthorized();

        var nowUtc = DateTime.UtcNow;
        var slaTargets = await TicketsController.ResolveSlaTargetsAsync(runtimeSettings);

        var tickets = await db.Tickets
            .Select(t => new TicketSlaProjection(
                t.Id, t.Subject, t.Status, t.Priority, t.CreatedAtUtc,
                t.FirstRespondedAtUtc, t.ResolvedAtUtc, t.AssignedToUserId))
            .ToListAsync();

        var kpis = new DashboardKpisDto(
            TotalTickets: tickets.Count,
            OpenTickets: tickets.Count(t => t.Status == "Open"),
            InProgressTickets: tickets.Count(t => t.Status == "InProgress"),
            ClosedTickets: tickets.Count(t => t.Status == "Closed"),
            EscalatedTickets: tickets.Count(t => IsEscalated(t, slaTargets, nowUtc)));

        // "My Work" is caller-scoped to the assigned tickets, narrowed to non-closed only -
        // a deliberately smaller, actionable-work subset of the full "/tickets Mine" toggle
        // (which includes closed history too), not a byte-for-byte copy of it.
        var myOpenTickets = tickets
            .Where(t => t.AssignedToUserId == actorId.Value && t.Status != "Closed")
            .ToList();

        var myRecentAssignedTickets = myOpenTickets
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(5)
            .Select(t => new DashboardMyTicketDto(
                t.Id, t.Subject, t.Status, t.Priority, IsEscalated(t, slaTargets, nowUtc), t.CreatedAtUtc))
            .ToList();

        var myUnreadNotificationCount = await db.Notifications
            .CountAsync(n => n.UserId == actorId.Value && !n.IsRead);

        // TicketTask has no AssignedToUserId of its own - "my tasks" is derived through the
        // parent ticket's current assignment (TicketsController.ListTasks uses the same
        // "due-date-nulls-last" ordering idiom).
        var myOutstandingTasks = await db.TicketTasks
            .Where(tt => !tt.IsDone && tt.Ticket!.AssignedToUserId == actorId.Value)
            .OrderBy(tt => tt.DueAtUtc == null)
            .ThenBy(tt => tt.DueAtUtc)
            .Take(5)
            .Select(tt => new DashboardMyTaskDto(tt.Id, tt.TicketId, tt.Title, tt.DueAtUtc))
            .ToListAsync();

        var myWork = new DashboardMyWorkDto(
            MyAssignedOpenCount: myOpenTickets.Count,
            MyRecentAssignedTickets: myRecentAssignedTickets,
            MyUnreadNotificationCount: myUnreadNotificationCount,
            MyOutstandingTasks: myOutstandingTasks);

        DashboardAdminSummaryDto? adminSummary = null;
        if (User.IsInRole("Admin"))
        {
            adminSummary = await BuildAdminSummaryAsync(db, tickets, slaTargets, nowUtc);
        }

        return Ok(new DashboardResponseDto(kpis, myWork, adminSummary));
    }

    private async Task<DashboardAdminSummaryDto> BuildAdminSummaryAsync(
        AppDbContext db, List<TicketSlaProjection> tickets,
        IReadOnlyDictionary<string, SlaTargetSetting> slaTargets, DateTime nowUtc)
    {
        var unassignedOpenCount = tickets.Count(t => t.AssignedToUserId == null && t.Status != "Closed");
        var escalatedOpenCount = tickets.Count(t => t.Status != "Closed" && IsEscalated(t, slaTargets, nowUtc));

        // Same Agent/Admin role filter as ReportsController.BuildAgentReportAsync - a User row
        // can exist with no role assigned yet (AdminController.CreateUser doesn't require
        // one), and such a user must not appear in a "top agents" ranking.
        var agentUsers = await db.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role!.Name == "Agent" || ur.Role!.Name == "Admin"))
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync();

        var feedback = await db.TicketFeedbacks
            .Join(db.Tickets, f => f.TicketId, t => t.Id, (f, t) => new { f.Rating, t.AssignedToUserId })
            .Where(x => x.AssignedToUserId != null)
            .ToListAsync();
        var feedbackByAgent = feedback.ToLookup(x => x.AssignedToUserId!.Value);

        var ticketsByAgent = tickets.ToLookup(t => t.AssignedToUserId);

        var topAgents = agentUsers
            .Select(u =>
            {
                var agentTickets = ticketsByAgent[u.Id].ToList();
                var agentFeedback = feedbackByAgent[u.Id].ToList();
                double? averageSatisfaction = agentFeedback.Count == 0 ? null : agentFeedback.Average(f => f.Rating);

                return new DashboardAdminAgentRowDto(
                    UserId: u.Id,
                    DisplayName: u.DisplayName,
                    OpenAssignedCount: agentTickets.Count(t => t.Status != "Closed"),
                    ResolvedCount: agentTickets.Count(t => t.Status == "Closed"),
                    AverageSatisfaction: averageSatisfaction);
            })
            .OrderByDescending(row => row.ResolvedCount)
            .Take(5)
            .ToList();

        return new DashboardAdminSummaryDto(unassignedOpenCount, escalatedOpenCount, topAgents);
    }
}
