namespace CustomerSupportCrm.Api.Domain;

public class OutboundWebhookSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string TargetUrl { get; set; } = string.Empty;

    // "ticket.created" | "ticket.closed"
    public string EventType { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // No FK - mirrors AuditLog's "generic actor id" precedent.
    public Guid CreatedByUserId { get; set; }

    // HMAC-SHA256 signing key for outbound delivery (X-Squad-Signature header). Nullable:
    // existing rows predate signing and are backfilled by the AddPlatformAdminFoundations
    // migration; new subscriptions always get one generated at creation time.
    public string? SigningSecret { get; set; }
}
