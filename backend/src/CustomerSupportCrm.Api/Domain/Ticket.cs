namespace CustomerSupportCrm.Api.Domain;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Allowed values: "Open", "InProgress", "Closed". Enforced in controller.
    public string Status { get; set; } = "Open";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
