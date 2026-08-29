using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

public class ReportsControllerTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Story 15: ReportsController's SLA endpoints now resolve targets through
    // IRuntimeSettings - a real RuntimeSettingsService (backed by the same InMemory db and
    // a fresh cache) exercises the exact fallback-to-default path these pre-Story-15 tests
    // already assert on.
    private static IRuntimeSettings NewRuntimeSettings(AppDbContext db) =>
        new RuntimeSettingsService(db, new MemoryCache(new MemoryCacheOptions()));

    private static ReportsController NewController() =>
        new() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    private static Customer NewCustomer() => new() { FullName = "Test Customer", Email = $"{Guid.NewGuid()}@example.com" };

    private static async Task<T> GetValue<T>(Task<IActionResult> action)
    {
        var result = await action;
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<T>(ok.Value);
    }

    [Fact]
    public async Task GetTicketCounts_ReturnsGroupedCounts()
    {
        await using var db = NewDb();
        var customer = NewCustomer();
        db.Customers.Add(customer);
        db.Tickets.AddRange(
            new Ticket { CustomerId = customer.Id, Subject = "A", Status = "Open", Category = "General", Priority = "Normal", Source = "Manual" },
            new Ticket { CustomerId = customer.Id, Subject = "B", Status = "Closed", Category = "Billing", Priority = "High", Source = "Portal" },
            new Ticket { CustomerId = customer.Id, Subject = "C", Status = "Closed", Category = "Billing", Priority = "High", Source = "Portal" });
        await db.SaveChangesAsync();

        var report = await GetValue<TicketCountsReport>(
            NewController().GetTicketCounts(db, new ReportDateRangeQuery(null, null)));

        Assert.Equal(3, report.Total);
        Assert.Equal(1, report.ByStatus["Open"]);
        Assert.Equal(2, report.ByStatus["Closed"]);
        Assert.Equal(0, report.ByStatus["InProgress"]);
        Assert.Equal(2, report.ByCategory["Billing"]);
        Assert.Equal(1, report.ByCategory["General"]);
        Assert.Equal(2, report.BySource["Portal"]);
        Assert.Equal(1, report.BySource["Manual"]);
    }

    [Fact]
    public async Task GetTicketCounts_RespectsDateRange()
    {
        await using var db = NewDb();
        var customer = NewCustomer();
        db.Customers.Add(customer);
        var now = DateTime.UtcNow;
        db.Tickets.AddRange(
            new Ticket { CustomerId = customer.Id, Subject = "InRange", CreatedAtUtc = now.AddDays(-1) },
            new Ticket { CustomerId = customer.Id, Subject = "OutOfRange", CreatedAtUtc = now.AddDays(-10) });
        await db.SaveChangesAsync();

        var range = new ReportDateRangeQuery(now.AddDays(-3), now);
        var report = await GetValue<TicketCountsReport>(NewController().GetTicketCounts(db, range));

        Assert.Equal(1, report.Total);
    }

    [Fact]
    public async Task GetSla_ComputesMetAndBreachedUsingTicketsControllerHelpers()
    {
        await using var db = NewDb();
        var customer = NewCustomer();
        db.Customers.Add(customer);
        var now = DateTime.UtcNow;

        // Urgent: response due = +1h, resolution due = +4h.
        var metTicket = new Ticket
        {
            CustomerId = customer.Id, Subject = "Met", Priority = "Urgent", Status = "Closed",
            CreatedAtUtc = now.AddHours(-5), FirstRespondedAtUtc = now.AddHours(-4.5), ResolvedAtUtc = now.AddHours(-2),
        };
        var breachedRespondedLate = new Ticket
        {
            CustomerId = customer.Id, Subject = "BreachedLate", Priority = "Urgent", Status = "Open",
            CreatedAtUtc = now.AddHours(-5), FirstRespondedAtUtc = now.AddHours(-2),
        };
        var breachedNeverResponded = new Ticket
        {
            CustomerId = customer.Id, Subject = "BreachedNever", Priority = "Urgent", Status = "Open",
            CreatedAtUtc = now.AddHours(-5),
        };
        var pending = new Ticket
        {
            CustomerId = customer.Id, Subject = "Pending", Priority = "Urgent", Status = "Open",
            CreatedAtUtc = now.AddMinutes(-10),
        };
        db.Tickets.AddRange(metTicket, breachedRespondedLate, breachedNeverResponded, pending);
        await db.SaveChangesAsync();

        var report = await GetValue<SlaPerformanceReport>(
            NewController().GetSlaPerformance(db, NewRuntimeSettings(db), new ReportDateRangeQuery(null, null)));

        Assert.Equal(4, report.TotalConsidered);
        Assert.Equal(1, report.ResponseMet);
        Assert.Equal(2, report.ResponseBreached);
        Assert.True(report.AverageResponseMinutes > 0);
    }

    [Fact]
    public async Task GetSla_EscalatedCountMatchesComputeIsEscalated()
    {
        await using var db = NewDb();
        var customer = NewCustomer();
        db.Customers.Add(customer);
        var now = DateTime.UtcNow;

        // Urgent, never responded, created 2 hours ago -> response due (1h) has passed:
        // both breached-response AND escalated (status still Open, not Closed).
        db.Tickets.Add(new Ticket
        {
            CustomerId = customer.Id, Subject = "Escalated", Priority = "Urgent", Status = "Open",
            CreatedAtUtc = now.AddHours(-2),
        });
        await db.SaveChangesAsync();

        var report = await GetValue<SlaPerformanceReport>(
            NewController().GetSlaPerformance(db, NewRuntimeSettings(db), new ReportDateRangeQuery(null, null)));

        Assert.Equal(1, report.EscalatedCount);
    }

    [Fact]
    public async Task GetAgents_AggregatesPerAgentMetrics()
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
        var customer = NewCustomer();
        db.Customers.Add(customer);
        var now = DateTime.UtcNow;
        db.Tickets.AddRange(
            new Ticket { CustomerId = customer.Id, Subject = "A1-Open", Status = "Open", AssignedToUserId = agent1.Id },
            new Ticket
            {
                CustomerId = customer.Id, Subject = "A1-Closed", Status = "Closed", AssignedToUserId = agent1.Id,
                CreatedAtUtc = now.AddHours(-3), ResolvedAtUtc = now.AddHours(-1),
            },
            new Ticket { CustomerId = customer.Id, Subject = "Unassigned", Status = "Open", AssignedToUserId = null });
        await db.SaveChangesAsync();

        var report = await GetValue<AgentPerformanceReport>(
            NewController().GetAgentPerformance(db, new ReportDateRangeQuery(null, null)));

        Assert.Equal(2, report.Agents.Count); // both agents present, even agent2 with zero tickets
        var agent1Row = report.Agents.Single(a => a.UserId == agent1.Id);
        Assert.Equal(1, agent1Row.Open);
        Assert.Equal(1, agent1Row.Closed);
        Assert.Equal(1, agent1Row.Resolved);
        Assert.True(agent1Row.AverageResolutionMinutes > 0);
        var agent2Row = report.Agents.Single(a => a.UserId == agent2.Id);
        Assert.Equal(0, agent2Row.Open);
        Assert.Equal(0, agent2Row.Closed);
    }

    [Fact]
    public async Task GetSatisfaction_ComputesAveragesDistributionAndResponseRate()
    {
        await using var db = NewDb();
        var customer = NewCustomer();
        db.Customers.Add(customer);
        var t1 = new Ticket { CustomerId = customer.Id, Subject = "T1", Status = "Closed", Category = "Billing" };
        var t2 = new Ticket { CustomerId = customer.Id, Subject = "T2", Status = "Closed", Category = "Technical" };
        var t3 = new Ticket { CustomerId = customer.Id, Subject = "T3", Status = "Closed", Category = "General" };
        db.Tickets.AddRange(t1, t2, t3);
        db.TicketFeedbacks.AddRange(
            new TicketFeedback { TicketId = t1.Id, CustomerId = customer.Id, Rating = 5 },
            new TicketFeedback { TicketId = t2.Id, CustomerId = customer.Id, Rating = 3 });
        await db.SaveChangesAsync();

        var report = await GetValue<SatisfactionReport>(
            NewController().GetSatisfaction(db, new ReportDateRangeQuery(null, null)));

        Assert.Equal(2, report.FeedbackCount);
        Assert.Equal(3, report.ClosedTicketCount);
        Assert.Equal(4, report.AverageRating); // (5+3)/2
        Assert.Equal(1, report.Distribution.Single(d => d.Rating == 5).Count);
        Assert.Equal(1, report.Distribution.Single(d => d.Rating == 3).Count);
        Assert.Equal(0, report.Distribution.Single(d => d.Rating == 1).Count);
        Assert.Equal(5, report.Distribution.Count); // ratings 1-5 all present, zero-filled
        Assert.True(Math.Abs(report.ResponseRatePercent - (2 * 100.0 / 3)) < 0.001);
    }

    [Fact]
    public async Task GetDashboard_ComposesAllFourReports()
    {
        await using var db = NewDb();
        var customer = NewCustomer();
        db.Customers.Add(customer);
        db.Tickets.Add(new Ticket { CustomerId = customer.Id, Subject = "Only", Status = "Open" });
        await db.SaveChangesAsync();

        var report = await GetValue<ManagementDashboardReport>(
            NewController().GetDashboard(db, NewRuntimeSettings(db), new ReportDateRangeQuery(null, null)));

        Assert.Equal(1, report.Tickets.Total);
        Assert.NotNull(report.Sla);
        Assert.NotNull(report.TopAgents);
        Assert.NotNull(report.Satisfaction);
    }

    [Fact]
    public void ReportsController_HasAdminOnlyAuthorizationAttribute()
    {
        // Metadata-only check: this does NOT prove a live request from an Agent-role
        // token returns 403 (this test harness calls controller actions as plain C#
        // methods, never through ASP.NET Core's authorization middleware). It confirms
        // the attribute that IS the real security boundary is present with the expected
        // values. Genuine 401/403 verification is manual (Verification Step 3).
        var attribute = typeof(ReportsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("RequireStaff", attribute!.Policy);
        Assert.Equal("Admin", attribute.Roles);
    }

    [Fact]
    public async Task GetReports_ReturnsBadRequest_WhenFromGreaterThanTo()
    {
        await using var db = NewDb();
        var now = DateTime.UtcNow;
        var range = new ReportDateRangeQuery(now, now.AddDays(-1));

        var result = await NewController().GetTicketCounts(db, range);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
