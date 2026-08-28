namespace CustomerSupportCrm.Api.Domain;

public class TicketFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    // Denormalized for query/authorization convenience - the ticket's own CustomerId is
    // the authority; this must always match it (set once, never mutated).
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int Rating { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
