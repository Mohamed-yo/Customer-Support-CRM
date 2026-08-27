using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly JwtTokenService _tokenService;
    private readonly AuditLogger _audit;

    public AuthController(
        AppDbContext db,
        PasswordHasher<User> passwordHasher,
        JwtTokenService tokenService,
        AuditLogger audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _audit = audit;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            await _audit.WriteAsync(new AuditLog
            {
                Action = "auth.login",
                Outcome = "failure",
                ActorEmail = email,
                Details = "user_not_found",
            });
            return Unauthorized(new { error = "invalid_credentials" });
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            await _audit.WriteAsync(new AuditLog
            {
                Action = "auth.login",
                Outcome = "failure",
                ActorUserId = user.Id,
                ActorEmail = email,
                Details = "invalid_password",
            });
            return Unauthorized(new { error = "invalid_credentials" });
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n).ToList();
        var token = _tokenService.IssueToken(user, roles, out var expiresAtUtc);
        await _audit.WriteAsync(new AuditLog
        {
            Action = "auth.login",
            Outcome = "success",
            ActorUserId = user.Id,
            ActorEmail = email,
        });
        return Ok(new LoginResponse(token, user.Email, user.DisplayName, expiresAtUtc, roles));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var idClaim = User.FindFirst("sub");
        if (idClaim is null || !Guid.TryParse(idClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n).ToList();
        return Ok(new MeResponse(user.Id, user.Email, user.DisplayName, roles));
    }
}
