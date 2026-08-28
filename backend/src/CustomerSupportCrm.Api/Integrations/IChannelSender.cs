namespace CustomerSupportCrm.Api.Integrations;

// One interface, resolved per-channel via keyed DI (registered in Program.cs as
// AddKeyedScoped<IChannelSender, XxxSender>("Email"/"WhatsApp"/"SMS")). Story 12,
// Decision 1: no specific provider is named, so WhatsApp/SMS share one generic
// HTTP-POST-to-configured-endpoint shape; only Email talks a real protocol (SMTP via
// MailKit, Decision 4).
public interface IChannelSender
{
    Task<SendResult> SendAsync(string to, string? subject, string body, CancellationToken ct = default);
}
