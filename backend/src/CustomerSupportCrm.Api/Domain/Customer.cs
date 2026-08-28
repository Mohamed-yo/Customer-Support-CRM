namespace CustomerSupportCrm.Api.Domain;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Required. Human display name of the customer contact.
    public string FullName { get; set; } = string.Empty;

    // Required. Format validated at the API boundary; stored lowercased-as-provided.
    public string Email { get; set; } = string.Empty;

    // Optional. Free-form phone string; no format enforcement this story.
    public string? Phone { get; set; }

    // Null until the customer registers a portal login (Story 11). A customer record
    // created by staff (Stories 06-10) has no login capability until then.
    public string? PasswordHash { get; set; }

    // Set on create; never mutated. UTC.
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
