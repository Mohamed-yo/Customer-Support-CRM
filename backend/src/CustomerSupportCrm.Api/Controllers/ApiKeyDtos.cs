namespace CustomerSupportCrm.Api.Controllers;

public sealed record ApiKeyListItem(
    Guid Id, string Label, string Prefix, DateTime CreatedAtUtc,
    DateTime? LastUsedAtUtc, DateTime? RevokedAtUtc, bool IsActive);

public sealed class CreateApiKeyRequest
{
    public string Label { get; set; } = string.Empty;
}

// Only returned once, on creation. The plaintext is never stored or returned again.
public sealed record CreateApiKeyResponse(
    Guid Id, string Label, string Prefix, string PlaintextKey, DateTime CreatedAtUtc);
