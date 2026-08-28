namespace CustomerSupportCrm.Api.Integrations;

public enum SendStatus
{
    Success,
    Failure,
    NotConfigured,
}

public sealed record SendResult(SendStatus Status, string? Detail = null, string? ExternalMessageId = null);
