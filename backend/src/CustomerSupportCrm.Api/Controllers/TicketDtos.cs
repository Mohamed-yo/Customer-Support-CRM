namespace CustomerSupportCrm.Api.Controllers;

public record TicketListItem(
    Guid Id,
    Guid CustomerId,
    string CustomerFullName,
    string Subject,
    string? Description,
    string Status,
    DateTime CreatedAtUtc);

public class TicketUpsertRequest
{
    public Guid CustomerId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Open";
}
