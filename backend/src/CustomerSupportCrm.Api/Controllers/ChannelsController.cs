using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerSupportCrm.Api.Controllers;

// Story 12: Web Form (fully self-contained, no external dependency) plus complete
// provider-agnostic code paths for Email/WhatsApp/SMS. Inbound is authenticated by a
// shared-secret header (Decision 5). Outbound invokes the matching sender abstraction
// and always returns a clean Success/Failure/NotConfigured result - live provider
// connectivity is NOT verified for WhatsApp/SMS in this environment (no real
// credentials exist); see the story report for what is and isn't end-to-end tested.
[ApiController]
[Route("api/channels")]
public class ChannelsController : ControllerBase
{
    // route segment -> Ticket.Source / ChannelMessage.Channel value
    private static readonly Dictionary<string, string> ChannelMap = new()
    {
        ["email"] = "Email",
        ["whatsapp"] = "WhatsApp",
        ["sms"] = "SMS",
    };

    [AllowAnonymous]
    [HttpPost("webform")]
    public async Task<IActionResult> WebForm(
        [FromBody] WebFormSubmissionRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit,
        [FromServices] IOutboundWebhookDispatcher webhooks)
    {
        // [ApiController] already rejects invalid ModelState (missing/malformed
        // FullName/Email/Subject) with a 400 before this action runs.
        var priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority;
        if (!TicketsController.AllowedPriorities.Contains(priority))
        {
            return BadRequest(new { error = "priority_invalid" });
        }

        var customer = await ChannelIntakeHelpers.FindOrCreateCustomerAsync(
            db, request.FullName, request.Email, request.Phone);

        var ticket = new Ticket
        {
            CustomerId = customer.Id,
            Customer = customer,
            Subject = request.Subject,
            Description = request.Description,
            Status = "Open",
            Category = "General",
            Priority = priority,
            AssignedToUserId = null,
            Source = "WebForm",
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "ticket.create",
            Outcome = "success",
            ActorUserId = null,
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

        var reference = ticket.Id.ToString("N")[..8].ToUpperInvariant();
        return StatusCode(201, new WebFormSubmissionResponse(ticket.Id, reference));
    }

    [AllowAnonymous]
    [HttpPost("{channel}/inbound")]
    public async Task<IActionResult> Inbound(
        string channel,
        [FromBody] InboundChannelWebhookRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit,
        [FromServices] IInboundWebhookAuthenticator authenticator,
        [FromServices] IOutboundWebhookDispatcher webhooks)
    {
        // The channel-name check runs before authentication - it is a pure route-shape
        // check ("email"/"whatsapp"/"sms" are the only three segments this endpoint ever
        // accepts, already public knowledge from this feature's own documentation), not a
        // disclosure of anything secret. Only once the channel resolves to a real secret
        // slot does the shared-secret check gate everything else (body validation, DB
        // access, ticket/customer mutation).
        if (!ChannelMap.TryGetValue(channel.ToLowerInvariant(), out var source))
        {
            return BadRequest(new { error = "channel_invalid" });
        }

        if (!authenticator.Verify(source, Request))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.From) || string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(new { error = "from_and_body_required" });
        }

        if (request.TicketId.HasValue)
        {
            var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == request.TicketId.Value);
            if (ticket is null)
            {
                return BadRequest(new { error = "ticket_not_found" });
            }
            if (ticket.Source != source)
            {
                return BadRequest(new { error = "ticket_source_mismatch" });
            }

            var appended = new ChannelMessage
            {
                TicketId = ticket.Id,
                Channel = source,
                Direction = "Inbound",
                FromAddress = request.From,
                Subject = request.Subject,
                Body = request.Body,
                SendResult = string.Empty,
            };
            db.ChannelMessages.Add(appended);
            await db.SaveChangesAsync();

            return Ok(new InboundChannelWebhookResponse(ticket.Id, appended.Id, CreatedNewTicket: false));
        }

        // Email identifies the customer by email; WhatsApp/SMS identify by phone - "From"
        // on those two channels is a phone number, never an email address (Important #2
        // fix: it must never be written into Customer.Email).
        var customer = source == "Email"
            ? await ChannelIntakeHelpers.FindOrCreateCustomerAsync(
                db, request.FromName ?? request.From, request.From, phone: null)
            : await ChannelIntakeHelpers.FindOrCreateCustomerByPhoneAsync(
                db, request.FromName ?? request.From, request.From);

        var newTicket = new Ticket
        {
            CustomerId = customer.Id,
            Customer = customer,
            Subject = request.Subject ?? request.Body[..Math.Min(request.Body.Length, 200)],
            Description = request.Body,
            Status = "Open",
            Category = "General",
            Priority = "Normal",
            AssignedToUserId = null,
            Source = source,
        };
        db.Tickets.Add(newTicket);
        await db.SaveChangesAsync();

        var inboundMessage = new ChannelMessage
        {
            TicketId = newTicket.Id,
            Channel = source,
            Direction = "Inbound",
            FromAddress = request.From,
            Subject = request.Subject,
            Body = request.Body,
            SendResult = string.Empty,
        };
        db.ChannelMessages.Add(inboundMessage);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "ticket.create",
            Outcome = "success",
            ActorUserId = null,
            Details = newTicket.Id.ToString(),
        });

        var assignee = await TicketsController.PickLeastLoadedAssigneeAsync(db);
        if (assignee is not null)
        {
            newTicket.AssignedToUserId = assignee.Id;
            await db.SaveChangesAsync();
            await TicketsController.CreateAssignedNotificationAsync(newTicket.Id, assignee.Id, db);
        }

        await webhooks.DispatchAsync("ticket.created", new
        {
            id = newTicket.Id, subject = newTicket.Subject, status = newTicket.Status,
            priority = newTicket.Priority, source = newTicket.Source, customerId = newTicket.CustomerId,
            createdAtUtc = newTicket.CreatedAtUtc,
        });

        return Ok(new InboundChannelWebhookResponse(newTicket.Id, inboundMessage.Id, CreatedNewTicket: true));
    }

    [Authorize(Policy = "RequireStaff")]
    [HttpPost("{channel}/outbound")]
    public async Task<IActionResult> Outbound(
        string channel,
        [FromBody] OutboundChannelReplyRequest request,
        [FromServices] AppDbContext db,
        [FromServices] IServiceProvider serviceProvider)
    {
        if (!ChannelMap.TryGetValue(channel.ToLowerInvariant(), out var source))
        {
            return BadRequest(new { error = "channel_invalid" });
        }

        var ticket = await db.Tickets
            .Include(t => t.Customer)
            .SingleOrDefaultAsync(t => t.Id == request.TicketId);
        if (ticket is null) return NotFound(new { error = "ticket_not_found" });
        if (ticket.Source != source)
        {
            return BadRequest(new { error = "ticket_source_mismatch" });
        }

        // Email replies go to the customer's email; WhatsApp/SMS replies go to their phone
        // (Story 12 fix: Customer.Email is no longer guaranteed non-null, so this can no
        // longer default to Email and override for the other two channels).
        var toAddress = source == "Email" ? ticket.Customer!.Email : ticket.Customer!.Phone;
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            return BadRequest(new { error = "customer_contact_missing" });
        }

        var message = new ChannelMessage
        {
            TicketId = ticket.Id,
            Channel = source,
            Direction = "Outbound",
            FromAddress = toAddress,
            ToAddress = toAddress,
            Subject = request.Subject ?? ticket.Subject,
            Body = request.Body,
            SendResult = "",
        };
        db.ChannelMessages.Add(message);
        await db.SaveChangesAsync();

        var sender = serviceProvider.GetRequiredKeyedService<IChannelSender>(source);
        var result = await sender.SendAsync(toAddress, message.Subject, request.Body);

        message.SendResult = result.Status.ToString();
        message.SendResultDetail = result.Detail;
        message.ExternalMessageId = result.ExternalMessageId;
        await db.SaveChangesAsync();

        return Ok(new ChannelMessageItem(
            message.Id, message.Channel, message.Direction, message.FromAddress, message.ToAddress,
            message.Subject, message.Body, message.SendResult, message.SendResultDetail, message.CreatedAtUtc));
    }

    [Authorize(Policy = "RequireStaff")]
    [HttpGet("tickets/{ticketId:guid}/messages")]
    public async Task<IActionResult> ListMessages(Guid ticketId, [FromServices] AppDbContext db)
    {
        var exists = await db.Tickets.AnyAsync(t => t.Id == ticketId);
        if (!exists) return NotFound(new { error = "ticket_not_found" });

        var items = await db.ChannelMessages
            .Where(m => m.TicketId == ticketId)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new ChannelMessageItem(
                m.Id, m.Channel, m.Direction, m.FromAddress, m.ToAddress, m.Subject, m.Body,
                m.SendResult, m.SendResultDetail, m.CreatedAtUtc))
            .ToListAsync();

        return Ok(items);
    }
}
