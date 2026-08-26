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
    public async Task<IActionResult> AssignRole(Guid userId, string roleName, [FromServices] AppDbContext db)
    {
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == roleName);
        if (role is null) return NotFound(new { error = "role_not_found" });
        var user = await db.Users.Include(u => u.UserRoles).SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound(new { error = "user_not_found" });
        if (user.UserRoles.Any(ur => ur.RoleId == role.Id)) return NoContent();
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        return NoContent();
    }
}
