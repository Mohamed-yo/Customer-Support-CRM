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

// Direct action-call tests, same style as ApiKeysControllerTests.cs - see that file's
// header comment for what these can and cannot prove about RBAC.
public class AdminUsersControllerTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AdminController NewController(Guid actorUserId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim("sub", actorUserId.ToString()) }, "TestAuth"));
        return new AdminController { ControllerContext = new ControllerContext { HttpContext = httpContext } };
    }

    private static AuditLogger NewAudit(AppDbContext db) => new(db, NullLogger<AuditLogger>.Instance);

    [Fact]
    public async Task CreateUser_PersistsHashedPassword_NotPlaintext()
    {
        await using var db = NewDb();
        var hasher = new PasswordHasher<User>();
        var controller = NewController(Guid.NewGuid());

        var result = await controller.CreateUser(
            new AdminController.CreateUserRequest("New.Agent@Example.com", "New Agent", "Passw0rd!", null, null),
            db, hasher, NewAudit(db));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var item = Assert.IsType<AdminController.UserListItem>(created.Value);
        Assert.Equal("new.agent@example.com", item.Email);
        Assert.True(item.IsActive);

        var stored = await db.Users.SingleAsync(u => u.Id == item.Id);
        Assert.NotEqual("Passw0rd!", stored.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(stored, stored.PasswordHash, "Passw0rd!"));
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ReturnsConflict()
    {
        await using var db = NewDb();
        db.Users.Add(new User { Email = "dup@example.com", DisplayName = "Existing", PasswordHash = "x" });
        await db.SaveChangesAsync();

        var controller = NewController(Guid.NewGuid());
        var result = await controller.CreateUser(
            new AdminController.CreateUserRequest("dup@example.com", "Dup", "Passw0rd!", null, null),
            db, new PasswordHasher<User>(), NewAudit(db));

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task ListUsers_FiltersBySearchTerm()
    {
        await using var db = NewDb();
        db.Users.AddRange(
            new User { Email = "alice@example.com", DisplayName = "Alice Agent", PasswordHash = "x" },
            new User { Email = "bob@example.com", DisplayName = "Bob Builder", PasswordHash = "x" });
        await db.SaveChangesAsync();

        var controller = NewController(Guid.NewGuid());
        var result = await controller.ListUsers(db, "alice");

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<AdminController.UserListItem>>(ok.Value);
        Assert.Single(items);
        Assert.Equal("Alice Agent", items.Single().DisplayName);
    }

    [Fact]
    public async Task DeactivateUser_SetsIsActiveFalse_ThenReactivate_SetsItBackToTrue()
    {
        await using var db = NewDb();
        var user = new User { Email = "toggle@example.com", DisplayName = "Toggle", PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = NewController(Guid.NewGuid());
        await controller.DeactivateUser(user.Id, db, NewAudit(db));
        Assert.False((await db.Users.SingleAsync(u => u.Id == user.Id)).IsActive);

        await controller.ReactivateUser(user.Id, db, NewAudit(db));
        Assert.True((await db.Users.SingleAsync(u => u.Id == user.Id)).IsActive);
    }

    [Fact]
    public async Task DeactivateUser_CannotDeactivateSelf()
    {
        await using var db = NewDb();
        var actorId = Guid.NewGuid();
        var self = new User { Id = actorId, Email = "self@example.com", DisplayName = "Self", PasswordHash = "x" };
        db.Users.Add(self);
        await db.SaveChangesAsync();

        var controller = NewController(actorId);
        var result = await controller.DeactivateUser(actorId, db, NewAudit(db));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.True((await db.Users.SingleAsync(u => u.Id == actorId)).IsActive);
    }

    [Fact]
    public async Task AssignRoleById_ThenRemoveRole_RoundTrips()
    {
        await using var db = NewDb();
        var role = new Role { Name = "Agent" };
        var user = new User { Email = "roled@example.com", DisplayName = "Roled", PasswordHash = "x" };
        db.Roles.Add(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = NewController(Guid.NewGuid());
        await controller.AssignRoleById(user.Id, new AdminController.AssignRoleByIdRequest(role.Id), db, NewAudit(db));
        Assert.True(await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id));

        await controller.RemoveRole(user.Id, role.Id, db, NewAudit(db));
        Assert.False(await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id));
    }

    [Fact]
    public async Task RemoveRole_CannotRemoveOwnAdminRole()
    {
        await using var db = NewDb();
        var actorId = Guid.NewGuid();
        var adminRole = new Role { Name = "Admin" };
        var self = new User { Id = actorId, Email = "admin-self@example.com", DisplayName = "Admin Self", PasswordHash = "x" };
        db.Roles.Add(adminRole);
        db.Users.Add(self);
        db.UserRoles.Add(new UserRole { UserId = actorId, RoleId = adminRole.Id });
        await db.SaveChangesAsync();

        var controller = NewController(actorId);
        var result = await controller.RemoveRole(actorId, adminRole.Id, db, NewAudit(db));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(await db.UserRoles.AnyAsync(ur => ur.UserId == actorId && ur.RoleId == adminRole.Id));
    }

    [Fact]
    public void AdminController_HasAdminOnlyAuthorizationAttribute()
    {
        // Metadata-only check - see ApiKeysControllerTests.cs's header comment for what
        // this can and cannot prove.
        var attribute = typeof(AdminController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("RequireStaff", attribute!.Policy);
        Assert.Equal("Admin", attribute.Roles);
    }
}
