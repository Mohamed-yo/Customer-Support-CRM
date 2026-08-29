using System.Text.RegularExpressions;
using CustomerSupportCrm.Api.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupportCrm.Api.Controllers;

// Story 15 Phase 7: unlike every other controller in this codebase, GET here must be
// anonymous (login page, portal pages, and the customer widget all render branding before
// any authentication exists) while PUT stays Admin-only - so authorization is set per-action
// instead of the usual single class-level [Authorize].
[ApiController]
[Route("api/branding")]
public class BrandingController : ControllerBase
{
    private const int MaxLogoBytes = 256 * 1024;
    private static readonly Regex HexColorPattern = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    private Guid? GetActorUserId()
    {
        var sub = User.FindFirst("sub");
        return sub is not null && Guid.TryParse(sub.Value, out var parsed) ? parsed : null;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromServices] IBrandingService brandingService)
    {
        var settings = await brandingService.GetAsync();
        return Ok(settings);
    }

    [HttpPut]
    [Authorize(Policy = "RequireStaff", Roles = "Admin")]
    public async Task<IActionResult> Put([FromBody] BrandingSettings request, [FromServices] IBrandingService brandingService)
    {
        if (string.IsNullOrWhiteSpace(request.AppName)) return BadRequest(new { error = "app_name_required" });

        if (!string.IsNullOrEmpty(request.LogoDataUrl))
        {
            // Cheap upper-bound on the base64 payload without decoding it - real bytes are
            // ~3/4 of the encoded string length.
            var approxBytes = request.LogoDataUrl.Length * 3 / 4;
            if (approxBytes > MaxLogoBytes) return BadRequest(new { error = "logo_too_large" });
        }

        if (!string.IsNullOrEmpty(request.PrimaryColorHex) && !HexColorPattern.IsMatch(request.PrimaryColorHex))
        {
            return BadRequest(new { error = "primary_color_invalid" });
        }

        await brandingService.SetAsync(
            new BrandingSettings
            {
                AppName = request.AppName.Trim(),
                LogoDataUrl = request.LogoDataUrl,
                PrimaryColorHex = request.PrimaryColorHex,
            },
            GetActorUserId());

        return NoContent();
    }
}
