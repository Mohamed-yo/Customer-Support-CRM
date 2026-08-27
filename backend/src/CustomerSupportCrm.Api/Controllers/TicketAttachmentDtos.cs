namespace CustomerSupportCrm.Api.Controllers;

public record TicketAttachmentItem(
    Guid Id,
    Guid TicketId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedByUserId,
    string UploadedByDisplayName,
    DateTime CreatedAtUtc);
