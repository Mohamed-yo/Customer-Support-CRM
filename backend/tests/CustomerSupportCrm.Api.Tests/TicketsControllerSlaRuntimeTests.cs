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

// Direct calls, not an HTTP round-trip - exercises TicketsController.Create's SLA due-date
// computation against a real IRuntimeSettings-backed store.
public class TicketsControllerSlaRuntimeTests
{
    private sealed class NoOpDispatcher : IOutboundWebhookDispatcher
    {
        public Task DispatchAsync(string eventType, object payload, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static TicketsController NewController() =>
        new() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    private static IRuntimeSettings NewRuntimeSettings(AppDbContext db) =>
        new RuntimeSettingsService(db, new MemoryCache(new MemoryCacheOptions()));

    private static async Task<TicketListItem> CreateTicketAsync(
        AppDbContext db, IRuntimeSettings runtimeSettings, Customer customer, string priority)
    {
        var request = new TicketUpsertRequest
        {
            CustomerId = customer.Id,
            Subject = "Test ticket",
            Status = "Open",
            Category = "General",
            Priority = priority,
        };
        var result = await NewController().Create(request, db, new AuditLogger(db, NullLogger<AuditLogger>.Instance), new NoOpDispatcher(), runtimeSettings);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        return Assert.IsType<TicketListItem>(created.Value);
    }

    [Fact]
    public async Task Create_NoAdminOverride_UsesHardcodedDefaults()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Customer" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var runtimeSettings = NewRuntimeSettings(db);

        var item = await CreateTicketAsync(db, runtimeSettings, customer, "Urgent");

        // Default Urgent: response +1h, resolution +4h.
        Assert.Equal(item.CreatedAtUtc.AddHours(1), item.ResponseDueAtUtc);
        Assert.Equal(item.CreatedAtUtc.AddHours(4), item.ResolutionDueAtUtc);
    }

    [Fact]
    public async Task Create_WithAdminOverride_UsesOverriddenValues()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Customer" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var runtimeSettings = NewRuntimeSettings(db);

        await runtimeSettings.SetAsync(
            RuntimeSettingKeys.SlaTargets,
            new Dictionary<string, SlaTargetSetting>
            {
                ["Urgent"] = new SlaTargetSetting { ResponseHours = 2, ResolutionHours = 10 },
                ["High"] = new SlaTargetSetting { ResponseHours = 2, ResolutionHours = 8 },
                ["Normal"] = new SlaTargetSetting { ResponseHours = 4, ResolutionHours = 24 },
                ["Low"] = new SlaTargetSetting { ResponseHours = 8, ResolutionHours = 48 },
            },
            updatedByUserId: null);

        var item = await CreateTicketAsync(db, runtimeSettings, customer, "Urgent");

        Assert.Equal(item.CreatedAtUtc.AddHours(2), item.ResponseDueAtUtc);
        Assert.Equal(item.CreatedAtUtc.AddHours(10), item.ResolutionDueAtUtc);
    }

    [Fact]
    public async Task Create_PartialAdminOverride_MissingPriorityFallsBackToDefault()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Customer" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var runtimeSettings = NewRuntimeSettings(db);

        // Only "Urgent" saved - "Low" must still fall back to its hardcoded default.
        await runtimeSettings.SetAsync(
            RuntimeSettingKeys.SlaTargets,
            new Dictionary<string, SlaTargetSetting> { ["Urgent"] = new SlaTargetSetting { ResponseHours = 2, ResolutionHours = 10 } },
            updatedByUserId: null);

        var item = await CreateTicketAsync(db, runtimeSettings, customer, "Low");

        Assert.Equal(item.CreatedAtUtc.AddHours(8), item.ResponseDueAtUtc);
        Assert.Equal(item.CreatedAtUtc.AddHours(48), item.ResolutionDueAtUtc);
    }
}
