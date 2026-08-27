namespace CustomerSupportCrm.Api.Controllers;

public record TicketTaskItem(
    Guid Id,
    Guid TicketId,
    string Title,
    DateTime? DueAtUtc,
    bool IsDone,
    DateTime CreatedAtUtc);

public class TicketTaskUpsertRequest
{
    public string Title { get; set; } = string.Empty;
    public DateTime? DueAtUtc { get; set; }
    public bool IsDone { get; set; }
}
