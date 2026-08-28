using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CustomerSupportCrm.Api.Integrations;

// Story 12, Decision 4/1: MailKit-based SMTP sender. Blank configuration returns
// NotConfigured rather than attempting a connection - safe to ship with no real
// provider credentials, and configurable later without any redesign.
public sealed class EmailSender : IChannelSender
{
    private readonly SmtpOptions _options;

    public EmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task<SendResult> SendAsync(string to, string? subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            return new SendResult(SendStatus.NotConfigured, "SMTP host not configured");
        }
        if (string.IsNullOrWhiteSpace(_options.From))
        {
            return new SendResult(SendStatus.NotConfigured, "SMTP from-address not configured");
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject ?? string.Empty;
        message.Body = new TextPart("plain") { Text = body };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.Auto, ct);
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, ct);
            }
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            return new SendResult(SendStatus.Success);
        }
        catch (Exception ex)
        {
            // Never let a transport failure (unreachable host, auth failure, timeout)
            // become an unhandled exception - the caller always gets a clean result.
            return new SendResult(SendStatus.Failure, ex.Message);
        }
    }
}
