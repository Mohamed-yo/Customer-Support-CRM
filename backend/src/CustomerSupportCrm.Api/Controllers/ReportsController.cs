using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

// Story 13: read-only, Admin-only reporting over data Stories 07/09/10/11 already
// persist - no new entity, no migration. SLA/escalation math is never reimplemented
// here; every endpoint calls TicketsController.ComputeDueDates/ComputeIsEscalated.
//
// SLA outcome policy (documented here, applied consistently by /sla, /agents via
// AverageResolutionMinutes, and /dashboard): for a given ticket, its response (or
// resolution) outcome is:
//   - Met      - responded/resolved within the computed due date.
//   - Breached - responded/resolved after the due date, OR never responded/resolved and
//                the due date has already passed as of now.
//   - Pending  - never responded/resolved yet, but the due date has not passed yet.
// Pending tickets are excluded from TotalConsidered/Met/Breached (their outcome isn't
// determined yet - mirrors ComputeIsEscalated's own "not escalated until overdue" rule),
// so ResponseMetPercent/ResolutionMetPercent always sum Met+Breached to 100% of
// TotalConsidered. Averages (AverageResponseMinutes/AverageResolutionMinutes) count any
// ticket that HAS actually responded/resolved (met or breached), never Pending ones.
[ApiController]
[Route("api/reports")]
[Authorize(Policy = "RequireStaff", Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private sealed record TicketSlaProjection(
        string Status, string Priority, DateTime CreatedAtUtc,
        DateTime? FirstRespondedAtUtc, DateTime? ResolvedAtUtc);

    private sealed record TicketAgentProjection(
        Guid? AssignedToUserId, string Status, DateTime CreatedAtUtc, DateTime? ResolvedAtUtc);

    private sealed record FeedbackProjection(
        int Rating, string Category, Guid? AssignedToUserId, string? AssignedToDisplayName);

    private static bool RangeInvalid(ReportDateRangeQuery range) =>
        range.FromUtc.HasValue && range.ToUtc.HasValue && range.FromUtc > range.ToUtc;

    private static IQueryable<Ticket> FilterByRange(IQueryable<Ticket> query, ReportDateRangeQuery range) =>
        query.Where(t =>
            (!range.FromUtc.HasValue || t.CreatedAtUtc >= range.FromUtc) &&
            (!range.ToUtc.HasValue || t.CreatedAtUtc <= range.ToUtc));

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTicketCounts(
        [FromServices] AppDbContext db, [FromQuery] ReportDateRangeQuery range)
    {
        if (RangeInvalid(range)) return BadRequest(new { error = "date_range_invalid" });

        var report = await BuildTicketCountsAsync(db, range);
        return Ok(report);
    }

    [HttpGet("sla")]
    public async Task<IActionResult> GetSlaPerformance(
        [FromServices] AppDbContext db, [FromServices] IRuntimeSettings runtimeSettings, [FromQuery] ReportDateRangeQuery range)
    {
        if (RangeInvalid(range)) return BadRequest(new { error = "date_range_invalid" });

        var report = await BuildSlaReportAsync(db, runtimeSettings, range);
        return Ok(report);
    }

    [HttpGet("agents")]
    public async Task<IActionResult> GetAgentPerformance(
        [FromServices] AppDbContext db, [FromQuery] ReportDateRangeQuery range)
    {
        if (RangeInvalid(range)) return BadRequest(new { error = "date_range_invalid" });

        var report = await BuildAgentReportAsync(db, range);
        return Ok(report);
    }

    [HttpGet("satisfaction")]
    public async Task<IActionResult> GetSatisfaction(
        [FromServices] AppDbContext db, [FromQuery] ReportDateRangeQuery range)
    {
        if (RangeInvalid(range)) return BadRequest(new { error = "date_range_invalid" });

        var report = await BuildSatisfactionReportAsync(db, range);
        return Ok(report);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromServices] AppDbContext db, [FromServices] IRuntimeSettings runtimeSettings, [FromQuery] ReportDateRangeQuery range)
    {
        if (RangeInvalid(range)) return BadRequest(new { error = "date_range_invalid" });

        var tickets = await BuildTicketCountsAsync(db, range);
        var sla = await BuildSlaReportAsync(db, runtimeSettings, range);
        var agents = await BuildAgentReportAsync(db, range);
        var satisfaction = await BuildSatisfactionReportAsync(db, range);

        var topAgents = agents.Agents
            .OrderByDescending(a => a.Resolved)
            .Take(5)
            .ToList();

        return Ok(new ManagementDashboardReport(tickets, sla, topAgents, satisfaction));
    }

    private async Task<TicketCountsReport> BuildTicketCountsAsync(AppDbContext db, ReportDateRangeQuery range)
    {
        var tickets = FilterByRange(db.Tickets.AsQueryable(), range);

        var total = await tickets.CountAsync();
        var statusCounts = await tickets.GroupBy(t => t.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
        var categoryCounts = await tickets.GroupBy(t => t.Category).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
        var priorityCounts = await tickets.GroupBy(t => t.Priority).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
        var sourceCounts = await tickets.GroupBy(t => t.Source).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();

        // Zero-fill the three known allow-lists so the report always has a stable, complete
        // shape for the frontend to render as a fixed table, even when a value has no
        // tickets this period. Source has no equivalent allow-list constant (Story 12
        // removed the unused one) - it reports only the values actually present.
        Dictionary<string, int> ZeroFill(string[] allowed, IEnumerable<(string Key, int Count)> found)
        {
            var byKey = found.ToDictionary(f => f.Key, f => f.Count);
            return allowed.ToDictionary(k => k, k => byKey.GetValueOrDefault(k, 0));
        }

        return new TicketCountsReport(
            total,
            ZeroFill(TicketsController.AllowedStatuses, statusCounts.Select(s => (s.Key, s.Count))),
            ZeroFill(TicketsController.AllowedCategories, categoryCounts.Select(s => (s.Key, s.Count))),
            ZeroFill(TicketsController.AllowedPriorities, priorityCounts.Select(s => (s.Key, s.Count))),
            sourceCounts.ToDictionary(s => s.Key, s => s.Count));
    }

    private async Task<SlaPerformanceReport> BuildSlaReportAsync(
        AppDbContext db, IRuntimeSettings runtimeSettings, ReportDateRangeQuery range)
    {
        var nowUtc = DateTime.UtcNow;

        var projected = await FilterByRange(db.Tickets.AsQueryable(), range)
            .Select(t => new TicketSlaProjection(t.Status, t.Priority, t.CreatedAtUtc, t.FirstRespondedAtUtc, t.ResolvedAtUtc))
            .ToListAsync();

        var slaTargets = await TicketsController.ResolveSlaTargetsAsync(runtimeSettings);
        int responseMet = 0, responseBreached = 0, resolutionMet = 0, resolutionBreached = 0, escalated = 0;
        var responseMinutes = new List<double>();
        var resolutionMinutes = new List<double>();

        foreach (var t in projected)
        {
            var (responseDue, resolutionDue) = TicketsController.ComputeDueDates(t.CreatedAtUtc, t.Priority, slaTargets);

            if (t.FirstRespondedAtUtc.HasValue)
            {
                responseMinutes.Add((t.FirstRespondedAtUtc.Value - t.CreatedAtUtc).TotalMinutes);
                if (t.FirstRespondedAtUtc.Value <= responseDue) responseMet++; else responseBreached++;
            }
            else if (nowUtc > responseDue)
            {
                responseBreached++;
            }
            // else: pending - not yet due, not yet responded to; excluded from both counts.

            if (t.ResolvedAtUtc.HasValue)
            {
                resolutionMinutes.Add((t.ResolvedAtUtc.Value - t.CreatedAtUtc).TotalMinutes);
                if (t.ResolvedAtUtc.Value <= resolutionDue) resolutionMet++; else resolutionBreached++;
            }
            else if (nowUtc > resolutionDue)
            {
                resolutionBreached++;
            }

            if (TicketsController.ComputeIsEscalated(
                    t.Status, responseDue, resolutionDue, t.FirstRespondedAtUtc, t.ResolvedAtUtc, nowUtc))
            {
                escalated++;
            }
        }

        var responseConsidered = responseMet + responseBreached;
        var resolutionConsidered = resolutionMet + resolutionBreached;

        return new SlaPerformanceReport(
            TotalConsidered: projected.Count,
            ResponseMet: responseMet,
            ResponseBreached: responseBreached,
            ResponseMetPercent: responseConsidered == 0 ? 0 : responseMet * 100.0 / responseConsidered,
            ResolutionMet: resolutionMet,
            ResolutionBreached: resolutionBreached,
            ResolutionMetPercent: resolutionConsidered == 0 ? 0 : resolutionMet * 100.0 / resolutionConsidered,
            AverageResponseMinutes: responseMinutes.Count == 0 ? 0 : responseMinutes.Average(),
            AverageResolutionMinutes: resolutionMinutes.Count == 0 ? 0 : resolutionMinutes.Average(),
            EscalatedCount: escalated);
    }

    private async Task<AgentPerformanceReport> BuildAgentReportAsync(AppDbContext db, ReportDateRangeQuery range)
    {
        var agentUsers = await db.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role!.Name == "Agent" || ur.Role!.Name == "Admin"))
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync();

        var assignedTickets = await FilterByRange(db.Tickets.AsQueryable(), range)
            .Where(t => t.AssignedToUserId != null)
            .Select(t => new TicketAgentProjection(t.AssignedToUserId, t.Status, t.CreatedAtUtc, t.ResolvedAtUtc))
            .ToListAsync();

        var byAgent = assignedTickets.ToLookup(t => t.AssignedToUserId!.Value);

        var rows = agentUsers.Select(u =>
        {
            var tickets = byAgent[u.Id].ToList();
            var closed = tickets.Where(t => t.Status == "Closed").ToList();
            var resolutionMinutes = closed
                .Where(t => t.ResolvedAtUtc.HasValue)
                .Select(t => (t.ResolvedAtUtc!.Value - t.CreatedAtUtc).TotalMinutes)
                .ToList();

            return new AgentPerformanceRow(
                UserId: u.Id,
                DisplayName: u.DisplayName,
                Open: tickets.Count(t => t.Status == "Open"),
                InProgress: tickets.Count(t => t.Status == "InProgress"),
                Closed: closed.Count,
                // "Resolved" mirrors "Closed" - this domain has no separate Resolved status
                // (Ticket.ResolvedAtUtc is set exactly when Status becomes "Closed").
                Resolved: closed.Count,
                AverageResolutionMinutes: resolutionMinutes.Count == 0 ? 0 : resolutionMinutes.Average());
        }).ToList();

        return new AgentPerformanceReport(rows);
    }

    private async Task<SatisfactionReport> BuildSatisfactionReportAsync(AppDbContext db, ReportDateRangeQuery range)
    {
        var feedback = await db.TicketFeedbacks
            .Join(FilterByRange(db.Tickets.AsQueryable(), range), f => f.TicketId, t => t.Id, (f, t) => new { f.Rating, t.Category, t.AssignedToUserId, t.AssignedToUser })
            .Select(x => new FeedbackProjection(x.Rating, x.Category, x.AssignedToUserId, x.AssignedToUser == null ? null : x.AssignedToUser.DisplayName))
            .ToListAsync();

        var closedTicketCount = await FilterByRange(db.Tickets.AsQueryable(), range).CountAsync(t => t.Status == "Closed");

        var distribution = Enumerable.Range(1, 5)
            .Select(rating => new RatingDistributionEntry(rating, feedback.Count(f => f.Rating == rating)))
            .ToList();

        var byCategory = feedback
            .GroupBy(f => f.Category)
            .ToDictionary(g => g.Key, g => g.Average(f => f.Rating));

        var byAgent = feedback
            .Where(f => f.AssignedToUserId.HasValue)
            .GroupBy(f => f.AssignedToDisplayName ?? "Unassigned")
            .ToDictionary(g => g.Key, g => g.Average(f => f.Rating));

        return new SatisfactionReport(
            AverageRating: feedback.Count == 0 ? 0 : feedback.Average(f => f.Rating),
            FeedbackCount: feedback.Count,
            ClosedTicketCount: closedTicketCount,
            ResponseRatePercent: closedTicketCount == 0 ? 0 : feedback.Count * 100.0 / closedTicketCount,
            Distribution: distribution,
            AverageRatingByCategory: byCategory,
            AverageRatingByAgent: byAgent);
    }
}
