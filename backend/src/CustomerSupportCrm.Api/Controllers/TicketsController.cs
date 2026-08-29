using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
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

    // Internal: also read by ReportsController (Story 13) to label ticket-count breakdowns.
    internal static readonly string[] AllowedStatuses = { "Open", "InProgress", "Closed" };
    internal static readonly string[] AllowedCategories = { "General", "Billing", "Technical", "Account" };
    // Internal: also validated against by PortalController for portal-submitted tickets.
    internal static readonly string[] AllowedPriorities = { "Low", "Normal", "High", "Urgent" };

    // Story 15: admin-editable via RuntimeSettingsController (key RuntimeSettingKeys.SlaTargets).
    // These are the historical hardcoded values, now used only as the fallback when no admin
    // override has been saved yet - existing behavior is unchanged until an admin edits them.
    internal static readonly IReadOnlyDictionary<string, SlaTargetSetting> DefaultSlaTargets =
        new Dictionary<string, SlaTargetSetting>(StringComparer.Ordinal)
        {
            ["Urgent"] = new SlaTargetSetting { ResponseHours = 1, ResolutionHours = 4 },
            ["High"] = new SlaTargetSetting { ResponseHours = 2, ResolutionHours = 8 },
            ["Normal"] = new SlaTargetSetting { ResponseHours = 4, ResolutionHours = 24 },
            ["Low"] = new SlaTargetSetting { ResponseHours = 8, ResolutionHours = 48 },
        };

    // Internal: also called by ReportsController for SLA/escalation reporting, so every
    // caller resolves the exact same effective targets a ticket action would use.
    internal static async Task<Dictionary<string, SlaTargetSetting>> ResolveSlaTargetsAsync(IRuntimeSettings runtimeSettings)
    {
        var stored = await runtimeSettings.GetAsync(
            RuntimeSettingKeys.SlaTargets,
            new Dictionary<string, SlaTargetSetting>(DefaultSlaTargets, StringComparer.Ordinal));

        // Defensive merge: an admin save that omits a priority (e.g. only "Urgent" edited)
        // must not leave ComputeDueDates without a fallback for the priorities it wasn't given.
        if (DefaultSlaTargets.Keys.All(stored.ContainsKey))
        {
            return stored;
        }

        var merged = new Dictionary<string, SlaTargetSetting>(DefaultSlaTargets, StringComparer.Ordinal);
        foreach (var kvp in stored)
        {
            merged[kvp.Key] = kvp.Value;
        }
        return merged;
    }

    // Internal: also called by PortalController so a portal-submitted ticket gets identical
    // SLA due dates to a staff-created one.
    internal static (DateTime response, DateTime resolution) ComputeDueDates(
        DateTime createdUtc, string priority, IReadOnlyDictionary<string, SlaTargetSetting> slaTargets)
    {
        var target = slaTargets.TryGetValue(priority, out var t) ? t : slaTargets["Normal"];
        return (createdUtc + TimeSpan.FromHours(target.ResponseHours), createdUtc + TimeSpan.FromHours(target.ResolutionHours));
    }

    // Internal: also called by ReportsController (Story 13) for SLA/escalation reporting -
    // the escalation condition must never be duplicated outside this one place.
    internal static bool ComputeIsEscalated(
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
    public async Task<IActionResult> List(
        [FromServices] AppDbContext db,
        [FromServices] IRuntimeSettings runtimeSettings,
        [FromQuery] Guid? customerId = null)
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
                t.DepartmentId, DepartmentName = t.Department != null ? t.Department.Name : null,
                t.BranchId, BranchName = t.Branch != null ? t.Branch.Name : null,
            })
            .ToListAsync();

        var slaTargets = await ResolveSlaTargetsAsync(runtimeSettings);
        var items = BuildListItems(raw.Select(t => (
            t.Id, t.CustomerId, t.CustomerFullName, t.Subject, t.Description, t.Status, t.CreatedAtUtc,
            t.AssignedToUserId, t.AssignedToDisplayName, t.Category, t.Priority, t.FirstRespondedAtUtc, t.ResolvedAtUtc,
            t.DepartmentId, t.DepartmentName, t.BranchId, t.BranchName)),
            slaTargets);

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
            string Category, string Priority, DateTime? FirstRespondedAtUtc, DateTime? ResolvedAtUtc,
            Guid? DepartmentId, string? DepartmentName, Guid? BranchId, string? BranchName)> raw,
        IReadOnlyDictionary<string, SlaTargetSetting> slaTargets)
    {
        var now = DateTime.UtcNow;
        return raw.Select(t =>
        {
            var (responseDue, resolutionDue) = ComputeDueDates(t.CreatedAtUtc, t.Priority, slaTargets);
            var isEscalated = ComputeIsEscalated(t.Status, responseDue, resolutionDue, t.FirstRespondedAtUtc, t.ResolvedAtUtc, now);
            return new TicketListItem(
                t.Id, t.CustomerId, t.CustomerFullName, t.Subject, t.Description, t.Status, t.CreatedAtUtc,
                t.AssignedToUserId, t.AssignedToDisplayName, t.Category, t.Priority,
                responseDue, resolutionDue, t.FirstRespondedAtUtc, t.ResolvedAtUtc, isEscalated,
                t.DepartmentId, t.DepartmentName, t.BranchId, t.BranchName);
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

    [HttpGet("department-options")]
    public async Task<IActionResult> DepartmentOptions([FromServices] AppDbContext db)
    {
        var items = await db.Departments
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentOptionItem(d.Id, d.Name))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("branch-options")]
    public async Task<IActionResult> BranchOptions([FromServices] AppDbContext db)
    {
        var items = await db.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new BranchOptionItem(b.Id, b.Name))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("mentionable-users")]
    public async Task<IActionResult> MentionableUsers([FromServices] AppDbContext db, [FromQuery] string? search = null)
    {
        var query = db.Users.Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u => u.DisplayName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        var items = await query
            .OrderBy(u => u.DisplayName)
            .Take(20)
            .Select(u => new MentionableUserItem(u.Id, u.DisplayName, u.Email))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromServices] AppDbContext db, [FromServices] IRuntimeSettings runtimeSettings)
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
                t.DepartmentId, DepartmentName = t.Department != null ? t.Department.Name : null,
                t.BranchId, BranchName = t.Branch != null ? t.Branch.Name : null,
            })
            .SingleOrDefaultAsync();

        if (raw is null) return NotFound(new { error = "ticket_not_found" });

        var slaTargets = await ResolveSlaTargetsAsync(runtimeSettings);
        var item = BuildListItems(new[]
        {
            (raw.Id, raw.CustomerId, raw.CustomerFullName, raw.Subject, raw.Description, raw.Status, raw.CreatedAtUtc,
                raw.AssignedToUserId, raw.AssignedToDisplayName, raw.Category, raw.Priority, raw.FirstRespondedAtUtc, raw.ResolvedAtUtc,
                raw.DepartmentId, raw.DepartmentName, raw.BranchId, raw.BranchName),
        }, slaTargets).Single();

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

        var mentionedIds = request.MentionedUserIds?.Distinct().Where(mid => mid != actorId.Value).ToList()
            ?? new List<Guid>();
        if (mentionedIds.Count > 0)
        {
            var validMentionCount = await db.Users.CountAsync(u => mentionedIds.Contains(u.Id) && u.IsActive);
            if (validMentionCount != mentionedIds.Count)
            {
                return BadRequest(new { error = "mentioned_user_invalid" });
            }
        }

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

        if (mentionedIds.Count > 0)
        {
            foreach (var mentionedUserId in mentionedIds)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = mentionedUserId,
                    TicketId = id,
                    Type = "Mention",
                    Message = "You were mentioned in a ticket note.",
                    IsRead = false,
                    SourceTicketNoteId = note.Id,
                });
            }
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
        [FromServices] AuditLogger audit,
        [FromServices] IOutboundWebhookDispatcher webhooks,
        [FromServices] IRuntimeSettings runtimeSettings)
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
        if (request.DepartmentId.HasValue && !await db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value))
        {
            return BadRequest(new { error = "department_not_found" });
        }
        if (request.BranchId.HasValue && !await db.Branches.AnyAsync(b => b.Id == request.BranchId.Value))
        {
            return BadRequest(new { error = "branch_not_found" });
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
            Source = "Manual",
            DepartmentId = request.DepartmentId,
            BranchId = request.BranchId,
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

        await webhooks.DispatchAsync("ticket.created", new
        {
            id = ticket.Id, subject = ticket.Subject, status = ticket.Status,
            priority = ticket.Priority, source = ticket.Source, customerId = ticket.CustomerId,
            createdAtUtc = ticket.CreatedAtUtc,
        });

        var slaTargets = await ResolveSlaTargetsAsync(runtimeSettings);
        var (responseDue, resolutionDue) = ComputeDueDates(ticket.CreatedAtUtc, ticket.Priority, slaTargets);
        var departmentName = ticket.DepartmentId.HasValue
            ? await db.Departments.Where(d => d.Id == ticket.DepartmentId.Value).Select(d => d.Name).SingleOrDefaultAsync()
            : null;
        var branchName = ticket.BranchId.HasValue
            ? await db.Branches.Where(b => b.Id == ticket.BranchId.Value).Select(b => b.Name).SingleOrDefaultAsync()
            : null;
        var item = new TicketListItem(
            ticket.Id, ticket.CustomerId, customer.FullName,
            ticket.Subject, ticket.Description, ticket.Status, ticket.CreatedAtUtc,
            ticket.AssignedToUserId, assignee?.DisplayName,
            ticket.Category, ticket.Priority,
            responseDue, resolutionDue, ticket.FirstRespondedAtUtc, ticket.ResolvedAtUtc, IsEscalated: false,
            ticket.DepartmentId, departmentName, ticket.BranchId, branchName);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TicketUpsertRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit,
        [FromServices] IOutboundWebhookDispatcher webhooks,
        [FromServices] IRuntimeSettings runtimeSettings)
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
        if (request.DepartmentId.HasValue && !await db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value))
        {
            return BadRequest(new { error = "department_not_found" });
        }
        if (request.BranchId.HasValue && !await db.Branches.AnyAsync(b => b.Id == request.BranchId.Value))
        {
            return BadRequest(new { error = "branch_not_found" });
        }

        var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound(new { error = "ticket_not_found" });

        var previousStatus = ticket.Status;
        var previousAssigneeId = ticket.AssignedToUserId;
        var transitioningToClosed = request.Status == "Closed" && previousStatus != "Closed";

        ticket.CustomerId = request.CustomerId;
        ticket.Subject = request.Subject;
        ticket.Description = request.Description;
        ticket.Status = request.Status;
        ticket.Category = request.Category;
        ticket.Priority = request.Priority;
        ticket.AssignedToUserId = assignee?.Id;
        ticket.DepartmentId = request.DepartmentId;
        ticket.BranchId = request.BranchId;

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

        if (transitioningToClosed)
        {
            await webhooks.DispatchAsync("ticket.closed", new
            {
                id = ticket.Id, subject = ticket.Subject, status = ticket.Status,
                priority = ticket.Priority, source = ticket.Source, customerId = ticket.CustomerId,
                createdAtUtc = ticket.CreatedAtUtc,
            });
        }

        if (assignee is not null && assignee.Id != previousAssigneeId)
        {
            await CreateAssignedNotificationAsync(ticket.Id, assignee.Id, db);
        }

        if (ticket.AssignedToUserId.HasValue)
        {
            var slaTargets = await ResolveSlaTargetsAsync(runtimeSettings);
            var (responseDue, resolutionDue) = ComputeDueDates(ticket.CreatedAtUtc, ticket.Priority, slaTargets);
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
