using System.Security.Claims;
using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

public class DashboardControllerTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IRuntimeSettings NewRuntimeSettings(AppDbContext db) =>
        new RuntimeSettingsService(db, new MemoryCache(new MemoryCacheOptions()));

    private static DashboardController NewController(Guid actorUserId, bool isAdmin = false)
    {
        var claims = new List<Claim> { new("sub", actorUserId.ToString()) };
        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
        return new DashboardController { ControllerContext = new ControllerContext { HttpContext = httpContext } };
    }

    private static async Task<DashboardResponseDto> GetResponseAsync(Task<IActionResult> action)
    {
        var result = await action;
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<DashboardResponseDto>(ok.Value);
    }

    [Fact]
    public async Task Get_ReturnsKpis_ForStaffCaller()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Customer" };
        db.Customers.Add(customer);
        db.Tickets.AddRange(
            new Ticket { CustomerId = customer.Id, Subject = "A", Status = "Open" },
            new Ticket { CustomerId = customer.Id, Subject = "B", Status = "InProgress" },
            new Ticket { CustomerId = customer.Id, Subject = "C", Status = "Closed" },
            new Ticket { CustomerId = customer.Id, Subject = "D", Status = "Closed" });
        await db.SaveChangesAsync();

        var response = await GetResponseAsync(NewController(Guid.NewGuid()).Get(db, NewRuntimeSettings(db)));

        Assert.Equal(4, response.Kpis.TotalTickets);
        Assert.Equal(1, response.Kpis.OpenTickets);
        Assert.Equal(1, response.Kpis.InProgressTickets);
        Assert.Equal(2, response.Kpis.ClosedTickets);
    }

    [Fact]
    public async Task Get_ComputesEscalatedViaComputeIsEscalated()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Customer" };
        db.Customers.Add(customer);
        var now = DateTime.UtcNow;
        // Urgent: response due +1h. Created 5h ago, never responded -> breached/escalated.
        db.Tickets.Add(new Ticket
        {
            CustomerId = customer.Id, Subject = "Escalated", Priority = "Urgent", Status = "Open",
            CreatedAtUtc = now.AddHours(-5),
        });
        // Urgent, created 10 minutes ago - not yet due -> not escalated.
        db.Tickets.Add(new Ticket
        {
            CustomerId = customer.Id, Subject = "Pending", Priority = "Urgent", Status = "Open",
            CreatedAtUtc = now.AddMinutes(-10),
        });
        await db.SaveChangesAsync();

        var response = await GetResponseAsync(NewController(Guid.NewGuid()).Get(db, NewRuntimeSettings(db)));

        Assert.Equal(1, response.Kpis.EscalatedTickets);
    }

    [Fact]
    public async Task Get_MyWork_IsCallerScoped()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Customer" };
        var caller = new User { Email = "caller@example.com", DisplayName = "Caller", PasswordHash = "x" };
        var other = new User { Email = "other@example.com", DisplayName = "Other", PasswordHash = "x" };
        db.Customers.Add(customer);
        db.Users.AddRange(caller, other);
        db.Tickets.AddRange(
            new Ticket { CustomerId = customer.Id, Subject = "Mine-Open", Status = "Open", AssignedToUserId = caller.Id },
            new Ticket { CustomerId = customer.Id, Subject = "Mine-Closed", Status = "Closed", AssignedToUserId = caller.Id },
            new Ticket { CustomerId = customer.Id, Subject = "Others-Open", Status = "Open", AssignedToUserId = other.Id });
        db.Notifications.Add(new Notification { UserId = caller.Id, Type = "Assigned", Message = "x", IsRead = false });
        db.Notifications.Add(new Notification { UserId = other.Id, Type = "Assigned", Message = "x", IsRead = false });
        await db.SaveChangesAsync();

        var response = await GetResponseAsync(NewController(caller.Id).Get(db, NewRuntimeSettings(db)));

        // Deliberately narrower than the /tickets "Mine" toggle: non-closed only (AC5).
        Assert.Equal(1, response.MyWork.MyAssignedOpenCount);
        Assert.Single(response.MyWork.MyRecentAssignedTickets);
        Assert.Equal("Mine-Open", response.MyWork.MyRecentAssignedTickets[0].Subject);
        Assert.Equal(1, response.MyWork.MyUnreadNotificationCount);
    }

    [Fact]
    public async Task Get_MyOutstandingTasks_JoinsThroughTicketAssignment()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Customer" };
        var caller = new User { Email = "caller@example.com", DisplayName = "Caller", PasswordHash = "x" };
        var other = new User { Email = "other@example.com", DisplayName = "Other", PasswordHash = "x" };
        db.Customers.Add(customer);
        db.Users.AddRange(caller, other);
        var myTicket = new Ticket { CustomerId = customer.Id, Subject = "Mine", AssignedToUserId = caller.Id };
        var othersTicket = new Ticket { CustomerId = customer.Id, Subject = "Others", AssignedToUserId = other.Id };
        db.Tickets.AddRange(myTicket, othersTicket);
        db.TicketTasks.Add(new TicketTask { TicketId = myTicket.Id, Title = "My task", IsDone = false });
        db.TicketTasks.Add(new TicketTask { TicketId = othersTicket.Id, Title = "Others task", IsDone = false });
        await db.SaveChangesAsync();

        var response = await GetResponseAsync(NewController(caller.Id).Get(db, NewRuntimeSettings(db)));

        Assert.Single(response.MyWork.MyOutstandingTasks);
        Assert.Equal("My task", response.MyWork.MyOutstandingTasks[0].Title);
    }

    [Fact]
    public async Task Get_AdminSummary_IsNullForAgent()
    {
        await using var db = NewDb();

        var response = await GetResponseAsync(NewController(Guid.NewGuid(), isAdmin: false).Get(db, NewRuntimeSettings(db)));

        Assert.Null(response.AdminSummary);
    }

    [Fact]
    public async Task Get_AdminSummary_PopulatedForAdmin()
    {
        await using var db = NewDb();
        var role = new Role { Name = "Agent" };
        db.Roles.Add(role);
        var agent1 = new User { Email = "agent1@example.com", DisplayName = "Agent One", PasswordHash = "x" };
        var agent2 = new User { Email = "agent2@example.com", DisplayName = "Agent Two", PasswordHash = "x" };
        db.Users.AddRange(agent1, agent2);
        db.UserRoles.AddRange(
            new UserRole { UserId = agent1.Id, RoleId = role.Id },
            new UserRole { UserId = agent2.Id, RoleId = role.Id });
        var customer = new Customer { FullName = "Customer" };
        db.Customers.Add(customer);
        db.Tickets.AddRange(
            new Ticket { CustomerId = customer.Id, Subject = "A1-Closed-1", Status = "Closed", AssignedToUserId = agent1.Id },
            new Ticket { CustomerId = customer.Id, Subject = "A1-Closed-2", Status = "Closed", AssignedToUserId = agent1.Id },
            new Ticket { CustomerId = customer.Id, Subject = "A2-Closed", Status = "Closed", AssignedToUserId = agent2.Id },
            new Ticket { CustomerId = customer.Id, Subject = "Unassigned", Status = "Open", AssignedToUserId = null });
        await db.SaveChangesAsync();

        var response = await GetResponseAsync(NewController(Guid.NewGuid(), isAdmin: true).Get(db, NewRuntimeSettings(db)));

        Assert.NotNull(response.AdminSummary);
        Assert.Equal(1, response.AdminSummary!.UnassignedOpenCount);
        Assert.True(response.AdminSummary.TopAgents.Count <= 5);
        Assert.Equal(2, response.AdminSummary.TopAgents.Count);
        // agent1 (2 resolved) must be ranked ahead of agent2 (1 resolved).
        Assert.Equal(agent1.Id, response.AdminSummary.TopAgents[0].UserId);
        Assert.Equal(2, response.AdminSummary.TopAgents[0].ResolvedCount);
    }

    [Fact]
    public async Task Get_AdminSummary_TopAgents_AverageSatisfaction_NullWhenNoFeedback()
    {
        await using var db = NewDb();
        var role = new Role { Name = "Agent" };
        db.Roles.Add(role);
        var agent = new User { Email = "agent@example.com", DisplayName = "Agent", PasswordHash = "x" };
        db.Users.Add(agent);
        db.UserRoles.Add(new UserRole { UserId = agent.Id, RoleId = role.Id });
        var customer = new Customer { FullName = "Customer" };
        db.Customers.Add(customer);
        db.Tickets.Add(new Ticket { CustomerId = customer.Id, Subject = "A", Status = "Closed", AssignedToUserId = agent.Id });
        await db.SaveChangesAsync();

        var response = await GetResponseAsync(NewController(Guid.NewGuid(), isAdmin: true).Get(db, NewRuntimeSettings(db)));

        var agentRow = response.AdminSummary!.TopAgents.Single(a => a.UserId == agent.Id);
        Assert.Null(agentRow.AverageSatisfaction);
    }
}
