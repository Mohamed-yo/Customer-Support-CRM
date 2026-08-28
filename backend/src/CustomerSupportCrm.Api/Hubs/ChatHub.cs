using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Hubs;

// Story 12: live per-ticket chat between staff and the ticket's owning customer.
// Preserves the Story 11 Staff/Customer JWT identity separation - the `type` claim is
// the sole discriminator, mirroring every other controller in this codebase. Ownership
// is re-checked on every message (not just on join) so a stale group membership can
// never bypass it.
[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;

    public ChatHub(AppDbContext db)
    {
        _db = db;
    }

    private bool IsStaff => Context.User?.FindFirst("type")?.Value == "staff";

    private Guid? GetActorCustomerId()
    {
        var typeClaim = Context.User?.FindFirst("type")?.Value;
        if (typeClaim != "customer") return null;
        var sub = Context.User?.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var customerId)) return null;
        return customerId;
    }

    private Guid? GetActorUserId()
    {
        var typeClaim = Context.User?.FindFirst("type")?.Value;
        if (typeClaim != "staff") return null;
        var sub = Context.User?.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var userId)) return null;
        return userId;
    }

    private async Task<bool> CanAccessTicketAsync(Guid ticketId)
    {
        if (IsStaff) return true;

        var customerId = GetActorCustomerId();
        if (customerId is null) return false;

        return await _db.Tickets.AnyAsync(t => t.Id == ticketId && t.CustomerId == customerId.Value);
    }

    public async Task JoinTicket(Guid ticketId)
    {
        if (!await CanAccessTicketAsync(ticketId))
        {
            // Mirrors the 404-not-403 pattern used elsewhere for ticket ownership -
            // existence of another customer's ticket is never revealed.
            throw new HubException("not_found");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(ticketId));
    }

    public async Task SendMessage(Guid ticketId, string body)
    {
        // Re-verify on every message - a stale group membership must not bypass
        // ownership if it changed after JoinTicket.
        if (!await CanAccessTicketAsync(ticketId))
        {
            throw new HubException("not_found");
        }
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new HubException("body_required");
        }

        var senderType = IsStaff ? "Staff" : "Customer";
        var senderId = IsStaff ? GetActorUserId() : GetActorCustomerId();
        if (senderId is null)
        {
            throw new HubException("not_found");
        }

        var message = new ChatMessage
        {
            TicketId = ticketId,
            SenderType = senderType,
            SenderId = senderId.Value,
            Body = body,
        };
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        await Clients.Group(GroupName(ticketId)).SendAsync("ReceiveMessage", new
        {
            id = message.Id, ticketId = message.TicketId, senderType = message.SenderType,
            body = message.Body, sentAtUtc = message.SentAtUtc,
        });
    }

    private static string GroupName(Guid ticketId) => $"ticket:{ticketId}";
}
