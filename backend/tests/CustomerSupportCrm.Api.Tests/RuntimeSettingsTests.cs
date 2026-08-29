using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

public class RuntimeSettingsTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class Sample
    {
        public string Text { get; set; } = string.Empty;
    }

    [Fact]
    public async Task GetAsync_ReturnsFallback_WhenKeyAbsent()
    {
        await using var db = NewDb();
        var settings = new RuntimeSettingsService(db, new MemoryCache(new MemoryCacheOptions()));

        var fallback = new Sample { Text = "default" };
        var result = await settings.GetAsync("missing_key", fallback);

        Assert.Same(fallback, result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_RoundTripsTheStoredValue()
    {
        await using var db = NewDb();
        var settings = new RuntimeSettingsService(db, new MemoryCache(new MemoryCacheOptions()));

        await settings.SetAsync("sample_key", new Sample { Text = "stored" }, updatedByUserId: null);
        var result = await settings.GetAsync("sample_key", new Sample { Text = "fallback" });

        Assert.Equal("stored", result.Text);
    }

    [Fact]
    public async Task SetAsync_InvalidatesCache_SubsequentGetSeesNewValue()
    {
        await using var db = NewDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var settings = new RuntimeSettingsService(db, cache);

        await settings.SetAsync("sample_key", new Sample { Text = "first" }, updatedByUserId: null);
        var first = await settings.GetAsync("sample_key", new Sample { Text = "fallback" });
        Assert.Equal("first", first.Text);

        await settings.SetAsync("sample_key", new Sample { Text = "second" }, updatedByUserId: null);
        var second = await settings.GetAsync("sample_key", new Sample { Text = "fallback" });

        Assert.Equal("second", second.Text);
    }

    [Fact]
    public async Task SetAsync_PersistsUpdatedByUserId_AndUpdatesTimestamp()
    {
        await using var db = NewDb();
        var settings = new RuntimeSettingsService(db, new MemoryCache(new MemoryCacheOptions()));
        var actorId = Guid.NewGuid();

        await settings.SetAsync("sample_key", new Sample { Text = "x" }, actorId);

        var row = await db.RuntimeSettings.SingleAsync(r => r.Key == "sample_key");
        Assert.Equal(actorId, row.UpdatedByUserId);
        Assert.True((DateTime.UtcNow - row.UpdatedAtUtc).TotalMinutes < 1);
    }
}
