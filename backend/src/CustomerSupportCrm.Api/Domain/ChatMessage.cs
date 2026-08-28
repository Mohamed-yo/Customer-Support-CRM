namespace CustomerSupportCrm.Api.Domain;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    // "Staff" | "Customer"
    public string SenderType { get; set; } = string.Empty;

    // Either a User.Id (Staff) or a Customer.Id (Customer) - no FK constraint, mirrors the
    // AuditLog.ActorUserId "generic actor id" precedent.
    public Guid SenderId { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
