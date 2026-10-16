using System.Security.Cryptography;
using System.Text;
using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Auth;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/portal/auth")]
public class PortalAuthController : ControllerBase
{
    private static readonly TimeSpan VerificationLinkLifetime = TimeSpan.FromHours(24);

    private readonly AppDbContext _db;
    private readonly PasswordHasher<Customer> _passwordHasher;
    private readonly JwtTokenService _tokenService;
    private readonly AuditLogger _audit;
    private readonly PortalOptions _portalOptions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PortalAuthController> _logger;

    public PortalAuthController(
        AppDbContext db,
        PasswordHasher<Customer> passwordHasher,
        JwtTokenService tokenService,
        AuditLogger audit,
        IOptions<PortalOptions> portalOptions,
        IHostEnvironment environment,
        ILogger<PortalAuthController> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _audit = audit;
        _portalOptions = portalOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] CustomerRegisterRequest request,
        [FromKeyedServices("Email")] IChannelSender emailSender)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { error = "full_name_required" });
        }
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "email_required" });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == email);
        if (customer?.PasswordHash is not null)
        {
            // A password is already set on this email - registering again is a collision,
            // not an upgrade.
            return Conflict(new { error = "email_already_registered" });
        }

        if (customer is null)
        {
            customer = new Customer
            {
                FullName = request.FullName,
                Email = request.Email.Trim(),
                Phone = request.Phone,
            };
            _db.Customers.Add(customer);
        }
        // An existing passwordless record (staff/channel-created, or an earlier unfinished
        // registration) is never modified here beyond the pending token: whoever is calling
        // has not yet proven they own this mailbox. Issuing a new token replaces any older link.
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        customer.EmailVerificationTokenHash = HashToken(token);
        customer.EmailVerificationExpiresAtUtc = DateTime.UtcNow.Add(VerificationLinkLifetime);
        await _db.SaveChangesAsync();

        await _audit.WriteAsync(new AuditLog
        {
            Action = "portal.customer.register",
            Outcome = "verification_sent",
            ActorUserId = customer.Id,
            ActorEmail = customer.Email,
        });

        var link = $"{_portalOptions.VerifyEmailUrl}?token={token}";
        var sendResult = await emailSender.SendAsync(
            customer.Email!,
            "Verify your email",
            $"Open this link to choose your password and finish creating your account:\n\n{link}\n\n" +
            "The link expires in 24 hours. If you didn't request this, ignore this email.");
        if (sendResult.Status != SendStatus.Success)
        {
            if (!_environment.IsDevelopment())
            {
                _logger.LogError(
                    "Portal verification email to customer {CustomerId} not sent: {Status} {Detail}",
                    customer.Id, sendResult.Status, sendResult.Detail);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "verification_email_unavailable" });
            }
            // Development only - SMTP is usually unconfigured locally. Never log the link elsewhere.
            _logger.LogWarning(
                "Portal verification email not sent ({Status}). Dev verification link: {Link}",
                sendResult.Status, link);
        }

        return Accepted(new { status = "verification_sent" });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] CustomerVerifyEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(new { error = "password_too_short" });
        }
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { error = "token_invalid" });
        }

        var tokenHash = HashToken(request.Token.Trim().ToLowerInvariant());
        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.EmailVerificationTokenHash == tokenHash);
        // Unknown, replaced, already-used and expired links all get the same response.
        if (customer is null || customer.Email is null || customer.PasswordHash is not null ||
            customer.EmailVerificationExpiresAtUtc is null || customer.EmailVerificationExpiresAtUtc < DateTime.UtcNow)
        {
            return BadRequest(new { error = "token_invalid" });
        }

        customer.PasswordHash = _passwordHasher.HashPassword(customer, request.Password);
        customer.EmailVerificationTokenHash = null;
        customer.EmailVerificationExpiresAtUtc = null;
        await _db.SaveChangesAsync();

        await _audit.WriteAsync(new AuditLog
        {
            Action = "portal.customer.verify_email",
            Outcome = "success",
            ActorUserId = customer.Id,
            ActorEmail = customer.Email,
        });

        var jwt = _tokenService.IssueToken(
            customer.Id, customer.Email, customer.FullName, "customer", Array.Empty<string>(), out var expiresAtUtc);
        return Ok(new CustomerLoginResponse(customer.Id, jwt, customer.Email, customer.FullName, expiresAtUtc));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] CustomerLoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == email);

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

        // customer.Email is provably non-null here: found via the c.Email != null lookup
        // above, and PasswordHash (checked above) is only ever set alongside a real email.
        var token = _tokenService.IssueToken(
            customer.Id, customer.Email!, customer.FullName, "customer", Array.Empty<string>(), out var expiresAtUtc);
        return Ok(new CustomerLoginResponse(customer.Id, token, customer.Email!, customer.FullName, expiresAtUtc));
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

        // Reachable only via a "customer" JWT, which VerifyEmail/Login only ever issue to a
        // customer with a real Email - a phone-only customer has no PasswordHash to log in.
        return Ok(new CustomerMeResponse(customer.Id, customer.Email!, customer.FullName));
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
