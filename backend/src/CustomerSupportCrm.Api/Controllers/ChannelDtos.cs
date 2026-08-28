using System.ComponentModel.DataAnnotations;

namespace CustomerSupportCrm.Api.Controllers;

public sealed class WebFormSubmissionRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Phone { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public string? Priority { get; set; }
}

public sealed record WebFormSubmissionResponse(Guid TicketId, string ReferenceNumber);

// Shaped like a generic provider "inbound message" webhook payload - not tied to any
// specific vendor's actual schema (none has been verified).
public sealed class InboundChannelWebhookRequest
{
    public string From { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;

    // When set and it resolves to an existing ticket on this channel, this message is
    // appended to it instead of creating a new ticket.
    public Guid? TicketId { get; set; }
}

public sealed class OutboundChannelReplyRequest
{
    [Required]
    public Guid TicketId { get; set; }

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Body { get; set; } = string.Empty;

    public string? Subject { get; set; }
}

public sealed record ChannelMessageItem(
    Guid Id, string Channel, string Direction, string FromAddress, string? ToAddress,
    string? Subject, string Body, string SendResult, string? SendResultDetail, DateTime CreatedAtUtc);

public sealed record InboundChannelWebhookResponse(Guid TicketId, Guid MessageId, bool CreatedNewTicket);
