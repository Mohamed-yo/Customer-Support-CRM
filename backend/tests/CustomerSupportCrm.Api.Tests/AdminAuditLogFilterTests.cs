using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

public class AdminAuditLogFilterTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AdminController NewController() =>
        new() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    private static async Task<IReadOnlyList<AdminController.AuditLogListItem>> GetItems(Task<IActionResult> action)
    {
        var result = await action;
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsAssignableFrom<IEnumerable<AdminController.AuditLogListItem>>(ok.Value).ToList();
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        var actorA = Guid.NewGuid();
        var actorB = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.AuditLogs.AddRange(
            new AuditLog { Action = "admin.user.create", Outcome = "success", ActorUserId = actorA, TimestampUtc = now.AddDays(-5) },
            new AuditLog { Action = "admin.user.deactivate", Outcome = "success", ActorUserId = actorA, TimestampUtc = now.AddDays(-1) },
            new AuditLog { Action = "admin.role.assign", Outcome = "success", ActorUserId = actorB, TimestampUtc = now.AddDays(-1) },
            new AuditLog { Action = "auth.login", Outcome = "failure", ActorUserId = actorB, TimestampUtc = now });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ListAuditLogs_FiltersByAction()
    {
        await using var db = NewDb();
        await SeedAsync(db);

        var items = await GetItems(NewController().ListAuditLogs(db, action: "admin.role.assign"));

        Assert.Single(items);
        Assert.Equal("admin.role.assign", items[0].Action);
    }

    [Fact]
    public async Task ListAuditLogs_FiltersByActorUserId()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var actorA = (await db.AuditLogs.Where(a => a.Action == "admin.user.create").SingleAsync()).ActorUserId!.Value;

        var items = await GetItems(NewController().ListAuditLogs(db, actorUserId: actorA));

        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal(actorA, i.ActorUserId));
    }

    [Fact]
    public async Task ListAuditLogs_FiltersByDateRange()
    {
        await using var db = NewDb();
        await SeedAsync(db);
        var now = DateTime.UtcNow;

        var items = await GetItems(NewController().ListAuditLogs(db, fromUtc: now.AddDays(-2), toUtc: now.AddHours(1)));

        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task ListAuditLogs_Paginates()
    {
        await using var db = NewDb();
        await SeedAsync(db);

        var page1 = await GetItems(NewController().ListAuditLogs(db, page: 1, pageSize: 2));
        var page2 = await GetItems(NewController().ListAuditLogs(db, page: 2, pageSize: 2));

        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.DoesNotContain(page1[0].Id, page2.Select(i => i.Id));
    }

    [Fact]
    public async Task ListAuditLogs_NoFilters_UsesTakeDefault()
    {
        await using var db = NewDb();
        await SeedAsync(db);

        var items = await GetItems(NewController().ListAuditLogs(db));

        Assert.Equal(4, items.Count);
        // Newest first.
        Assert.Equal("auth.login", items[0].Action);
    }
}
