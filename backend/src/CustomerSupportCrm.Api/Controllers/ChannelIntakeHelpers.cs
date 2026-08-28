using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

// Shared by WebForm/channel intake (Story 12): finds-or-creates a Customer for an
// anonymous/external intake, identified by whichever address the channel actually
// provides (email for Email/WebForm; phone for WhatsApp/SMS - see
// FindOrCreateCustomerByPhoneAsync). Deliberately distinct from Story 11's
// PortalAuthController.Register - neither method here ever sets or touches
// PasswordHash. A channel/web-form-created customer has no portal login until they
// separately register.
internal static class ChannelIntakeHelpers
{
    internal static async Task<Customer> FindOrCreateCustomerAsync(
        AppDbContext db, string fullName, string email, string? phone)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await db.Customers.SingleOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == normalizedEmail);
        if (existing is not null)
        {
            return existing;
        }

        // Phone is uniquely constrained (it identifies WhatsApp/SMS customers). A supplied
        // phone that already belongs to a different customer - e.g. this same person
        // previously contacted via WhatsApp/SMS under this exact phone number - must not be
        // duplicated onto this new, email-identified row. Email remains the identifying key
        // here regardless; leaving Phone unset in that case loses nothing (the other row
        // still has it) and avoids a unique-constraint failure on save.
        var phoneInUse = !string.IsNullOrWhiteSpace(phone) && await db.Customers.AnyAsync(c => c.Phone == phone);

        var customer = new Customer
        {
            FullName = fullName,
            Email = email,
            Phone = phoneInUse ? null : phone,
        };
        db.Customers.Add(customer);
        return customer;
    }

    // Story 12 fix (post-review): WhatsApp/SMS inbound messages identify a customer by
    // phone, never by email - the "From" address on those channels IS a phone number,
    // not an email. Reusing an existing Customer by Phone (regardless of how that phone
    // number first got attached to them - portal profile, staff-entered, or a prior
    // message on this same channel) avoids fragmenting one real person into two Customer
    // rows. Email is intentionally left null for a customer created this way; it is never
    // guessed or backfilled with the phone number.
    internal static async Task<Customer> FindOrCreateCustomerByPhoneAsync(
        AppDbContext db, string fullName, string phone)
    {
        var normalizedPhone = phone.Trim();
        var existing = await db.Customers.SingleOrDefaultAsync(c => c.Phone == normalizedPhone);
        if (existing is not null)
        {
            return existing;
        }

        var customer = new Customer
        {
            FullName = fullName,
            Email = null,
            Phone = normalizedPhone,
        };
        db.Customers.Add(customer);
        return customer;
    }
}
