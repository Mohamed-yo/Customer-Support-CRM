namespace CustomerSupportCrm.Api.Domain;

// Also serves as the team-collaboration thread on a ticket — there is no
// separate internal-comment mechanism.
public class TicketNote
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public Guid AuthorUserId { get; set; }
    public User? AuthorUser { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
