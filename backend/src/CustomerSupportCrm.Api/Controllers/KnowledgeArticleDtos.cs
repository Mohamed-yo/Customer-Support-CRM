namespace CustomerSupportCrm.Api.Controllers;

public record KnowledgeArticleListItem(Guid Id, string Title, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public record KnowledgeArticleItem(
    Guid Id, string Title, string Body,
    Guid CreatedByUserId, string CreatedByDisplayName, DateTime CreatedAtUtc,
    Guid? UpdatedByUserId, string? UpdatedByDisplayName, DateTime? UpdatedAtUtc);

public record KnowledgeArticleUpsertRequest(string Title, string Body);
