namespace CustomerSupportCrm.Api.Controllers;

public sealed record WebhookSubscriptionItem(
    Guid Id, string TargetUrl, string EventType, bool IsActive, DateTime CreatedAtUtc);

// Only ever present in the response to Create/RotateSecret - never returned again by List,
// mirroring ApiKeysController's plaintext-once pattern.
public sealed record WebhookSubscriptionCreatedItem(
    Guid Id, string TargetUrl, string EventType, bool IsActive, DateTime CreatedAtUtc, string SigningSecret);

public sealed record RotateSecretResponse(string SigningSecret);

public sealed class WebhookSubscriptionUpsertRequest
{
    public string TargetUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
