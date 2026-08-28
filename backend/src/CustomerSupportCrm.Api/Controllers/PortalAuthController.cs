using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Auth;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/portal/auth")]
public class PortalAuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<Customer> _passwordHasher;
    private readonly JwtTokenService _tokenService;
    private readonly AuditLogger _audit;

    public PortalAuthController(
        AppDbContext db,
        PasswordHasher<Customer> passwordHasher,
        JwtTokenService tokenService,
        AuditLogger audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _audit = audit;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] CustomerRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { error = "full_name_required" });
        }
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "email_required" });
        }
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(new { error = "password_too_short" });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _db.Customers.SingleOrDefaultAsync(c => c.Email.ToLower() == email);

        Customer customer;
        if (existing is null)
        {
            customer = new Customer
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
            };
            customer.PasswordHash = _passwordHasher.HashPassword(customer, request.Password);
            _db.Customers.Add(customer);
        }
        else if (existing.PasswordHash is not null)
        {
            // A password is already set on this email - registering again is a collision,
            // not an upgrade.
            return Conflict(new { error = "email_already_registered" });
        }
        else
        {
            // Pre-existing staff-created customer record with no login yet: set the
            // password on it. Do not overwrite FullName/Phone - that data was entered by
            // staff and may be more accurate than what this form carries.
            customer = existing;
            customer.PasswordHash = _passwordHasher.HashPassword(customer, request.Password);
        }

        await _db.SaveChangesAsync();

        await _audit.WriteAsync(new AuditLog
        {
            Action = "portal.customer.register",
            Outcome = "success",
            ActorUserId = customer.Id,
            ActorEmail = customer.Email,
        });

        var token = _tokenService.IssueToken(
            customer.Id, customer.Email, customer.FullName, "customer", Array.Empty<string>(), out var expiresAtUtc);
        return Ok(new CustomerLoginResponse(customer.Id, token, customer.Email, customer.FullName, expiresAtUtc));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] CustomerLoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Email.ToLower() == email);

        if (customer is null || customer.PasswordHash is null)
        {
            await _audit.WriteAsync(new AuditLog
            {
                Action = "portal.customer.login",
                Outcome = "failure",
                ActorEmail = email,
                Details = customer is null ? "customer_not_found" : "no_password_set",
            });
            return Unauthorized(new { error = "invalid_credentials" });
        }

        var result = _passwordHasher.VerifyHashedPassword(customer, customer.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            await _audit.WriteAsync(new AuditLog
            {
                Action = "portal.customer.login",
                Outcome = "failure",
                ActorUserId = customer.Id,
                ActorEmail = email,
                Details = "invalid_password",
            });
            return Unauthorized(new { error = "invalid_credentials" });
        }

        await _audit.WriteAsync(new AuditLog
        {
            Action = "portal.customer.login",
            Outcome = "success",
            ActorUserId = customer.Id,
            ActorEmail = email,
        });

        var token = _tokenService.IssueToken(
            customer.Id, customer.Email, customer.FullName, "customer", Array.Empty<string>(), out var expiresAtUtc);
        return Ok(new CustomerLoginResponse(customer.Id, token, customer.Email, customer.FullName, expiresAtUtc));
    }

    [HttpGet("me")]
    [Authorize(Policy = "RequireCustomer")]
    public async Task<IActionResult> Me()
    {
        var idClaim = User.FindFirst("sub");
        if (idClaim is null || !Guid.TryParse(idClaim.Value, out var customerId))
        {
            return Unauthorized();
        }

        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == customerId);
        if (customer is null)
        {
            return Unauthorized();
        }

        return Ok(new CustomerMeResponse(customer.Id, customer.Email, customer.FullName));
    }
}
