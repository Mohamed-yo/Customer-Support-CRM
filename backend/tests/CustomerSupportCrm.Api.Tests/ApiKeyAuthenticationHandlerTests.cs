using System.Text.Encodings.Web;
using CustomerSupportCrm.Api.Auth;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Unit-level only: AuthenticationHandler<TOptions> exposes public InitializeAsync/
// AuthenticateAsync, so the handler's own logic is testable without a running HTTP
// server or WebApplicationFactory. This proves the handler's internal logic - it does
// NOT prove that a real HTTP request through the ASP.NET Core pipeline produces a
// 401/200 response; that is manual/runtime verification (see the story plan).
public class ApiKeyAuthenticationHandlerTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<AuthenticateResult> AuthenticateAsync(AppDbContext db, string? headerValue)
    {
        var hasher = new PasswordHasher<ApiKey>();
        var handler = new ApiKeyAuthenticationHandler(
            new OptionsMonitorStub(),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            db,
            hasher);

        var httpContext = new DefaultHttpContext();
        if (headerValue is not null)
        {
            httpContext.Request.Headers["X-Api-Key"] = headerValue;
        }

        var scheme = new AuthenticationScheme(
            ApiKeyAuthenticationHandler.SchemeName, ApiKeyAuthenticationHandler.SchemeName, typeof(ApiKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, httpContext);
        return await handler.AuthenticateAsync();
    }

    private sealed class OptionsMonitorStub : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue { get; } = new();
        public AuthenticationSchemeOptions Get(string? name) => CurrentValue;
        public IDisposable OnChange(Action<AuthenticationSchemeOptions, string> listener) => new NoopDisposable();
        private sealed class NoopDisposable : IDisposable { public void Dispose() { } }
    }

    private static (ApiKey entity, string plaintext) CreateKey(PasswordHasher<ApiKey> hasher, bool revoked = false)
    {
        const string plaintext = "csk_unit-test-plaintext-secret-value";
        var entity = new ApiKey
        {
            Label = "Test Key",
            Prefix = plaintext[..12],
            RevokedAtUtc = revoked ? DateTime.UtcNow : null,
        };
        entity.KeyHash = hasher.HashPassword(entity, plaintext);
        return (entity, plaintext);
    }

    [Fact]
    public async Task MissingHeader_ReturnsNoResult()
    {
        await using var db = NewDb();
        var result = await AuthenticateAsync(db, headerValue: null);
        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task UnknownKey_ReturnsFail()
    {
        await using var db = NewDb();
        var result = await AuthenticateAsync(db, "csk_no-such-key-at-all-1234567890");
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task RevokedKey_ReturnsFail()
    {
        await using var db = NewDb();
        var hasher = new PasswordHasher<ApiKey>();
        var (entity, plaintext) = CreateKey(hasher, revoked: true);
        db.ApiKeys.Add(entity);
        await db.SaveChangesAsync();

        var result = await AuthenticateAsync(db, plaintext);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task ValidKey_Succeeds_AndUpdatesLastUsedAtUtc()
    {
        await using var db = NewDb();
        var hasher = new PasswordHasher<ApiKey>();
        var (entity, plaintext) = CreateKey(hasher);
        db.ApiKeys.Add(entity);
        await db.SaveChangesAsync();

        var result = await AuthenticateAsync(db, plaintext);

        Assert.True(result.Succeeded);
        Assert.Equal(entity.Label, result.Principal!.Identity!.Name);
        Assert.Equal("true", result.Principal.FindFirst("external_client")?.Value);

        var reloaded = await db.ApiKeys.SingleAsync(k => k.Id == entity.Id);
        Assert.NotNull(reloaded.LastUsedAtUtc);
    }
}
