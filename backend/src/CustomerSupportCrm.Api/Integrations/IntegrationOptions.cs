namespace CustomerSupportCrm.Api.Integrations;

public sealed class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
}

public sealed class ChannelInboundSecrets
{
    public string? EmailSecret { get; set; }
    public string? WhatsappSecret { get; set; }
    public string? SmsSecret { get; set; }
}

public sealed class WhatsappOutboundOptions
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
}

public sealed class SmsOutboundOptions
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
}
