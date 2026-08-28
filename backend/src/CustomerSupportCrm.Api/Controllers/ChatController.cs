using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
public class ChatController : ControllerBase
{
    private bool IsStaff => User.FindFirst("type")?.Value == "staff";

    private Guid? GetActorCustomerId()
    {
        if (User.FindFirst("type")?.Value != "customer") return null;
        var sub = User.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var customerId)) return null;
        return customerId;
    }

    // Accepts either a staff or a customer JWT - a bare [Authorize] with no policy is
    // satisfied by any authenticated caller regardless of `type`; ownership is enforced
    // explicitly below, mirroring ChatHub's CanAccessTicketAsync.
    [Authorize]
    [HttpGet("/api/chat/{ticketId:guid}/history")]
    public async Task<IActionResult> History(Guid ticketId, [FromServices] AppDbContext db)
    {
        if (IsStaff)
        {
            var exists = await db.Tickets.AnyAsync(t => t.Id == ticketId);
            if (!exists) return NotFound(new { error = "ticket_not_found" });
        }
        else
        {
            var customerId = GetActorCustomerId();
            if (customerId is null) return Unauthorized();

            var owns = await db.Tickets.AnyAsync(t => t.Id == ticketId && t.CustomerId == customerId.Value);
            if (!owns) return NotFound(new { error = "ticket_not_found" });
        }

        var items = await db.ChatMessages
            .Where(m => m.TicketId == ticketId)
            .OrderBy(m => m.SentAtUtc)
            .Select(m => new { id = m.Id, ticketId = m.TicketId, senderType = m.SenderType, body = m.Body, sentAtUtc = m.SentAtUtc })
            .ToListAsync();

        return Ok(items);
    }

    // Story 12 (post-review amendment): Live Chat is a first-class intake channel, not
    // merely an add-on to tickets created another way. A customer with no open chat
    // conversation gets a new Ticket(Source="Chat") through the exact same
    // auto-assignment/SLA/notification/webhook path as every other channel; continuing
    // an existing one reuses it - no duplicates.
    [Authorize(Policy = "RequireCustomer")]
    [HttpPost("/api/portal/chat/start")]
    public async Task<IActionResult> StartChat(
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit,
        [FromServices] IOutboundWebhookDispatcher webhooks)
    {
        var customerId = GetActorCustomerId();
        if (customerId is null) return Unauthorized();

        var existing = await db.Tickets
            .Where(t => t.CustomerId == customerId.Value && t.Source == "Chat" && t.Status != "Closed")
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync();
        if (existing is not null)
        {
            return Ok(new { ticketId = existing.Id });
        }

        var ticket = new Ticket
        {
            CustomerId = customerId.Value,
            Subject = "Live chat conversation",
            Status = "Open",
            Category = "General",
            Priority = "Normal",
            AssignedToUserId = null,
            Source = "Chat",
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

        return StatusCode(201, new { ticketId = ticket.Id });
    }
}
