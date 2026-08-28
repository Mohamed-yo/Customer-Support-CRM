namespace CustomerSupportCrm.Api.Domain;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    // Ticket-scoped notifications only in this story; kept nullable for future non-ticket types.
    public Guid? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    // "Assigned" | "Escalated"
    public string Type { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}
