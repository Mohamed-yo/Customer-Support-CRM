using CustomerSupportCrm.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private const int MaxItems = 50;

    private Guid? GetActorUserId()
    {
        var sub = User.FindFirst("sub");
        if (sub is not null && Guid.TryParse(sub.Value, out var parsed))
        {
            return parsed;
        }
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db)
    {
        var actorId = GetActorUserId();
        if (actorId is null) return Unauthorized();

        var items = await db.Notifications
            .Where(n => n.UserId == actorId.Value)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(MaxItems)
            .Select(n => new NotificationItem(n.Id, n.Type, n.Message, n.TicketId, n.IsRead, n.CreatedAtUtc, n.ReadAtUtc))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount([FromServices] AppDbContext db)
    {
        var actorId = GetActorUserId();
        if (actorId is null) return Unauthorized();

        var count = await db.Notifications.CountAsync(n => n.UserId == actorId.Value && !n.IsRead);
        return Ok(new UnreadCountResponse(count));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, [FromServices] AppDbContext db)
    {
        var actorId = GetActorUserId();
        if (actorId is null) return Unauthorized();

        var notification = await db.Notifications.SingleOrDefaultAsync(n => n.Id == id);
        // Not found and belongs-to-someone-else both return 404: never reveal another
        // user's notification exists.
        if (notification is null || notification.UserId != actorId.Value)
        {
            return NotFound(new { error = "notification_not_found" });
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        return NoContent();
    }
}
