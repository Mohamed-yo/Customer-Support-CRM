namespace CustomerSupportCrm.Api.Controllers;

public record NotificationItem(
    Guid Id,
    string Type,
    string Message,
    Guid? TicketId,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public record UnreadCountResponse(int Count);
