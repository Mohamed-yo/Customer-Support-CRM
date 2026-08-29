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
    bool IsEscalated,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? BranchId,
    string? BranchName);

public class TicketUpsertRequest
{
    public Guid CustomerId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Open";
    public Guid? AssignedToUserId { get; set; }
    public string Category { get; set; } = "General";
    public string Priority { get; set; } = "Normal";
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }
}

public record AssignableUserItem(Guid Id, string DisplayName);

// Story 15: options for the ticket form's Department/Branch pickers - all-staff-readable
// (active only), distinct from DepartmentsController/BranchesController's Admin-only CRUD.
public record DepartmentOptionItem(Guid Id, string Name);
public record BranchOptionItem(Guid Id, string Name);

// Story 15: @-mention autocomplete candidates. A distinct, all-staff-readable endpoint from
// AdminController's Admin-only GET /api/admin/users - any agent composing a ticket note
// must be able to search mention candidates, not just Admins.
public record MentionableUserItem(Guid Id, string DisplayName, string Email);
