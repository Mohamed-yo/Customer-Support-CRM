using System.Text.RegularExpressions;
using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Auth;
using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Portal registration must never grant a login to someone who hasn't proven they own the
// mailbox - in particular it must not let a caller claim a staff/channel-created customer
// record (and that customer's tickets) just by knowing its email.
public class PortalAuthVerificationTests
{
    private const string Password = "Passw0rd!";

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IConfiguration NewConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:SigningKey"] = "test-only-signing-key-at-least-32-characters-long",
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build();

    private sealed class CapturingEmailSender : IChannelSender
    {
        public List<(string To, string Body)> Sent { get; } = new();

        public Task<SendResult> SendAsync(string to, string? subject, string body, CancellationToken ct = default)
        {
            Sent.Add((to, body));
            return Task.FromResult(new SendResult(SendStatus.Success));
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static PortalAuthController NewController(AppDbContext db) =>
        new(
            db,
            new PasswordHasher<Customer>(),
            new JwtTokenService(NewConfig()),
            new AuditLogger(db, NullLogger<AuditLogger>.Instance),
            Options.Create(new PortalOptions { VerifyEmailUrl = "http://portal.test/verify-email" }),
            new TestHostEnvironment(),
            NullLogger<PortalAuthController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

    private static string ExtractToken(string emailBody) =>
        Regex.Match(emailBody, "token=([0-9a-f]{64})").Groups[1].Value;

    private static object? ErrorOf(IActionResult result) =>
        ((ObjectResult)result).Value!.GetType().GetProperty("error")!.GetValue(((ObjectResult)result).Value);

    [Fact]
    public async Task Register_ExistingPasswordlessCustomer_DoesNotGrantLogin()
    {
        await using var db = NewDb();
        var victim = new Customer { FullName = "Victim", Email = "victim@example.com" };
        db.Customers.Add(victim);
        await db.SaveChangesAsync();

        var sender = new CapturingEmailSender();
        var controller = NewController(db);
        var result = await controller.Register(new CustomerRegisterRequest("Attacker", "victim@example.com", null), sender);

        Assert.IsType<AcceptedResult>(result);
        // The link goes to the record's own mailbox, and nothing about the record changed.
        var sent = Assert.Single(sender.Sent);
        Assert.Equal("victim@example.com", sent.To);
        var stored = await db.Customers.SingleAsync(c => c.Id == victim.Id);
        Assert.Null(stored.PasswordHash);
        Assert.Equal("Victim", stored.FullName);

        var login = await controller.Login(new CustomerLoginRequest("victim@example.com", Password));
        Assert.IsType<UnauthorizedObjectResult>(login);
    }

    [Fact]
    public async Task VerifyEmail_WithEmailedToken_SetsPasswordAndSignsIn()
    {
        await using var db = NewDb();
        var sender = new CapturingEmailSender();
        var controller = NewController(db);
        await controller.Register(new CustomerRegisterRequest("New Customer", " New@Example.com ", null), sender);

        var token = ExtractToken(Assert.Single(sender.Sent).Body);
        var result = await controller.VerifyEmail(new CustomerVerifyEmailRequest(token, Password));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CustomerLoginResponse>(ok.Value);
        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Equal("New@Example.com", response.Email);

        var login = await controller.Login(new CustomerLoginRequest("new@example.com", Password));
        Assert.IsType<OkObjectResult>(login);
    }

    [Fact]
    public async Task VerifyEmail_TokenIsSingleUse()
    {
        await using var db = NewDb();
        var sender = new CapturingEmailSender();
        var controller = NewController(db);
        await controller.Register(new CustomerRegisterRequest("Customer", "once@example.com", null), sender);
        var token = ExtractToken(sender.Sent[0].Body);

        Assert.IsType<OkObjectResult>(await controller.VerifyEmail(new CustomerVerifyEmailRequest(token, Password)));
        var second = await controller.VerifyEmail(new CustomerVerifyEmailRequest(token, "AnotherPassw0rd!"));

        Assert.IsType<BadRequestObjectResult>(second);
        Assert.Equal("token_invalid", ErrorOf(second));
    }

    [Fact]
    public async Task VerifyEmail_ExpiredToken_IsRejected()
    {
        await using var db = NewDb();
        var sender = new CapturingEmailSender();
        var controller = NewController(db);
        await controller.Register(new CustomerRegisterRequest("Customer", "late@example.com", null), sender);
        var token = ExtractToken(sender.Sent[0].Body);

        var customer = await db.Customers.SingleAsync(c => c.Email == "late@example.com");
        customer.EmailVerificationExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var result = await controller.VerifyEmail(new CustomerVerifyEmailRequest(token, Password));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("token_invalid", ErrorOf(result));
        Assert.Null((await db.Customers.SingleAsync(c => c.Id == customer.Id)).PasswordHash);
    }

    [Fact]
    public async Task Register_Again_InvalidatesPreviousLink()
    {
        await using var db = NewDb();
        var sender = new CapturingEmailSender();
        var controller = NewController(db);
        await controller.Register(new CustomerRegisterRequest("Customer", "twice@example.com", null), sender);
        await controller.Register(new CustomerRegisterRequest("Customer", "twice@example.com", null), sender);
        var firstToken = ExtractToken(sender.Sent[0].Body);
        var secondToken = ExtractToken(sender.Sent[1].Body);

        var first = await controller.VerifyEmail(new CustomerVerifyEmailRequest(firstToken, Password));
        Assert.IsType<BadRequestObjectResult>(first);
        Assert.Equal("token_invalid", ErrorOf(first));

        Assert.IsType<OkObjectResult>(await controller.VerifyEmail(new CustomerVerifyEmailRequest(secondToken, Password)));
    }

    [Fact]
    public async Task Register_EmailThatAlreadyHasPassword_Returns409()
    {
        await using var db = NewDb();
        var hasher = new PasswordHasher<Customer>();
        var existing = new Customer { FullName = "Existing", Email = "taken@example.com" };
        existing.PasswordHash = hasher.HashPassword(existing, Password);
        db.Customers.Add(existing);
        await db.SaveChangesAsync();

        var sender = new CapturingEmailSender();
        var result = await NewController(db).Register(new CustomerRegisterRequest("Someone", "TAKEN@example.com", null), sender);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("email_already_registered", ErrorOf(result));
        Assert.Empty(sender.Sent);
    }
}
