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

    public AuthController(AppDbContext db, PasswordHasher<User> passwordHasher, JwtTokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            return Unauthorized(new { error = "invalid_credentials" });
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = "invalid_credentials" });
        }

        var token = _tokenService.IssueToken(user, out var expiresAtUtc);
        return Ok(new LoginResponse(token, user.Email, user.DisplayName, expiresAtUtc));
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

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new MeResponse(user.Id, user.Email, user.DisplayName));
    }
}
