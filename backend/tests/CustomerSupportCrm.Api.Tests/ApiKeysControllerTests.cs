using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Direct action-call unit/controller-logic tests, same style as
// ChannelsControllerInboundTests.cs / ReportsControllerTests.cs. These never pass through
// ASP.NET Core's authorization middleware, so they cannot and do not claim to prove a
// live 403/401 HTTP response - see ApiKeysController_HasAdminOnlyAuthorizationAttribute
// below for what they DO prove about RBAC, and the story plan's Manual/Runtime
// Verification section for the live-response check.
public class ApiKeysControllerTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ApiKeysController NewController(Guid actorUserId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim("sub", actorUserId.ToString()) }, "TestAuth"));
        return new ApiKeysController { ControllerContext = new ControllerContext { HttpContext = httpContext } };
    }

    [Fact]
    public async Task Create_ReturnsPlaintextOnce_AndPersistsOnlyTheHash()
    {
        await using var db = NewDb();
        var hasher = new PasswordHasher<ApiKey>();
        var actorId = Guid.NewGuid();
        var controller = NewController(actorId);

        var result = await controller.Create(
            new CreateApiKeyRequest { Label = "Partner Integration" },
            db, hasher, new AuditLogger(db, NullLogger<AuditLogger>.Instance));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<CreateApiKeyResponse>(created.Value);

        Assert.Equal("Partner Integration", response.Label);
        Assert.False(string.IsNullOrEmpty(response.PlaintextKey));
        Assert.StartsWith(response.Prefix, response.PlaintextKey);

        var stored = await db.ApiKeys.SingleAsync(k => k.Id == response.Id);
        Assert.NotEqual(response.PlaintextKey, stored.KeyHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(stored, stored.KeyHash, response.PlaintextKey));
        Assert.Equal(actorId, stored.CreatedByUserId);
    }

    [Fact]
    public async Task List_NeverExposesHashOrPlaintext()
    {
        await using var db = NewDb();
        var hasher = new PasswordHasher<ApiKey>();
        var key = new ApiKey { Label = "Existing", Prefix = "csk_abcdefgh" };
        key.KeyHash = hasher.HashPassword(key, "irrelevant-plaintext");
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync();

        var controller = NewController(Guid.NewGuid());
        var result = await controller.List(db);

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<ApiKeyListItem>>(ok.Value);
        var item = Assert.Single(items);

        Assert.Equal(key.Label, item.Label);
        Assert.Equal(key.Prefix, item.Prefix);
        Assert.True(item.IsActive);
        // ApiKeyListItem has no property that could carry a hash or plaintext - the type
        // itself is the guarantee, not a runtime check.
    }

    [Fact]
    public async Task Revoke_SetsRevokedAtUtc()
    {
        await using var db = NewDb();
        var hasher = new PasswordHasher<ApiKey>();
        var key = new ApiKey { Label = "To Revoke", Prefix = "csk_ijklmnop" };
        key.KeyHash = hasher.HashPassword(key, "irrelevant-plaintext");
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync();

        var controller = NewController(Guid.NewGuid());
        var result = await controller.Revoke(key.Id, db, new AuditLogger(db, NullLogger<AuditLogger>.Instance));

        Assert.IsType<NoContentResult>(result);
        var reloaded = await db.ApiKeys.SingleAsync(k => k.Id == key.Id);
        Assert.NotNull(reloaded.RevokedAtUtc);
        Assert.False(reloaded.IsActive);
    }

    [Fact]
    public async Task Revoke_UnknownOrAlreadyRevoked_ReturnsNotFound()
    {
        await using var db = NewDb();
        var controller = NewController(Guid.NewGuid());

        var unknown = await controller.Revoke(Guid.NewGuid(), db, new AuditLogger(db, NullLogger<AuditLogger>.Instance));
        Assert.IsType<NotFoundObjectResult>(unknown);

        var hasher = new PasswordHasher<ApiKey>();
        var key = new ApiKey { Label = "Already Revoked", Prefix = "csk_qrstuvwx", RevokedAtUtc = DateTime.UtcNow };
        key.KeyHash = hasher.HashPassword(key, "irrelevant-plaintext");
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync();

        var alreadyRevoked = await controller.Revoke(key.Id, db, new AuditLogger(db, NullLogger<AuditLogger>.Instance));
        Assert.IsType<NotFoundObjectResult>(alreadyRevoked);
    }

    [Fact]
    public void ApiKeysController_HasAdminOnlyAuthorizationAttribute()
    {
        // Metadata-only check: does NOT prove a live request from a non-Admin staff
        // token returns 403, or that an anonymous request returns 401 - this test
        // harness calls controller actions as plain C# methods, never through ASP.NET
        // Core's authorization middleware. It confirms the attribute that IS the real
        // security boundary is present with the expected values. Genuine 403/401
        // verification is manual (see the story plan's Manual/Runtime Verification).
        var attribute = typeof(ApiKeysController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("RequireStaff", attribute!.Policy);
        Assert.Equal("Admin", attribute.Roles);
    }
}
