using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

public class DepartmentsBranchesControllerTests
{
    private sealed class NoOpDispatcher : IOutboundWebhookDispatcher
    {
        public Task DispatchAsync(string eventType, object payload, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static DepartmentsController NewDepartmentsController() =>
        new() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    private static BranchesController NewBranchesController() =>
        new() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    [Fact]
    public async Task Department_CreateListDeactivateReactivate_RoundTrips()
    {
        await using var db = NewDb();
        var controller = NewDepartmentsController();

        var createResult = await controller.Create(new DepartmentUpsertRequest { Name = "Support" }, db);
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var item = Assert.IsType<DepartmentItem>(created.Value);
        Assert.True(item.IsActive);

        var listResult = await controller.List(db);
        var ok = Assert.IsType<OkObjectResult>(listResult);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<DepartmentItem>>(ok.Value));

        await controller.Deactivate(item.Id, db);
        Assert.False((await db.Departments.SingleAsync(d => d.Id == item.Id)).IsActive);

        await controller.Reactivate(item.Id, db);
        Assert.True((await db.Departments.SingleAsync(d => d.Id == item.Id)).IsActive);
    }

    [Fact]
    public async Task Department_DuplicateName_ReturnsConflict()
    {
        await using var db = NewDb();
        var controller = NewDepartmentsController();
        await controller.Create(new DepartmentUpsertRequest { Name = "Support" }, db);

        var result = await controller.Create(new DepartmentUpsertRequest { Name = "Support" }, db);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Branch_CreateListDeactivateReactivate_RoundTrips()
    {
        await using var db = NewDb();
        var controller = NewBranchesController();

        var createResult = await controller.Create(new BranchUpsertRequest { Name = "Downtown" }, db);
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var item = Assert.IsType<BranchItem>(created.Value);
        Assert.True(item.IsActive);

        await controller.Deactivate(item.Id, db);
        Assert.False((await db.Branches.SingleAsync(b => b.Id == item.Id)).IsActive);

        await controller.Reactivate(item.Id, db);
        Assert.True((await db.Branches.SingleAsync(b => b.Id == item.Id)).IsActive);
    }

    [Fact]
    public async Task Branch_DuplicateName_ReturnsConflict()
    {
        await using var db = NewDb();
        var controller = NewBranchesController();
        await controller.Create(new BranchUpsertRequest { Name = "Downtown" }, db);

        var result = await controller.Create(new BranchUpsertRequest { Name = "Downtown" }, db);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task User_CanBeAssignedDepartmentAndBranch_ViaAdminPatchUser()
    {
        await using var db = NewDb();
        var department = new Department { Name = "Support" };
        var branch = new Branch { Name = "Downtown" };
        var user = new User { Email = "agent@example.com", DisplayName = "Agent", PasswordHash = "x" };
        db.Departments.Add(department);
        db.Branches.Add(branch);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var adminController = new AdminController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var result = await adminController.PatchUser(
            user.Id,
            new AdminController.PatchUserRequest(null, department.Id, branch.Id),
            db,
            new AuditLogger(db, NullLogger<AuditLogger>.Instance));

        Assert.IsType<OkObjectResult>(result);
        var stored = await db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal(department.Id, stored.DepartmentId);
        Assert.Equal(branch.Id, stored.BranchId);
    }

    [Fact]
    public async Task Ticket_CanBeAssignedDepartmentAndBranch_ViaTicketsControllerCreate()
    {
        await using var db = NewDb();
        var department = new Department { Name = "Support" };
        var branch = new Branch { Name = "Downtown" };
        var customer = new Customer { FullName = "Customer" };
        db.Departments.Add(department);
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var ticketsController = new TicketsController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var request = new TicketUpsertRequest
        {
            CustomerId = customer.Id,
            Subject = "Needs routing",
            Status = "Open",
            Category = "General",
            Priority = "Normal",
            DepartmentId = department.Id,
            BranchId = branch.Id,
        };
        var runtimeSettings = new RuntimeSettingsService(db, new MemoryCache(new MemoryCacheOptions()));

        var result = await ticketsController.Create(
            request, db, new AuditLogger(db, NullLogger<AuditLogger>.Instance), new NoOpDispatcher(), runtimeSettings);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var item = Assert.IsType<TicketListItem>(created.Value);
        Assert.Equal(department.Id, item.DepartmentId);
        Assert.Equal(branch.Id, item.BranchId);
    }
}
