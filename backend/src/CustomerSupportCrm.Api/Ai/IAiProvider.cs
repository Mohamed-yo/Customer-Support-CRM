namespace CustomerSupportCrm.Api.Ai;

public sealed record KbArticleCandidate(Guid Id, string Title);

// Provider-agnostic AI abstraction - no vendor-specific type appears in this interface.
// Every method takes plain text/primitives (assembled by the caller from the DB), never a
// domain id, so a provider implementation never needs its own DB access.
public interface IAiProvider
{
    Task<AiResult<string>> SummarizeTicketAsync(string ticketContent, CancellationToken ct = default);

    Task<AiResult<string>> SuggestReplyAsync(string ticketContent, CancellationToken ct = default);

    Task<AiResult<string>> SuggestCategoryAsync(string ticketContent, IReadOnlyList<string> allowedCategories, CancellationToken ct = default);

    Task<AiResult<IReadOnlyList<Guid>>> SuggestKbArticlesAsync(
        string ticketContent, IReadOnlyList<KbArticleCandidate> candidates, CancellationToken ct = default);

    Task<AiResult<string>> ChatAsync(string sessionId, string message, CancellationToken ct = default);
}
