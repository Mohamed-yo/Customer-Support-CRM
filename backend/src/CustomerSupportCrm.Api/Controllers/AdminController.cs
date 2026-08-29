using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "RequireStaff", Roles = "Admin")]
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
        [FromQuery] int take = 100,
        // Story 15: additive filters for the global audit viewer. "take" keeps working exactly
        // as before for any existing caller that omits page/pageSize.
        [FromQuery] string? action = null,
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        // Cap "take" defensively — no pagination UI this story, but avoid runaway reads.
        if (take <= 0) take = 100;
        if (take > 500) take = 500;

        var query = db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (actorUserId.HasValue) query = query.Where(a => a.ActorUserId == actorUserId.Value);
        if (fromUtc.HasValue) query = query.Where(a => a.TimestampUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(a => a.TimestampUtc <= toUtc.Value);

        query = query.OrderByDescending(a => a.TimestampUtc);

        if (page.HasValue && pageSize.HasValue)
        {
            var effectivePageSize = pageSize.Value <= 0 ? 100 : Math.Min(pageSize.Value, 500);
            var effectivePage = page.Value <= 0 ? 1 : page.Value;
            query = query.Skip((effectivePage - 1) * effectivePageSize).Take(effectivePageSize);
        }
        else
        {
            query = query.Take(take);
        }

        var items = await query
            .Select(a => new AuditLogListItem(
                a.Id, a.TimestampUtc, a.Action, a.Outcome,
                a.ActorUserId, a.ActorEmail, a.TargetUserId, a.Details))
            .ToListAsync();

        return Ok(items);
    }

    public record RoleListItem(Guid Id, string Name);

    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles([FromServices] AppDbContext db)
    {
        var roles = await db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleListItem(r.Id, r.Name))
            .ToListAsync();
        return Ok(roles);
    }

    public record UserListItem(
        Guid Id, string Email, string DisplayName, bool IsActive, DateTime CreatedAtUtc,
        Guid? DepartmentId, string? DepartmentName, Guid? BranchId, string? BranchName,
        IReadOnlyList<string> Roles);

    // Takes the already-filtered entity query (not AppDbContext) - filtering by Id must
    // happen on the User entity, before projecting into UserListItem. A Where clause
    // applied after this projection (e.g. SingleAsync(u => u.Id == id) on the result)
    // fails to translate, because the projection's Roles list is itself a subquery.
    private static IQueryable<UserListItem> ProjectUsers(IQueryable<User> users) =>
        users.Select(u => new UserListItem(
            u.Id, u.Email, u.DisplayName, u.IsActive, u.CreatedAtUtc,
            u.DepartmentId, u.Department != null ? u.Department.Name : null,
            u.BranchId, u.Branch != null ? u.Branch.Name : null,
            u.UserRoles.Select(ur => ur.Role!.Name).OrderBy(n => n).ToList()));

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers([FromServices] AppDbContext db, [FromQuery] string? search = null)
    {
        // Filter and order on the User entity itself, before projecting - a Where/OrderBy
        // applied after ProjectUsers fails to translate for the same reason documented on
        // ProjectUsers itself (the Roles list is a subquery inside the projection).
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u => u.DisplayName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }
        query = query.OrderBy(u => u.DisplayName);

        var items = await ProjectUsers(query).ToListAsync();
        return Ok(items);
    }

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, [FromServices] AppDbContext db)
    {
        var item = await ProjectUsers(db.Users.Where(u => u.Id == id)).SingleOrDefaultAsync();
        if (item is null) return NotFound(new { error = "user_not_found" });
        return Ok(item);
    }

    public record CreateUserRequest(string Email, string DisplayName, string Password, Guid? DepartmentId, Guid? BranchId);

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        [FromServices] AppDbContext db,
        [FromServices] PasswordHasher<User> passwordHasher,
        [FromServices] AuditLogger audit)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest(new { error = "email_required" });
        if (string.IsNullOrWhiteSpace(request.DisplayName)) return BadRequest(new { error = "display_name_required" });
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(new { error = "password_too_short" });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email)) return Conflict(new { error = "email_in_use" });

        if (request.DepartmentId.HasValue && !await db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value))
        {
            return BadRequest(new { error = "department_not_found" });
        }
        if (request.BranchId.HasValue && !await db.Branches.AnyAsync(b => b.Id == request.BranchId.Value))
        {
            return BadRequest(new { error = "branch_not_found" });
        }

        var user = new User
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            DepartmentId = request.DepartmentId,
            BranchId = request.BranchId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "admin.user.create",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = user.Id,
            Details = email,
        });

        var item = await ProjectUsers(db.Users.Where(u => u.Id == user.Id)).SingleAsync();
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, item);
    }

    public record PatchUserRequest(string? DisplayName, Guid? DepartmentId, Guid? BranchId);

    [HttpPatch("users/{id:guid}")]
    public async Task<IActionResult> PatchUser(
        Guid id,
        [FromBody] PatchUserRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(new { error = "user_not_found" });

        if (request.DisplayName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName)) return BadRequest(new { error = "display_name_required" });
            user.DisplayName = request.DisplayName.Trim();
        }
        if (request.DepartmentId.HasValue)
        {
            if (!await db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value))
            {
                return BadRequest(new { error = "department_not_found" });
            }
            user.DepartmentId = request.DepartmentId.Value;
        }
        if (request.BranchId.HasValue)
        {
            if (!await db.Branches.AnyAsync(b => b.Id == request.BranchId.Value))
            {
                return BadRequest(new { error = "branch_not_found" });
            }
            user.BranchId = request.BranchId.Value;
        }

        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "admin.user.update",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = user.Id,
        });

        var item = await ProjectUsers(db.Users.Where(u => u.Id == user.Id)).SingleAsync();
        return Ok(item);
    }

    [HttpPost("users/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id, [FromServices] AppDbContext db, [FromServices] AuditLogger audit)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(new { error = "user_not_found" });

        // Prevents an admin from locking themselves out of the admin panel entirely.
        if (GetActorUserId() == user.Id) return BadRequest(new { error = "cannot_deactivate_self" });

        if (!user.IsActive) return NoContent();

        user.IsActive = false;
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "admin.user.deactivate",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = user.Id,
        });

        return NoContent();
    }

    [HttpPost("users/{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateUser(Guid id, [FromServices] AppDbContext db, [FromServices] AuditLogger audit)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(new { error = "user_not_found" });

        if (user.IsActive) return NoContent();

        user.IsActive = true;
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "admin.user.reactivate",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = user.Id,
        });

        return NoContent();
    }

    public record AssignRoleByIdRequest(Guid RoleId);

    [HttpPost("users/{id:guid}/roles")]
    public async Task<IActionResult> AssignRoleById(
        Guid id,
        [FromBody] AssignRoleByIdRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Id == request.RoleId);
        if (role is null) return NotFound(new { error = "role_not_found" });
        var user = await db.Users.Include(u => u.UserRoles).SingleOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(new { error = "user_not_found" });

        if (user.UserRoles.Any(ur => ur.RoleId == role.Id)) return NoContent();
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "admin.role.assign",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = user.Id,
            Details = role.Name,
        });

        return NoContent();
    }

    [HttpDelete("users/{id:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> RemoveRole(
        Guid id, Guid roleId, [FromServices] AppDbContext db, [FromServices] AuditLogger audit)
    {
        var user = await db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).SingleOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(new { error = "user_not_found" });

        var link = user.UserRoles.SingleOrDefault(ur => ur.RoleId == roleId);
        if (link is null) return NotFound(new { error = "role_not_assigned" });

        // Prevents an admin from removing their own Admin role and losing access to this
        // very panel with no other way to restore it short of direct DB access.
        if (GetActorUserId() == user.Id && link.Role!.Name == "Admin")
        {
            return BadRequest(new { error = "cannot_remove_own_admin_role" });
        }

        user.UserRoles.Remove(link);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "admin.role.remove",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = user.Id,
            Details = link.Role!.Name,
        });

        return NoContent();
    }

    private Guid? GetActorUserId()
    {
        var sub = User.FindFirst("sub");
        return sub is not null && Guid.TryParse(sub.Value, out var parsed) ? parsed : null;
    }
}
