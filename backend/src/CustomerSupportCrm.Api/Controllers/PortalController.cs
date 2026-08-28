using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/portal")]
[Authorize(Policy = "RequireCustomer")]
public class PortalController : ControllerBase
{
    // Fixed for every portal-submitted ticket - customers do not choose an internal
    // routing category (Out of scope: no customer-facing category picker this story).
    private const string PortalTicketCategory = "General";

    private Guid GetActorCustomerId()
    {
        var typeClaim = User.FindFirst("type")?.Value;
        if (typeClaim != "customer")
        {
            throw new UnauthorizedAccessException("Caller is not a customer identity.");
        }
        var sub = User.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var customerId))
        {
            throw new UnauthorizedAccessException("No valid customer id in token.");
        }
        return customerId;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me([FromServices] AppDbContext db)
    {
        var customerId = GetActorCustomerId();
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == customerId);
        if (customer is null) return Unauthorized();

        // A customer reachable via a "customer" JWT always registered with an email
        // (Story 11 Register/Login both require one) - a phone-only, WhatsApp/SMS-created
        // customer (Story 12) has no PasswordHash and can never obtain this JWT.
        return Ok(new CustomerMeResponse(customer.Id, customer.Email!, customer.FullName));
    }

    [HttpPost("tickets")]
    public async Task<IActionResult> SubmitTicket(
        [FromBody] PortalSubmitTicketRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit,
        [FromServices] IOutboundWebhookDispatcher webhooks)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return BadRequest(new { error = "subject_required" });
        }
        var priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority;
        if (!TicketsController.AllowedPriorities.Contains(priority))
        {
            return BadRequest(new { error = "priority_invalid" });
        }

        var customerId = GetActorCustomerId();

        // Never trust a client-supplied CustomerId - always the server-resolved caller.
        var ticket = new Ticket
        {
            CustomerId = customerId,
            Subject = request.Subject,
            Description = request.Description,
            Status = "Open",
            Category = PortalTicketCategory,
            Priority = priority,
            AssignedToUserId = null,
            Source = "Portal",
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "ticket.create",
            Outcome = "success",
            ActorUserId = customerId,
            Details = ticket.Id.ToString(),
        });

        // Decision 5 / Story 10 parity: a portal ticket always has no assignee at
        // submission, so it always goes through the same auto-assignment path as a
        // staff-created ticket with AssignedToUserId omitted.
        var assignee = await TicketsController.PickLeastLoadedAssigneeAsync(db);
        if (assignee is not null)
        {
            ticket.AssignedToUserId = assignee.Id;
            await db.SaveChangesAsync();
            await TicketsController.CreateAssignedNotificationAsync(ticket.Id, assignee.Id, db);
        }

        await webhooks.DispatchAsync("ticket.created", new
        {
            id = ticket.Id, subject = ticket.Subject, status = ticket.Status,
            priority = ticket.Priority, source = ticket.Source, customerId = ticket.CustomerId,
            createdAtUtc = ticket.CreatedAtUtc,
        });

        return CreatedAtAction(nameof(GetMyRequest), new { id = ticket.Id }, ToListItem(ticket, hasFeedback: false));
    }

    [HttpGet("my-requests")]
    public async Task<IActionResult> ListMyRequests([FromServices] AppDbContext db)
    {
        var customerId = GetActorCustomerId();

        var items = await db.Tickets
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new PortalTicketListItem(
                t.Id, t.Subject, t.Status, t.Priority, t.CreatedAtUtc, t.ResolvedAtUtc,
                db.TicketFeedbacks.Any(f => f.TicketId == t.Id)))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("my-requests/{id:guid}")]
    public async Task<IActionResult> GetMyRequest(Guid id, [FromServices] AppDbContext db)
    {
        var customerId = GetActorCustomerId();

        // Single query, same-shaped 404 whether the ticket doesn't exist or belongs to
        // someone else - existence of another customer's ticket is never revealed.
        var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);
        if (ticket is null) return NotFound(new { error = "ticket_not_found" });

        var idStr = id.ToString();
        var history = await db.AuditLogs
            .Where(a => a.Action.StartsWith("ticket.") && a.Details == idStr)
            .OrderBy(a => a.TimestampUtc)
            .Select(a => new PortalTicketHistoryEntry(a.TimestampUtc, a.Action))
            .ToListAsync();

        var feedback = await db.TicketFeedbacks
            .Where(f => f.TicketId == id)
            .Select(f => new PortalFeedbackItem(f.Rating, f.Comment, f.CreatedAtUtc))
            .SingleOrDefaultAsync();

        return Ok(new PortalTicketDetail(
            ticket.Id, ticket.Subject, ticket.Description, ticket.Status, ticket.Priority,
            ticket.CreatedAtUtc, ticket.ResolvedAtUtc, history, feedback));
    }

    [HttpPost("my-requests/{id:guid}/feedback")]
    public async Task<IActionResult> SubmitFeedback(
        Guid id,
        [FromBody] PortalFeedbackRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new { error = "rating_invalid" });
        }
        if (request.Comment is { Length: > 2000 })
        {
            return BadRequest(new { error = "comment_too_long" });
        }

        var customerId = GetActorCustomerId();

        var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);
        if (ticket is null) return NotFound(new { error = "ticket_not_found" });

        // Decision 4: exact-match "Closed" only - no case-insensitive or "Resolved" leniency.
        if (ticket.Status != "Closed")
        {
            return BadRequest(new { error = "ticket_not_closed" });
        }

        var alreadyExists = await db.TicketFeedbacks.AnyAsync(f => f.TicketId == id);
        if (alreadyExists)
        {
            return Conflict(new { error = "feedback_already_submitted" });
        }

        var feedback = new TicketFeedback
        {
            TicketId = id,
            CustomerId = customerId,
            Rating = request.Rating,
            Comment = request.Comment,
        };
        db.TicketFeedbacks.Add(feedback);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "portal.ticket.feedback",
            Outcome = "success",
            ActorUserId = customerId,
            Details = id.ToString(),
        });

        return CreatedAtAction(nameof(GetMyRequest), new { id }, new PortalFeedbackItem(feedback.Rating, feedback.Comment, feedback.CreatedAtUtc));
    }

    private static PortalTicketListItem ToListItem(Ticket ticket, bool hasFeedback) =>
        new(ticket.Id, ticket.Subject, ticket.Status, ticket.Priority, ticket.CreatedAtUtc, ticket.ResolvedAtUtc, hasFeedback);
}
