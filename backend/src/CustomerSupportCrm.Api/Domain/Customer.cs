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

    // Null until the customer registers a portal login (Story 11). A customer record
    // created by staff (Stories 06-10) has no login capability until then.
    public string? PasswordHash { get; set; }

    // Set on create; never mutated. UTC.
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
