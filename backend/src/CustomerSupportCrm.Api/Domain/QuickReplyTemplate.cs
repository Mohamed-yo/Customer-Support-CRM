namespace CustomerSupportCrm.Api.Domain;

// Shared, org-wide library of reusable note text — not scoped to a ticket.
public class QuickReplyTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
