namespace CustomerSupportCrm.Api.Domain;

public class ChannelMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    // "Email" | "WhatsApp" | "SMS"
    public string Channel { get; set; } = string.Empty;

    // "Inbound" | "Outbound"
    public string Direction { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;
    public string? ToAddress { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;

    // Reserved for future provider-side threading correlation - not populated this story.
    public string? ExternalMessageId { get; set; }

    // "" for inbound; "Success" | "Failure" | "NotConfigured" for outbound.
    public string SendResult { get; set; } = string.Empty;
    public string? SendResultDetail { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
