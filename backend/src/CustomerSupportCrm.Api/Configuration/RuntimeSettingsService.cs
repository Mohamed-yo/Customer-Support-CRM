using System.Text.Json;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CustomerSupportCrm.Api.Configuration;

public class RuntimeSettingsService : IRuntimeSettings
{
    // Short-lived: keeps read-heavy paths (ticket SLA computation on every list/get) off the
    // database without letting an admin's change sit stale for long after a PUT.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public RuntimeSettingsService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private static string CacheKey(string key) => $"RuntimeSetting:{key}";

    public async Task<T> GetAsync<T>(string key, T fallback, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey(key), out T? cached) && cached is not null)
        {
            return cached;
        }

        var valueJson = await _db.RuntimeSettings
            .AsNoTracking()
            .Where(r => r.Key == key)
            .Select(r => r.ValueJson)
            .SingleOrDefaultAsync(ct);

        if (valueJson is null)
        {
            return fallback;
        }

        T? value;
        try
        {
            value = JsonSerializer.Deserialize<T>(valueJson);
        }
        catch (JsonException)
        {
            return fallback;
        }

        if (value is null)
        {
            return fallback;
        }

        _cache.Set(CacheKey(key), value, CacheDuration);
        return value;
    }

    public async Task SetAsync<T>(string key, T value, Guid? updatedByUserId, CancellationToken ct = default)
    {
        var valueJson = JsonSerializer.Serialize(value);

        var row = await _db.RuntimeSettings.SingleOrDefaultAsync(r => r.Key == key, ct);
        if (row is null)
        {
            row = new RuntimeSetting { Key = key };
            _db.RuntimeSettings.Add(row);
        }

        row.ValueJson = valueJson;
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.UpdatedByUserId = updatedByUserId;
        await _db.SaveChangesAsync(ct);

        _cache.Set(CacheKey(key), value, CacheDuration);
    }
}
