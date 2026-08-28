using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

// Story 12, Decision 2: Admin-only configuration for the generic outbound webhook
// mechanism (ERP / external systems) - mirrors AdminController.cs's exact RBAC shape.
[ApiController]
[Route("api/webhook-subscriptions")]
[Authorize(Policy = "RequireStaff", Roles = "Admin")]
public class WebhookSubscriptionsController : ControllerBase
{
    private static readonly string[] AllowedEventTypes = { "ticket.created", "ticket.closed" };

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
        var items = await db.OutboundWebhookSubscriptions
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new WebhookSubscriptionItem(s.Id, s.TargetUrl, s.EventType, s.IsActive, s.CreatedAtUtc))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] WebhookSubscriptionUpsertRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        if (!Uri.TryCreate(request.TargetUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new { error = "target_url_invalid" });
        }
        if (!AllowedEventTypes.Contains(request.EventType))
        {
            return BadRequest(new { error = "event_type_invalid" });
        }

        var actorId = GetActorUserId();
        if (actorId is null) return Unauthorized();

        var subscription = new OutboundWebhookSubscription
        {
            TargetUrl = request.TargetUrl,
            EventType = request.EventType,
            IsActive = request.IsActive,
            CreatedByUserId = actorId.Value,
        };
        db.OutboundWebhookSubscriptions.Add(subscription);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "webhook.subscription.create",
            Outcome = "success",
            ActorUserId = actorId,
            Details = subscription.Id.ToString(),
        });

        var item = new WebhookSubscriptionItem(
            subscription.Id, subscription.TargetUrl, subscription.EventType, subscription.IsActive, subscription.CreatedAtUtc);
        return CreatedAtAction(nameof(List), item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] WebhookSubscriptionUpsertRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        if (!Uri.TryCreate(request.TargetUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new { error = "target_url_invalid" });
        }
        if (!AllowedEventTypes.Contains(request.EventType))
        {
            return BadRequest(new { error = "event_type_invalid" });
        }

        var subscription = await db.OutboundWebhookSubscriptions.SingleOrDefaultAsync(s => s.Id == id);
        if (subscription is null) return NotFound(new { error = "subscription_not_found" });

        subscription.TargetUrl = request.TargetUrl;
        subscription.EventType = request.EventType;
        subscription.IsActive = request.IsActive;
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "webhook.subscription.update",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            Details = subscription.Id.ToString(),
        });

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        var subscription = await db.OutboundWebhookSubscriptions.SingleOrDefaultAsync(s => s.Id == id);
        if (subscription is null) return NotFound(new { error = "subscription_not_found" });

        db.OutboundWebhookSubscriptions.Remove(subscription);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "webhook.subscription.delete",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            Details = id.ToString(),
        });

        return NoContent();
    }
}
