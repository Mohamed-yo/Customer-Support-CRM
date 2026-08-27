using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private const long MaxAttachmentSizeBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedStatuses = { "Open", "InProgress", "Closed" };
    private static readonly string[] AllowedCategories = { "General", "Billing", "Technical", "Account" };
    private static readonly string[] AllowedPriorities = { "Low", "Normal", "High", "Urgent" };

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

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TicketListItem(
                t.Id, t.CustomerId, t.Customer!.FullName,
                t.Subject, t.Description, t.Status, t.CreatedAtUtc,
                t.AssignedToUserId,
                t.AssignedToUser != null ? t.AssignedToUser.DisplayName : null,
                t.Category, t.Priority))
            .ToListAsync();

        return Ok(items);
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
        var item = await db.Tickets
            .Where(t => t.Id == id)
            .Select(t => new TicketListItem(
                t.Id, t.CustomerId, t.Customer!.FullName,
                t.Subject, t.Description, t.Status, t.CreatedAtUtc,
                t.AssignedToUserId,
                t.AssignedToUser != null ? t.AssignedToUser.DisplayName : null,
                t.Category, t.Priority))
            .SingleOrDefaultAsync();

        if (item is null) return NotFound(new { error = "ticket_not_found" });
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
        var ticket = await db.Tickets.AnyAsync(t => t.Id == id);
        if (!ticket) return NotFound(new { error = "ticket_not_found" });

        var actorId = GetActorUserId();
        if (actorId is null) return Unauthorized();

        var note = new TicketNote
        {
            TicketId = id,
            AuthorUserId = actorId.Value,
            Body = request.Body,
        };
        db.TicketNotes.Add(note);
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

        var item = new TicketListItem(
            ticket.Id, ticket.CustomerId, customer.FullName,
            ticket.Subject, ticket.Description, ticket.Status, ticket.CreatedAtUtc,
            ticket.AssignedToUserId, assignee?.DisplayName,
            ticket.Category, ticket.Priority);
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

        ticket.CustomerId = request.CustomerId;
        ticket.Subject = request.Subject;
        ticket.Description = request.Description;
        ticket.Status = request.Status;
        ticket.Category = request.Category;
        ticket.Priority = request.Priority;
        ticket.AssignedToUserId = assignee?.Id;
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "ticket.update",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = null,
            Details = ticket.Id.ToString(),
        });

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
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
