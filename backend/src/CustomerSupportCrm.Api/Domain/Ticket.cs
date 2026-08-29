namespace CustomerSupportCrm.Api.Domain;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Allowed values: "Open", "InProgress", "Closed". Enforced in controller.
    public string Status { get; set; } = "Open";

    // Allowed values: "General", "Billing", "Technical", "Account". Enforced in controller.
    public string Category { get; set; } = "General";

    // Allowed values: "Low", "Normal", "High", "Urgent". Enforced in controller.
    public string Priority { get; set; } = "Normal";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Set once, the first time a TicketNote is posted on this ticket. Never set by a
    // plain field edit (PUT). Used to compute IsEscalated against ResponseDueAtUtc.
    public DateTime? FirstRespondedAtUtc { get; set; }

    // Set when Status transitions into "Closed"; cleared back to null if reopened.
    // Used to compute IsEscalated against ResolutionDueAtUtc.
    public DateTime? ResolvedAtUtc { get; set; }

    // Allowed values: "Manual", "Portal", "WebForm", "Email", "WhatsApp", "SMS", "Chat".
    // Enforced in controller. Existing rows backfill to "Manual".
    public string Source { get; set; } = "Manual";

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
}
