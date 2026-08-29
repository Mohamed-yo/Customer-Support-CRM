namespace CustomerSupportCrm.Api.Domain;

// Story 24: an external client's credential for the additive "ApiKey" authentication
// scheme (Program.cs) - distinct from staff/customer JWTs. Only KeyHash is ever
// persisted; the plaintext is generated, hashed, and returned to the caller exactly once
// at creation time (ApiKeysController), never stored or retrievable again.
public class ApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Label { get; set; } = string.Empty;

    // PasswordHasher<ApiKey>.HashPassword output - never the plaintext.
    public string KeyHash { get; set; } = string.Empty;

    // First ~12 chars of the plaintext, stored for display and as a non-unique lookup
    // shortlist in the authentication handler (prefix collisions are handled there by
    // verifying every matching candidate's hash, not by assuming uniqueness).
    public string Prefix { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // No FK - mirrors OutboundWebhookSubscription's "generic actor id" precedent.
    public Guid CreatedByUserId { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public bool IsActive => RevokedAtUtc is null;
}
