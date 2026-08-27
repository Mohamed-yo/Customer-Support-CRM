namespace CustomerSupportCrm.Api.Domain;

// Also serves as the "reminder" concept — a task with a DueAtUtc set is a
// reminder; there is no separate reminder entity or delivery mechanism.
public class TicketTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateTime? DueAtUtc { get; set; }
    public bool IsDone { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
