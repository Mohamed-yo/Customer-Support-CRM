using System.Net.Http.Json;
using CustomerSupportCrm.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Integrations;

public interface IOutboundWebhookDispatcher
{
    Task DispatchAsync(string eventType, object payload, CancellationToken ct = default);
}

// Story 12, Decision 2: one generic, provider-agnostic outbound webhook mechanism -
// satisfies "ERP integration" and "External system integrations" without any
// vendor-specific code. A delivery failure is logged and never propagated - the ticket
// mutation that triggered the event must never be rolled back by this.
public sealed class OutboundWebhookDispatcher : IOutboundWebhookDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private readonly ILogger<OutboundWebhookDispatcher> _logger;

    public OutboundWebhookDispatcher(HttpClient httpClient, AppDbContext db, ILogger<OutboundWebhookDispatcher> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _logger = logger;
    }

    public async Task DispatchAsync(string eventType, object payload, CancellationToken ct = default)
    {
        List<Domain.OutboundWebhookSubscription> subscriptions;
        try
        {
            // Snapshot active subscriptions before iterating so a concurrent delete mid-loop
            // is simply skipped, not a race.
            subscriptions = await _db.OutboundWebhookSubscriptions
                .Where(s => s.IsActive && s.EventType == eventType)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load outbound webhook subscriptions for event {EventType}", eventType);
            return;
        }

        if (subscriptions.Count == 0) return;

        var body = new { @event = eventType, data = payload };

        foreach (var subscription in subscriptions)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(subscription.TargetUrl, body, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Outbound webhook to {TargetUrl} for event {EventType} returned {StatusCode}",
                        subscription.TargetUrl, eventType, (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // One subscription's failure must never stop the others, and must never
                // surface to the request that triggered this dispatch.
                _logger.LogWarning(ex, "Outbound webhook to {TargetUrl} for event {EventType} failed", subscription.TargetUrl, eventType);
            }
        }
    }
}
