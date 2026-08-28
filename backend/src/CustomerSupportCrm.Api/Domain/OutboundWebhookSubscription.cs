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
}
