namespace CustomerSupportCrm.Api.Controllers;

public record TicketListItem(
    Guid Id,
    Guid CustomerId,
    string CustomerFullName,
    string Subject,
    string? Description,
    string Status,
    DateTime CreatedAtUtc,
    Guid? AssignedToUserId,
    string? AssignedToDisplayName,
    string Category,
    string Priority,
    DateTime ResponseDueAtUtc,
    DateTime ResolutionDueAtUtc,
    DateTime? FirstRespondedAtUtc,
    DateTime? ResolvedAtUtc,
    bool IsEscalated);

public class TicketUpsertRequest
{
    public Guid CustomerId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Open";
    public Guid? AssignedToUserId { get; set; }
    public string Category { get; set; } = "General";
    public string Priority { get; set; } = "Normal";
}

public record AssignableUserItem(Guid Id, string DisplayName);
