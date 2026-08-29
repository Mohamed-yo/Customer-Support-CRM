using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Auth;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Direct action-call test of AuthController.Login's own internal IsActive branch - it does
// not run through the ASP.NET Core pipeline, so it cannot prove the live HTTP 401 response;
// see the story plan's Manual/Runtime Verification for that.
public class AuthControllerDeactivatedUserTests
{
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

    private static AuthController NewController(AppDbContext db, PasswordHasher<User> hasher) =>
        new(db, hasher, new JwtTokenService(NewConfig()), new AuditLogger(db, NullLogger<AuditLogger>.Instance))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

    [Fact]
    public async Task Login_DeactivatedUser_CorrectPassword_ReturnsSameStableCodeAsInvalidCredentials()
    {
        await using var db = NewDb();
        var hasher = new PasswordHasher<User>();
        var user = new User { Email = "deactivated@example.com", DisplayName = "Deactivated", IsActive = false };
        user.PasswordHash = hasher.HashPassword(user, "Passw0rd!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = NewController(db, hasher);
        var result = await controller.Login(new LoginRequest("deactivated@example.com", "Passw0rd!"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var error = unauthorized.Value!.GetType().GetProperty("error")!.GetValue(unauthorized.Value);
        Assert.Equal("account_deactivated", error);
    }

    [Fact]
    public async Task Login_DeactivatedUser_WrongPassword_ReturnsGenericInvalidCredentials_NotLeakingDeactivation()
    {
        await using var db = NewDb();
        var hasher = new PasswordHasher<User>();
        var user = new User { Email = "deactivated2@example.com", DisplayName = "Deactivated", IsActive = false };
        user.PasswordHash = hasher.HashPassword(user, "Passw0rd!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = NewController(db, hasher);
        var result = await controller.Login(new LoginRequest("deactivated2@example.com", "WrongPassword!"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var error = unauthorized.Value!.GetType().GetProperty("error")!.GetValue(unauthorized.Value);
        // Same code as any wrong password, regardless of active status - a wrong-password
        // attempt must never reveal whether the account is deactivated.
        Assert.Equal("invalid_credentials", error);
    }

    [Fact]
    public async Task Login_ActiveUser_CorrectPassword_Succeeds()
    {
        await using var db = NewDb();
        var hasher = new PasswordHasher<User>();
        var user = new User { Email = "active@example.com", DisplayName = "Active", IsActive = true };
        user.PasswordHash = hasher.HashPassword(user, "Passw0rd!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = NewController(db, hasher);
        var result = await controller.Login(new LoginRequest("active@example.com", "Passw0rd!"));

        Assert.IsType<OkObjectResult>(result);
    }
}
