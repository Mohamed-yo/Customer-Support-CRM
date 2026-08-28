using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Story 12 fix (Important #2): WhatsApp/SMS inbound intake must identify/reuse a
// Customer by phone, never by writing the phone number into Customer.Email.
public class ChannelIntakeHelpersTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task FindOrCreateCustomerByPhoneAsync_CreatesNewCustomer_WithNullEmail()
    {
        await using var db = NewDb();

        var customer = await ChannelIntakeHelpers.FindOrCreateCustomerByPhoneAsync(
            db, "Jane Caller", "+15551234567");
        await db.SaveChangesAsync();

        Assert.Equal("+15551234567", customer.Phone);
        Assert.Null(customer.Email);
        Assert.Equal(1, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task FindOrCreateCustomerByPhoneAsync_ReusesExistingCustomer_OnRepeatContact()
    {
        await using var db = NewDb();
        var first = await ChannelIntakeHelpers.FindOrCreateCustomerByPhoneAsync(
            db, "Jane Caller", "+15551234567");
        await db.SaveChangesAsync();

        // A second inbound WhatsApp/SMS message from the same phone number must reuse the
        // same Customer row, not create a duplicate identity.
        var second = await ChannelIntakeHelpers.FindOrCreateCustomerByPhoneAsync(
            db, "Jane Caller", "+15551234567");
        await db.SaveChangesAsync();

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task FindOrCreateCustomerByPhoneAsync_ReusesCustomerWhoAlreadyHasAnEmail()
    {
        await using var db = NewDb();
        // Simulates a customer who already exists with a real email (e.g. entered by staff,
        // or registered via the portal) and who separately provided their phone number.
        var existing = new Customer { FullName = "Jane Caller", Email = "jane@example.com", Phone = "+15551234567" };
        db.Customers.Add(existing);
        await db.SaveChangesAsync();

        var found = await ChannelIntakeHelpers.FindOrCreateCustomerByPhoneAsync(
            db, "Jane Caller", "+15551234567");
        await db.SaveChangesAsync();

        // Contacting by WhatsApp/SMS must reuse this same identity, not fragment it into a
        // second Customer row, and must never overwrite the existing email.
        Assert.Equal(existing.Id, found.Id);
        Assert.Equal("jane@example.com", found.Email);
        Assert.Equal(1, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task FindOrCreateCustomerAsync_EmailLookup_IgnoresPhoneOnlyCustomers()
    {
        await using var db = NewDb();
        // A phone-only customer (Email is null) must never be matched or disturbed by the
        // email-based lookup used for the Email channel / Web Form.
        db.Customers.Add(new Customer { FullName = "Phone Only", Email = null, Phone = "+15559999999" });
        await db.SaveChangesAsync();

        var customer = await ChannelIntakeHelpers.FindOrCreateCustomerAsync(
            db, "New Emailer", "new@example.com", phone: null);
        await db.SaveChangesAsync();

        Assert.Equal("new@example.com", customer.Email);
        Assert.Equal(2, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task FindOrCreateCustomerAsync_NewCustomer_DoesNotDuplicatePhoneAlreadyOwnedByAnother()
    {
        await using var db = NewDb();
        // This phone number already identifies a different (WhatsApp-originated) customer.
        var whatsappCustomer = new Customer { FullName = "WhatsApp Contact", Email = null, Phone = "+15551234567" };
        db.Customers.Add(whatsappCustomer);
        await db.SaveChangesAsync();

        // A new Web Form submission with a different, new email but the same phone number
        // must not fail (Phone is uniquely constrained) and must not disturb the existing
        // WhatsApp customer's row.
        var webFormCustomer = await ChannelIntakeHelpers.FindOrCreateCustomerAsync(
            db, "Same Person, New Form", "person@example.com", "+15551234567");
        await db.SaveChangesAsync();

        Assert.NotEqual(whatsappCustomer.Id, webFormCustomer.Id);
        Assert.Null(webFormCustomer.Phone);
        Assert.Equal("+15551234567", (await db.Customers.FindAsync(whatsappCustomer.Id))!.Phone);
        Assert.Equal(2, await db.Customers.CountAsync());
    }
}
