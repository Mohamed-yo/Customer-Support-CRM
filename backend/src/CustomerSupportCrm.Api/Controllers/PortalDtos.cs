namespace CustomerSupportCrm.Api.Controllers;

public record PortalSubmitTicketRequest(string Subject, string? Description, string? Priority);

public record PortalTicketListItem(
    Guid Id, string Subject, string Status, string Priority,
    DateTime CreatedAtUtc, DateTime? ResolvedAtUtc, bool HasFeedback);

public record PortalTicketHistoryEntry(DateTime TimestampUtc, string Action);

public record PortalTicketDetail(
    Guid Id, string Subject, string? Description, string Status, string Priority,
    DateTime CreatedAtUtc, DateTime? ResolvedAtUtc,
    IReadOnlyList<PortalTicketHistoryEntry> History,
    PortalFeedbackItem? Feedback);

public record PortalFeedbackRequest(int Rating, string? Comment);

public record PortalFeedbackItem(int Rating, string? Comment, DateTime CreatedAtUtc);
