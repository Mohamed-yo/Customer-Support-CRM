namespace CustomerSupportCrm.Api.Domain;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    // Ticket-scoped notifications only in this story; kept nullable for future non-ticket types.
    public Guid? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    // "Assigned" | "Escalated" | "TaskReminder" | "Mention"
    public string Type { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }

    // Story 15: set only when Type == "TaskReminder". Bare Guid, no FK/navigation - mirrors
    // AuditLog.ActorUserId's "generic reference, may independently disappear" precedent,
    // because TicketsController.DeleteTask deletes a TicketTask with no cascade/cleanup here.
    // A stale reference after the source task is deleted is expected and harmless.
    public Guid? SourceTaskId { get; set; }

    // Story 15: set only when Type == "Mention". Same bare-Guid, no-FK rationale as
    // SourceTaskId - a ticket note can be deleted independently of any notification about it.
    // Invariant: exactly one of SourceTaskId/SourceTicketNoteId is non-null when Type is
    // "TaskReminder"/"Mention" respectively; both are null for every other Type.
    public Guid? SourceTicketNoteId { get; set; }
}
