namespace CustomerSupportCrm.Api.Controllers;

public record HistoryEntry(
    Guid Id,
    string Action,
    string Outcome,
    Guid? ActorUserId,
    string? ActorDisplayName,
    DateTime TimestampUtc);
