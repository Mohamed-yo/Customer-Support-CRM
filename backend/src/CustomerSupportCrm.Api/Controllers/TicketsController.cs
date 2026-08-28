using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize(Policy = "RequireStaff")]
public class TicketsController : ControllerBase
{
    private const long MaxAttachmentSizeBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedStatuses = { "Open", "InProgress", "Closed" };
    private static readonly string[] AllowedCategories = { "General", "Billing", "Technical", "Account" };
    // Internal: also validated against by PortalController for portal-submitted tickets.
    internal static readonly string[] AllowedPriorities = { "Low", "Normal", "High", "Urgent" };

    // Fixed this story — an admin-editable configuration UI is Story 14 scope, not this one.
    private static readonly IReadOnlyDictionary<string, (TimeSpan Response, TimeSpan Resolution)> SlaTargets =
        new Dictionary<string, (TimeSpan, TimeSpan)>(StringComparer.Ordinal)
        {
            ["Urgent"] = (TimeSpan.FromHours(1), TimeSpan.FromHours(4)),
            ["High"] = (TimeSpan.FromHours(2), TimeSpan.FromHours(8)),
            ["Normal"] = (TimeSpan.FromHours(4), TimeSpan.FromHours(24)),
            ["Low"] = (TimeSpan.FromHours(8), TimeSpan.FromHours(48)),
        };

    // Internal: also called by PortalController so a portal-submitted ticket gets identical
    // SLA due dates to a staff-created one.
    internal static (DateTime response, DateTime resolution) ComputeDueDates(DateTime createdUtc, string priority)
    {
        var target = SlaTargets.TryGetValue(priority, out var t) ? t : SlaTargets["Normal"];
        return (createdUtc + target.Response, createdUtc + target.Resolution);
    }

    private static bool ComputeIsEscalated(
        string status, DateTime responseDueUtc, DateTime resolutionDueUtc,
        DateTime? firstRespondedAtUtc, DateTime? resolvedAtUtc, DateTime nowUtc)
    {
        if (status == "Closed") return false;
        if (firstRespondedAtUtc is null && nowUtc > responseDueUtc) return true;
        if (resolvedAtUtc is null && nowUtc > resolutionDueUtc) return true;
        return false;
    }

    // Batched: checks all given (ticketId, assigneeId) pairs in one query and inserts
    // at most one unread "Escalated" notification per ticket, avoiding N+1 round trips
    // when called from List.
    private static async Task EnsureEscalationNotificationsAsync(
        IReadOnlyCollection<(Guid TicketId, Guid AssigneeId)> escalated, AppDbContext db)
    {
        if (escalated.Count == 0) return;

        var ticketIds = escalated.Select(x => x.TicketId).ToList();
        var alreadyNotified = await db.Notifications
            .Where(n => n.Type == "Escalated" && !n.IsRead && n.TicketId != null && ticketIds.Contains(n.TicketId.Value))
            .Select(n => n.TicketId!.Value)
            .ToListAsync();
        var alreadyNotifiedSet = alreadyNotified.ToHashSet();

        var toAdd = escalated
            .Where(x => !alreadyNotifiedSet.Contains(x.TicketId))
            .Select(x => new Notification
            {
                UserId = x.AssigneeId,
                TicketId = x.TicketId,
                Type = "Escalated",
                Message = "This ticket has breached its SLA.",
                IsRead = false,
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            db.Notifications.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }

    // Internal: also called by PortalController when a portal-submitted ticket is auto-assigned.
    internal static async Task CreateAssignedNotificationAsync(Guid ticketId, Guid assigneeId, AppDbContext db)
    {
        db.Notifications.Add(new Notification
        {
            UserId = assigneeId,
            TicketId = ticketId,
            Type = "Assigned",
            Message = "You have been assigned a ticket.",
            IsRead = false,
        });
        await db.SaveChangesAsync();
    }

    // Least-loaded, not strict round-robin: no persisted rotation-pointer state needed.
    // Internal: also called by PortalController for portal-submitted tickets (Decision 5 -
    // every portal ticket is unassigned at submission time, same as a staff-created ticket
    // with no assignee picked).
    internal static async Task<User?> PickLeastLoadedAssigneeAsync(AppDbContext db)
    {
        var candidates = await db.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role!.Name == "Agent" || ur.Role!.Name == "Admin"))
            .Select(u => u.Id)
            .ToListAsync();
        if (candidates.Count == 0) return null;

        var loads = await db.Tickets
            .Where(t => t.AssignedToUserId != null && t.Status != "Closed")
            .GroupBy(t => t.AssignedToUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var winnerId = candidates
            .OrderBy(id => loads.TryGetValue(id, out var n) ? n : 0)
            .ThenBy(id => id) // deterministic tiebreak
            .First();

        return await db.Users.FindAsync(winnerId);
    }

    private Guid? GetActorUserId()
    {
        var sub = User.FindFirst("sub");
        if (sub is not null && Guid.TryParse(sub.Value, out var parsed))
        {
            return parsed;
        }
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db, [FromQuery] Guid? customerId = null)
    {
        var query = db.Tickets.AsQueryable();
        if (customerId.HasValue)
        {
            query = query.Where(t => t.CustomerId == customerId.Value);
        }

        var raw = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new
            {
                t.Id, t.CustomerId, CustomerFullName = t.Customer!.FullName,
                t.Subject, t.Description, t.Status, t.CreatedAtUtc,
                t.AssignedToUserId,
                AssignedToDisplayName = t.AssignedToUser != null ? t.AssignedToUser.DisplayName : null,
                t.Category, t.Priority, t.FirstRespondedAtUtc, t.ResolvedAtUtc,
            })
            .ToListAsync();

        var items = BuildListItems(raw.Select(t => (
            t.Id, t.CustomerId, t.CustomerFullName, t.Subject, t.Description, t.Status, t.CreatedAtUtc,
            t.AssignedToUserId, t.AssignedToDisplayName, t.Category, t.Priority, t.FirstRespondedAtUtc, t.ResolvedAtUtc)));

        await EnsureEscalationNotificationsAsync(
            items.Where(i => i.IsEscalated && i.AssignedToUserId.HasValue)
                .Select(i => (i.Id, i.AssignedToUserId!.Value))
                .ToList(),
            db);

        return Ok(items);
    }

    private static List<TicketListItem> BuildListItems(
        IEnumerable<(Guid Id, Guid CustomerId, string CustomerFullName, string Subject, string? Description,
            string Status, DateTime CreatedAtUtc, Guid? AssignedToUserId, string? AssignedToDisplayName,
            string Category, string Priority, DateTime? FirstRespondedAtUtc, DateTime? ResolvedAtUtc)> raw)
    {
        var now = DateTime.UtcNow;
        return raw.Select(t =>
        {
            var (responseDue, resolutionDue) = ComputeDueDates(t.CreatedAtUtc, t.Priority);
            var isEscalated = ComputeIsEscalated(t.Status, responseDue, resolutionDue, t.FirstRespondedAtUtc, t.ResolvedAtUtc, now);
            return new TicketListItem(
                t.Id, t.CustomerId, t.CustomerFullName, t.Subject, t.Description, t.Status, t.CreatedAtUtc,
                t.AssignedToUserId, t.AssignedToDisplayName, t.Category, t.Priority,
                responseDue, resolutionDue, t.FirstRespondedAtUtc, t.ResolvedAtUtc, isEscalated);
        }).ToList();
    }

    [HttpGet("assignable-users")]
    public async Task<IActionResult> AssignableUsers([FromServices] AppDbContext db)
    {
        var items = await db.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role!.Name == "Agent" || ur.Role!.Name == "Admin"))
            .OrderBy(u => u.DisplayName)
            .Select(u => new AssignableUserItem(u.Id, u.DisplayName))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromServices] AppDbContext db)
    {
        var raw = await db.Tickets
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Id, t.CustomerId, CustomerFullName = t.Customer!.FullName,
                t.Subject, t.Description, t.Status, t.CreatedAtUtc,
                t.AssignedToUserId,
                AssignedToDisplayName = t.AssignedToUser != null ? t.AssignedToUser.DisplayName : null,
                t.Category, t.Priority, t.FirstRespondedAtUtc, t.ResolvedAtUtc,
            })
            .SingleOrDefaultAsync();

        if (raw is null) return NotFound(new { error = "ticket_not_found" });

        var item = BuildListItems(new[]
        {
            (raw.Id, raw.CustomerId, raw.CustomerFullName, raw.Subject, raw.Description, raw.Status, raw.CreatedAtUtc,
                raw.AssignedToUserId, raw.AssignedToDisplayName, raw.Category, raw.Priority, raw.FirstRespondedAtUtc, raw.ResolvedAtUtc),
        }).Single();

        if (item.IsEscalated && item.AssignedToUserId.HasValue)
        {
            await EnsureEscalationNotificationsAsync(new[] { (item.Id, item.AssignedToUserId.Value) }, db);
        }

        return Ok(item);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, [FromServices] AppDbContext db)
    {
        var exists = await db.Tickets.AnyAsync(t => t.Id == id);
        if (!exists) return NotFound(new { error = "ticket_not_found" });

        var idStr = id.ToString();
        var items = await db.AuditLogs
            .Where(a => a.Action.StartsWith("ticket.") && a.Details == idStr)
            .OrderBy(a => a.TimestampUtc)
            .Select(a => new HistoryEntry(
                a.Id, a.Action, a.Outcome, a.ActorUserId,
                a.ActorUserId == null ? null : db.Users.Where(u => u.Id == a.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                a.TimestampUtc))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}/notes")]
    public async Task<IActionResult> ListNotes(Guid id, [FromServices] AppDbContext db)
    {
        var exists = await db.Tickets.AnyAsync(t => t.Id == id);
        if (!exists) return NotFound(new { error = "ticket_not_found" });

        var items = await db.TicketNotes
            .Where(n => n.TicketId == id)
            .OrderBy(n => n.CreatedAtUtc)
            .Select(n => new TicketNoteItem(n.Id, n.TicketId, n.AuthorUserId, n.AuthorUser!.DisplayName, n.Body, n.CreatedAtUtc))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> CreateNote(Guid id, [FromBody] TicketNoteCreateRequest request, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(new { error = "body_required" });
        }
        var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound(new { error = "ticket_not_found" });

        var actorId = GetActorUserId();
        if (actorId is null) return Unauthorized();

        var note = new TicketNote
        {
            TicketId = id,
            AuthorUserId = actorId.Value,
            Body = request.Body,
        };
        db.TicketNotes.Add(note);

        if (ticket.FirstRespondedAtUtc is null)
        {
            ticket.FirstRespondedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        var author = await db.Users.Where(u => u.Id == actorId.Value).Select(u => u.DisplayName).SingleAsync();
        var item = new TicketNoteItem(note.Id, note.TicketId, note.AuthorUserId, author, note.Body, note.CreatedAtUtc);
        return CreatedAtAction(nameof(ListNotes), new { id }, item);
    }

    [HttpGet("{id:guid}/attachments")]
    public async Task<IActionResult> ListAttachments(Guid id, [FromServices] AppDbContext db)
    {
        var exists = await db.Tickets.AnyAsync(t => t.Id == id);
        if (!exists) return NotFound(new { error = "ticket_not_found" });

        var items = await db.TicketAttachments
            .Where(a => a.TicketId == id)
            .OrderBy(a => a.CreatedAtUtc)
            .Select(a => new TicketAttachmentItem(
                a.Id, a.TicketId, a.FileName, a.ContentType, a.SizeBytes,
                a.UploadedByUserId, a.UploadedByUser!.DisplayName, a.CreatedAtUtc))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile? file, [FromServices] AppDbContext db)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "file_required" });
        }
        if (file.Length > MaxAttachmentSizeBytes)
        {
            return BadRequest(new { error = "file_too_large" });
        }
        var ticket = await db.Tickets.AnyAsync(t => t.Id == id);
        if (!ticket) return NotFound(new { error = "ticket_not_found" });

        var actorId = GetActorUserId();
        if (actorId is null) return Unauthorized();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        var attachment = new TicketAttachment
        {
            TicketId = id,
            UploadedByUserId = actorId.Value,
            FileName = Path.GetFileName(file.FileName),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            Content = stream.ToArray(),
        };
        db.TicketAttachments.Add(attachment);
        await db.SaveChangesAsync();

        var uploader = await db.Users.Where(u => u.Id == actorId.Value).Select(u => u.DisplayName).SingleAsync();
        var item = new TicketAttachmentItem(
            attachment.Id, attachment.TicketId, attachment.FileName, attachment.ContentType, attachment.SizeBytes,
            attachment.UploadedByUserId, uploader, attachment.CreatedAtUtc);
        return CreatedAtAction(nameof(ListAttachments), new { id }, item);
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, [FromServices] AppDbContext db)
    {
        var attachment = await db.TicketAttachments.SingleOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == id);
        if (attachment is null) return NotFound(new { error = "attachment_not_found" });

        return File(attachment.Content, attachment.ContentType, attachment.FileName);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, [FromServices] AppDbContext db)
    {
        var attachment = await db.TicketAttachments.SingleOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == id);
        if (attachment is null) return NotFound(new { error = "attachment_not_found" });

        db.TicketAttachments.Remove(attachment);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/tasks")]
    public async Task<IActionResult> ListTasks(Guid id, [FromServices] AppDbContext db)
    {
        var exists = await db.Tickets.AnyAsync(t => t.Id == id);
        if (!exists) return NotFound(new { error = "ticket_not_found" });

        var items = await db.TicketTasks
            .Where(t => t.TicketId == id)
            .OrderBy(t => t.IsDone)
            .ThenBy(t => t.DueAtUtc == null)
            .ThenBy(t => t.DueAtUtc)
            .ThenBy(t => t.CreatedAtUtc)
            .Select(t => new TicketTaskItem(t.Id, t.TicketId, t.Title, t.DueAtUtc, t.IsDone, t.CreatedAtUtc))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{id:guid}/tasks")]
    public async Task<IActionResult> CreateTask(Guid id, [FromBody] TicketTaskUpsertRequest request, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "title_required" });
        }
        var exists = await db.Tickets.AnyAsync(t => t.Id == id);
        if (!exists) return NotFound(new { error = "ticket_not_found" });

        var task = new TicketTask
        {
            TicketId = id,
            Title = request.Title,
            DueAtUtc = request.DueAtUtc,
            IsDone = request.IsDone,
        };
        db.TicketTasks.Add(task);
        await db.SaveChangesAsync();

        var item = new TicketTaskItem(task.Id, task.TicketId, task.Title, task.DueAtUtc, task.IsDone, task.CreatedAtUtc);
        return CreatedAtAction(nameof(ListTasks), new { id }, item);
    }

    [HttpPut("{id:guid}/tasks/{taskId:guid}")]
    public async Task<IActionResult> UpdateTask(Guid id, Guid taskId, [FromBody] TicketTaskUpsertRequest request, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "title_required" });
        }
        var task = await db.TicketTasks.SingleOrDefaultAsync(t => t.Id == taskId && t.TicketId == id);
        if (task is null) return NotFound(new { error = "task_not_found" });

        task.Title = request.Title;
        task.DueAtUtc = request.DueAtUtc;
        task.IsDone = request.IsDone;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}/tasks/{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id, Guid taskId, [FromServices] AppDbContext db)
    {
        var task = await db.TicketTasks.SingleOrDefaultAsync(t => t.Id == taskId && t.TicketId == id);
        if (task is null) return NotFound(new { error = "task_not_found" });

        db.TicketTasks.Remove(task);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static async Task<(bool ok, User? assignee)> TryResolveAssignee(
        Guid? assignedToUserId, AppDbContext db)
    {
        if (!assignedToUserId.HasValue) return (true, null);

        var assignee = await db.Users
            .Where(u => u.Id == assignedToUserId.Value)
            .Where(u => u.UserRoles.Any(ur => ur.Role!.Name == "Agent" || ur.Role!.Name == "Admin"))
            .SingleOrDefaultAsync();

        return (assignee is not null, assignee);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] TicketUpsertRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return BadRequest(new { error = "subject_required" });
        }
        if (!AllowedStatuses.Contains(request.Status))
        {
            return BadRequest(new { error = "status_invalid" });
        }
        if (!AllowedCategories.Contains(request.Category))
        {
            return BadRequest(new { error = "category_invalid" });
        }
        if (!AllowedPriorities.Contains(request.Priority))
        {
            return BadRequest(new { error = "priority_invalid" });
        }
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == request.CustomerId);
        if (customer is null)
        {
            return BadRequest(new { error = "customer_not_found" });
        }
        var (assigneeOk, assignee) = await TryResolveAssignee(request.AssignedToUserId, db);
        if (!assigneeOk)
        {
            return BadRequest(new { error = "assignee_not_found" });
        }

        // Decision 4: any null AssignedToUserId on Create triggers auto-assignment
        // (the plain Guid? shape can't distinguish "omitted" from "explicit null").
        if (assignee is null)
        {
            assignee = await PickLeastLoadedAssigneeAsync(db);
        }

        var ticket = new Ticket
        {
            CustomerId = request.CustomerId,
            Subject = request.Subject,
            Description = request.Description,
            Status = request.Status,
            Category = request.Category,
            Priority = request.Priority,
            AssignedToUserId = assignee?.Id,
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "ticket.create",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = null,
            Details = ticket.Id.ToString(),
        });

        if (assignee is not null)
        {
            await CreateAssignedNotificationAsync(ticket.Id, assignee.Id, db);
        }

        var (responseDue, resolutionDue) = ComputeDueDates(ticket.CreatedAtUtc, ticket.Priority);
        var item = new TicketListItem(
            ticket.Id, ticket.CustomerId, customer.FullName,
            ticket.Subject, ticket.Description, ticket.Status, ticket.CreatedAtUtc,
            ticket.AssignedToUserId, assignee?.DisplayName,
            ticket.Category, ticket.Priority,
            responseDue, resolutionDue, ticket.FirstRespondedAtUtc, ticket.ResolvedAtUtc, IsEscalated: false);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TicketUpsertRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return BadRequest(new { error = "subject_required" });
        }
        if (!AllowedStatuses.Contains(request.Status))
        {
            return BadRequest(new { error = "status_invalid" });
        }
        if (!AllowedCategories.Contains(request.Category))
        {
            return BadRequest(new { error = "category_invalid" });
        }
        if (!AllowedPriorities.Contains(request.Priority))
        {
            return BadRequest(new { error = "priority_invalid" });
        }
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == request.CustomerId);
        if (customer is null)
        {
            return BadRequest(new { error = "customer_not_found" });
        }
        var (assigneeOk, assignee) = await TryResolveAssignee(request.AssignedToUserId, db);
        if (!assigneeOk)
        {
            return BadRequest(new { error = "assignee_not_found" });
        }

        var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound(new { error = "ticket_not_found" });

        var previousStatus = ticket.Status;
        var previousAssigneeId = ticket.AssignedToUserId;

        ticket.CustomerId = request.CustomerId;
        ticket.Subject = request.Subject;
        ticket.Description = request.Description;
        ticket.Status = request.Status;
        ticket.Category = request.Category;
        ticket.Priority = request.Priority;
        ticket.AssignedToUserId = assignee?.Id;

        if (ticket.Status == "Closed" && previousStatus != "Closed")
        {
            ticket.ResolvedAtUtc = DateTime.UtcNow;
        }
        else if (ticket.Status != "Closed" && previousStatus == "Closed")
        {
            ticket.ResolvedAtUtc = null;
        }

        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "ticket.update",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = null,
            Details = ticket.Id.ToString(),
        });

        if (assignee is not null && assignee.Id != previousAssigneeId)
        {
            await CreateAssignedNotificationAsync(ticket.Id, assignee.Id, db);
        }

        if (ticket.AssignedToUserId.HasValue)
        {
            var (responseDue, resolutionDue) = ComputeDueDates(ticket.CreatedAtUtc, ticket.Priority);
            var isEscalated = ComputeIsEscalated(
                ticket.Status, responseDue, resolutionDue, ticket.FirstRespondedAtUtc, ticket.ResolvedAtUtc, DateTime.UtcNow);
            if (isEscalated)
            {
                await EnsureEscalationNotificationsAsync(new[] { (ticket.Id, ticket.AssignedToUserId.Value) }, db);
            }
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireStaff", Roles = "Admin")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound(new { error = "ticket_not_found" });

        db.Tickets.Remove(ticket);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "ticket.delete",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = null,
            Details = id.ToString(),
        });

        return NoContent();
    }
}
