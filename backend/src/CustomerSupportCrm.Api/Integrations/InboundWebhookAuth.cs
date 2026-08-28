using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace CustomerSupportCrm.Api.Integrations;

public interface IInboundWebhookAuthenticator
{
    bool Verify(string channel, HttpRequest request);
}

// Story 12, Decision 5: a shared-secret header, kept behind this single interface so a
// later story can swap in real provider-specific signature verification (SendGrid's,
// Twilio's, Meta's own signing schemes) without touching any controller.
public sealed class SharedSecretAuthenticator : IInboundWebhookAuthenticator
{
    private readonly ChannelInboundSecrets _secrets;

    public SharedSecretAuthenticator(IOptions<ChannelInboundSecrets> secrets)
    {
        _secrets = secrets.Value;
    }

    public bool Verify(string channel, HttpRequest request)
    {
        var configured = channel switch
        {
            "Email" => _secrets.EmailSecret,
            "WhatsApp" => _secrets.WhatsappSecret,
            "SMS" => _secrets.SmsSecret,
            _ => null,
        };
        if (string.IsNullOrEmpty(configured))
        {
            // No secret configured for this channel = the channel is not enabled.
            // Reject rather than silently allow unauthenticated traffic.
            return false;
        }
        if (!request.Headers.TryGetValue("X-Webhook-Secret", out var provided))
        {
            return false;
        }

        var a = Encoding.UTF8.GetBytes(configured);
        var b = Encoding.UTF8.GetBytes(provided.ToString());
        if (a.Length != b.Length) return false;
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
