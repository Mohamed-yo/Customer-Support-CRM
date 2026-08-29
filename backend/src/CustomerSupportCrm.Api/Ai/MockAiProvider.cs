namespace CustomerSupportCrm.Api.Ai;

// Registered when Ai:Provider = "mock" - deterministic, canned output with no external
// network call. Lets the credential-dependent acceptance criteria (an AI feature actually
// producing a result) be verified in dev/CI without any real vendor API key, while
// NullAiProvider (the default) separately covers the credential-independent, NotConfigured
// acceptance criteria.
public sealed class MockAiProvider : IAiProvider
{
    public Task<AiResult<string>> SummarizeTicketAsync(string ticketContent, CancellationToken ct = default) =>
        Task.FromResult(new AiResult<string>(AiStatus.Ok, Value: $"Summary: {Truncate(ticketContent)}"));

    public Task<AiResult<string>> SuggestReplyAsync(string ticketContent, CancellationToken ct = default) =>
        Task.FromResult(new AiResult<string>(
            AiStatus.Ok, Value: "Thank you for reaching out. We're looking into this and will follow up shortly."));

    public Task<AiResult<string>> SuggestCategoryAsync(
        string ticketContent, IReadOnlyList<string> allowedCategories, CancellationToken ct = default)
    {
        var category = allowedCategories.Count > 0 ? allowedCategories[0] : "General";
        return Task.FromResult(new AiResult<string>(AiStatus.Ok, Value: category));
    }

    public Task<AiResult<IReadOnlyList<Guid>>> SuggestKbArticlesAsync(
        string ticketContent, IReadOnlyList<KbArticleCandidate> candidates, CancellationToken ct = default)
    {
        IReadOnlyList<Guid> ids = candidates.Take(3).Select(c => c.Id).ToList();
        return Task.FromResult(new AiResult<IReadOnlyList<Guid>>(AiStatus.Ok, Value: ids));
    }

    public Task<AiResult<string>> ChatAsync(string sessionId, string message, CancellationToken ct = default) =>
        Task.FromResult(new AiResult<string>(AiStatus.Ok, Value: $"(mock AI) You said: {message}"));

    private static string Truncate(string text) => text.Length <= 200 ? text : text[..200] + "…";
}
