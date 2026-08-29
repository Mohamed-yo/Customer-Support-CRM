using System.Security.Claims;
using System.Text.Encodings.Web;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustomerSupportCrm.Api.Auth;

// Story 24: additive authentication scheme, registered alongside (never replacing) the
// existing JWT bearer scheme in Program.cs. Validates the X-Api-Key header against
// hashed ApiKey rows and emits a claims identity satisfying the "RequireExternalClient"
// policy only - it can never satisfy RequireStaff/RequireCustomer, which key off the JWT
// "type" claim this scheme never issues.
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    private const string HeaderName = "X-Api-Key";

    private readonly AppDbContext _db;
    private readonly PasswordHasher<ApiKey> _hasher;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext db,
        PasswordHasher<ApiKey> hasher)
        : base(options, logger, encoder)
    {
        _db = db;
        _hasher = hasher;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var plaintext = headerValues.ToString();
        if (string.IsNullOrEmpty(plaintext))
        {
            return AuthenticateResult.NoResult();
        }

        // Prefix narrows the candidate set; it is not assumed unique (see ApiKey.Prefix).
        var prefix = plaintext[..Math.Min(12, plaintext.Length)];
        var candidates = await _db.ApiKeys
            .Where(k => k.Prefix == prefix && k.RevokedAtUtc == null)
            .ToListAsync();

        ApiKey? matched = null;
        foreach (var candidate in candidates)
        {
            var result = _hasher.VerifyHashedPassword(candidate, candidate.KeyHash, plaintext);
            if (result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
            {
                matched = candidate;
                break;
            }
        }

        if (matched is null)
        {
            // Deliberately generic - never reveals whether the prefix matched a
            // revoked/unknown key vs. a wrong secret.
            return AuthenticateResult.Fail("Invalid API key");
        }

        matched.LastUsedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, matched.Id.ToString()),
            new Claim(ClaimTypes.Name, matched.Label),
            new Claim("external_client", "true"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
