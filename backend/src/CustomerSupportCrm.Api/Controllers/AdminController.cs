using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { ok = true });

    [HttpPost("users/{userId:guid}/roles/{roleName}")]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        string roleName,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == roleName);
        if (role is null) return NotFound(new { error = "role_not_found" });
        var user = await db.Users.Include(u => u.UserRoles).SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound(new { error = "user_not_found" });
        // Idempotent short-circuit — nothing mutated, so nothing to audit.
        if (user.UserRoles.Any(ur => ur.RoleId == role.Id)) return NoContent();
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();

        Guid? actorId = null;
        var sub = User.FindFirst("sub");
        if (sub is not null && Guid.TryParse(sub.Value, out var parsed))
        {
            actorId = parsed;
        }

        await audit.WriteAsync(new AuditLog
        {
            Action = "admin.role.assign",
            Outcome = "success",
            ActorUserId = actorId,
            TargetUserId = userId,
            Details = roleName,
        });

        return NoContent();
    }

    public record AuditLogListItem(
        Guid Id,
        DateTime TimestampUtc,
        string Action,
        string Outcome,
        Guid? ActorUserId,
        string? ActorEmail,
        Guid? TargetUserId,
        string? Details);

    [HttpGet("audit-logs")]
    public async Task<IActionResult> ListAuditLogs(
        [FromServices] AppDbContext db,
        [FromQuery] int take = 100)
    {
        // Cap "take" defensively — no pagination UI this story, but avoid runaway reads.
        if (take <= 0) take = 100;
        if (take > 500) take = 500;

        var items = await db.AuditLogs
            .OrderByDescending(a => a.TimestampUtc)
            .Take(take)
            .Select(a => new AuditLogListItem(
                a.Id, a.TimestampUtc, a.Action, a.Outcome,
                a.ActorUserId, a.ActorEmail, a.TargetUserId, a.Details))
            .ToListAsync();

        return Ok(items);
    }
}
