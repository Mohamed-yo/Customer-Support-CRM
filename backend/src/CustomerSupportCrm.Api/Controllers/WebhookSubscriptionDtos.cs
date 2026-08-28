namespace CustomerSupportCrm.Api.Controllers;

public sealed record WebhookSubscriptionItem(
    Guid Id, string TargetUrl, string EventType, bool IsActive, DateTime CreatedAtUtc);

public sealed class WebhookSubscriptionUpsertRequest
{
    public string TargetUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
