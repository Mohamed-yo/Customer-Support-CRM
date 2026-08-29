using System.Net;
using System.Security.Cryptography;
using System.Text;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

public class OutboundWebhookSigningTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static string ComputeSignature(string secret, string timestamp, string body)
    {
        var bytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [Fact]
    public async Task DispatchAsync_SignsRequest_WithValidSignatureAndTimestampHeaders()
    {
        await using var db = NewDb();
        var subscription = new OutboundWebhookSubscription
        {
            TargetUrl = "https://example.com/hook",
            EventType = "ticket.created",
            IsActive = true,
            SigningSecret = "test-secret-abc123",
        };
        db.OutboundWebhookSubscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var handler = new CapturingHandler();
        var httpClient = new HttpClient(handler);
        var dispatcher = new OutboundWebhookDispatcher(httpClient, db, NullLogger<OutboundWebhookDispatcher>.Instance);

        await dispatcher.DispatchAsync("ticket.created", new { id = Guid.NewGuid() });

        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest!.Headers.TryGetValues("X-Squad-Timestamp", out var timestamps));
        Assert.True(handler.LastRequest.Headers.TryGetValues("X-Squad-Signature", out var signatures));

        var timestamp = timestamps!.Single();
        var signatureHeader = signatures!.Single();
        Assert.StartsWith("sha256=", signatureHeader);
        var actualSignature = signatureHeader["sha256=".Length..];

        var expectedSignature = ComputeSignature(subscription.SigningSecret, timestamp, handler.LastBody!);
        Assert.Equal(expectedSignature, actualSignature);
    }

    [Fact]
    public async Task DispatchAsync_TamperedBody_FailsSignatureVerification()
    {
        await using var db = NewDb();
        var subscription = new OutboundWebhookSubscription
        {
            TargetUrl = "https://example.com/hook",
            EventType = "ticket.closed",
            IsActive = true,
            SigningSecret = "another-secret",
        };
        db.OutboundWebhookSubscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var handler = new CapturingHandler();
        var httpClient = new HttpClient(handler);
        var dispatcher = new OutboundWebhookDispatcher(httpClient, db, NullLogger<OutboundWebhookDispatcher>.Instance);

        await dispatcher.DispatchAsync("ticket.closed", new { id = Guid.NewGuid() });

        var timestamp = handler.LastRequest!.Headers.GetValues("X-Squad-Timestamp").Single();
        var signatureHeader = handler.LastRequest.Headers.GetValues("X-Squad-Signature").Single();
        var actualSignature = signatureHeader["sha256=".Length..];

        var tamperedBody = handler.LastBody + "tampered";
        var signatureOverTamperedBody = ComputeSignature(subscription.SigningSecret, timestamp, tamperedBody);

        Assert.NotEqual(signatureOverTamperedBody, actualSignature);
    }
}
