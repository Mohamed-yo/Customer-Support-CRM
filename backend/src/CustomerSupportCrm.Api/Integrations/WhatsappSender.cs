using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace CustomerSupportCrm.Api.Integrations;

// Story 12, Decision 1: no specific WhatsApp provider is named, so this is a generic
// "POST { to, subject, body } to a configured endpoint" shape, not a vendor SDK.
public sealed class WhatsappSender : IChannelSender
{
    private readonly HttpClient _httpClient;
    private readonly WhatsappOutboundOptions _options;

    public WhatsappSender(HttpClient httpClient, IOptions<WhatsappOutboundOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<SendResult> SendAsync(string to, string? subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            return new SendResult(SendStatus.NotConfigured, "WhatsApp outbound endpoint not configured");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
            {
                Content = JsonContent.Create(new { to, subject, body }),
            };
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }

            using var response = await _httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                return new SendResult(SendStatus.Success);
            }
            return new SendResult(SendStatus.Failure, $"Provider returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return new SendResult(SendStatus.Failure, ex.Message);
        }
    }
}
