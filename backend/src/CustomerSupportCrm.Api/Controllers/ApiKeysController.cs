using System.Security.Cryptography;
using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

// Story 24: Admin-only issuance/management of API keys for the additive "ApiKey"
// authentication scheme (Program.cs) - mirrors WebhookSubscriptionsController.cs's exact
// RBAC shape.
[ApiController]
[Route("api/api-keys")]
[Authorize(Policy = "RequireStaff", Roles = "Admin")]
public class ApiKeysController : ControllerBase
{
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
        var items = await db.ApiKeys
            .OrderByDescending(k => k.CreatedAtUtc)
            .Select(k => new ApiKeyListItem(
                k.Id, k.Label, k.Prefix, k.CreatedAtUtc, k.LastUsedAtUtc, k.RevokedAtUtc, k.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateApiKeyRequest request,
        [FromServices] AppDbContext db,
        [FromServices] PasswordHasher<ApiKey> hasher,
        [FromServices] AuditLogger audit)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
        {
            return BadRequest(new { error = "label_required" });
        }

        var actorId = GetActorUserId();
        if (actorId is null) return Unauthorized();

        // Cryptographically random 32-byte secret, base64url-encoded (no padding, no
        // '+'/'/' that would need escaping in a header value), prefixed for at-a-glance
        // identification (e.g. in logs) without revealing the secret.
        var plaintext = "csk_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var prefix = plaintext[..Math.Min(12, plaintext.Length)];

        var apiKey = new ApiKey
        {
            Label = request.Label,
            Prefix = prefix,
            CreatedByUserId = actorId.Value,
        };
        apiKey.KeyHash = hasher.HashPassword(apiKey, plaintext);

        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "apikey.create",
            Outcome = "success",
            ActorUserId = actorId,
            Details = apiKey.Id.ToString(),
        });

        // The only place the plaintext is ever returned. Never persisted or retrievable
        // again after this response.
        return CreatedAtAction(
            nameof(List),
            new CreateApiKeyResponse(apiKey.Id, apiKey.Label, apiKey.Prefix, plaintext, apiKey.CreatedAtUtc));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(
        Guid id,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        var apiKey = await db.ApiKeys.SingleOrDefaultAsync(k => k.Id == id);
        if (apiKey is null || apiKey.RevokedAtUtc is not null)
        {
            return NotFound(new { error = "api_key_not_found" });
        }

        apiKey.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "apikey.revoke",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            Details = id.ToString(),
        });

        return NoContent();
    }
}
