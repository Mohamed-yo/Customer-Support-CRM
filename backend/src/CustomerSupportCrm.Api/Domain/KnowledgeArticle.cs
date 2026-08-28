namespace CustomerSupportCrm.Api.Domain;

public class KnowledgeArticle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
