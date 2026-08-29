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

    // Story 15: staff user ids to notify with a "Mention" notification. Each must reference
    // an existing, active staff user - validated in the controller, not here.
    public Guid[] MentionedUserIds { get; set; } = Array.Empty<Guid>();
}
