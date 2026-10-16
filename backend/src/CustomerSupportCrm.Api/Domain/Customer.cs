namespace CustomerSupportCrm.Api.Domain;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Required. Human display name of the customer contact.
    public string FullName { get; set; } = string.Empty;

    // Optional (Story 12): required and format-validated at the API boundary for every
    // flow that collects an email (staff create/edit, WebForm, Email channel, portal
    // register/login) - but null for a customer first identified by phone only (WhatsApp/
    // SMS inbound). Never set to a phone number; see FindOrCreateCustomerByPhoneAsync.
    public string? Email { get; set; }

    // Optional. Free-form phone string; no format enforcement this story. Story 12: also
    // the identifying key for WhatsApp/SMS-originated customers (see AppDbContext's
    // filtered unique index).
    public string? Phone { get; set; }

    // Null until the customer completes portal email verification - the password is chosen
    // on the verification step, never at register time, so only someone who controls the
    // mailbox can set it. A staff/channel-created record has no login capability until then.
    public string? PasswordHash { get; set; }

    // SHA-256 (hex) of the one outstanding portal verification token, and when it expires.
    // Both null when nothing is pending. The raw token only ever exists in the emailed link.
    public string? EmailVerificationTokenHash { get; set; }
    public DateTime? EmailVerificationExpiresAtUtc { get; set; }

    // Set on create; never mutated. UTC.
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
