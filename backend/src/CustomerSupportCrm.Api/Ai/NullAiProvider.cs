namespace CustomerSupportCrm.Api.Ai;

// Default registration (Ai:Provider unset or "none") - every method returns NotConfigured
// without attempting any network call. Mirrors EmailSender/WhatsappSender/SmsSender's
// blank-configuration NotConfigured precedent, so every AI feature ships safely with no
// real vendor credentials and degrades visibly rather than failing silently.
public sealed class NullAiProvider : IAiProvider
{
    private const string NotConfiguredDetail = "AI is not configured in this environment.";

    public Task<AiResult<string>> SummarizeTicketAsync(string ticketContent, CancellationToken ct = default) =>
        Task.FromResult(new AiResult<string>(AiStatus.NotConfigured, Detail: NotConfiguredDetail));

    public Task<AiResult<string>> SuggestReplyAsync(string ticketContent, CancellationToken ct = default) =>
        Task.FromResult(new AiResult<string>(AiStatus.NotConfigured, Detail: NotConfiguredDetail));

    public Task<AiResult<string>> SuggestCategoryAsync(
        string ticketContent, IReadOnlyList<string> allowedCategories, CancellationToken ct = default) =>
        Task.FromResult(new AiResult<string>(AiStatus.NotConfigured, Detail: NotConfiguredDetail));

    public Task<AiResult<IReadOnlyList<Guid>>> SuggestKbArticlesAsync(
        string ticketContent, IReadOnlyList<KbArticleCandidate> candidates, CancellationToken ct = default) =>
        Task.FromResult(new AiResult<IReadOnlyList<Guid>>(AiStatus.NotConfigured, Detail: NotConfiguredDetail));

    public Task<AiResult<string>> ChatAsync(string sessionId, string message, CancellationToken ct = default) =>
        Task.FromResult(new AiResult<string>(AiStatus.NotConfigured, Detail: NotConfiguredDetail));
}
