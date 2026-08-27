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
    private static readonly string[] AllowedStatuses = { "Open", "InProgress", "Closed" };

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
    public async Task<IActionResult> List([FromServices] AppDbContext db)
    {
        var items = await db.Tickets
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TicketListItem(
                t.Id, t.CustomerId, t.Customer!.FullName,
                t.Subject, t.Description, t.Status, t.CreatedAtUtc,
                t.AssignedToUserId,
                t.AssignedToUser != null ? t.AssignedToUser.DisplayName : null))
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
                t.AssignedToUser != null ? t.AssignedToUser.DisplayName : null))
            .SingleOrDefaultAsync();

        if (item is null) return NotFound(new { error = "ticket_not_found" });
        return Ok(item);
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
            ticket.AssignedToUserId, assignee?.DisplayName);
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
