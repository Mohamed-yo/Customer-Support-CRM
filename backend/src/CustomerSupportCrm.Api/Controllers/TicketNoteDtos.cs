namespace CustomerSupportCrm.Api.Controllers;

public record TicketNoteItem(
    Guid Id,
    Guid TicketId,
    Guid AuthorUserId,
    string AuthorDisplayName,
    string Body,
    DateTime CreatedAtUtc);

public class TicketNoteCreateRequest
{
    public string Body { get; set; } = string.Empty;
}
