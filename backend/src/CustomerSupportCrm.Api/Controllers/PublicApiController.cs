using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CustomerSupportCrm.Api.Controllers;

// Story 24: the one minimal, documented, read-only endpoint proving the API-key
// authentication scheme + RequireExternalClient policy + rate limiter end-to-end. This
// story deliberately does not expose any other existing business/ticket/customer data to
// external clients - see the story intake's "Out of scope".
[ApiController]
[Route("api/public")]
[Authorize(Policy = "RequireExternalClient")]
[EnableRateLimiting("ApiKeyPolicy")]
public class PublicApiController : ControllerBase
{
    public sealed record PingResponse(string Label, DateTime ServerTimeUtc);

    /// <summary>
    /// Confirms the calling API key authenticated successfully. Requires the
    /// <c>X-Api-Key</c> header; rate-limited per key.
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        var label = User.Identity!.Name ?? string.Empty;
        return Ok(new PingResponse(label, DateTime.UtcNow));
    }
}
