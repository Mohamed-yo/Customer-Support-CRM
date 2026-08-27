namespace CustomerSupportCrm.Api.Controllers;

public record QuickReplyTemplateItem(
    Guid Id,
    string Title,
    string Body,
    Guid CreatedByUserId,
    string CreatedByDisplayName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public class QuickReplyTemplateUpsertRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
