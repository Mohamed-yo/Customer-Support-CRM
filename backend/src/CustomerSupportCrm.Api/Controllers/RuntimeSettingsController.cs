using System.Text.Json;
using CustomerSupportCrm.Api.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupportCrm.Api.Controllers;

// Story 15: generic admin GET/PUT surface over RuntimeSettings, one key at a time. Branding
// (RuntimeSettingKeys.Branding) is deliberately not exposed here - BrandingController owns
// that key so it can enforce the logo-size cap and serve it anonymously.
[ApiController]
[Route("api/runtime-settings")]
[Authorize(Policy = "RequireStaff", Roles = "Admin")]
public class RuntimeSettingsController : ControllerBase
{
    private static readonly string[] EditableKeys = { RuntimeSettingKeys.SlaTargets, RuntimeSettingKeys.ReminderLeadHrs };

    private Guid? GetActorUserId()
    {
        var sub = User.FindFirst("sub");
        return sub is not null && Guid.TryParse(sub.Value, out var parsed) ? parsed : null;
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, [FromServices] IRuntimeSettings runtimeSettings)
    {
        if (!EditableKeys.Contains(key)) return NotFound(new { error = "setting_not_found" });

        object value = key switch
        {
            RuntimeSettingKeys.SlaTargets => await runtimeSettings.GetAsync(
                key, new Dictionary<string, SlaTargetSetting>(TicketsController.DefaultSlaTargets, StringComparer.Ordinal)),
            RuntimeSettingKeys.ReminderLeadHrs => await runtimeSettings.GetAsync(key, new ReminderLeadTimeSetting()),
            _ => throw new InvalidOperationException("Unreachable: key already validated against EditableKeys."),
        };

        return Ok(new { key, value });
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Put(string key, [FromBody] JsonElement body, [FromServices] IRuntimeSettings runtimeSettings)
    {
        if (!EditableKeys.Contains(key)) return NotFound(new { error = "setting_not_found" });

        if (key == RuntimeSettingKeys.SlaTargets)
        {
            Dictionary<string, SlaTargetSetting>? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<Dictionary<string, SlaTargetSetting>>(body.GetRawText());
            }
            catch (JsonException)
            {
                return BadRequest(new { error = "value_invalid" });
            }
            if (parsed is null) return BadRequest(new { error = "value_invalid" });

            foreach (var requiredPriority in TicketsController.DefaultSlaTargets.Keys)
            {
                if (!parsed.TryGetValue(requiredPriority, out var target) ||
                    target.ResponseHours <= 0 || target.ResolutionHours <= 0)
                {
                    return BadRequest(new { error = "value_invalid" });
                }
            }

            await runtimeSettings.SetAsync(key, parsed, GetActorUserId());
            return NoContent();
        }

        // RuntimeSettingKeys.ReminderLeadHrs
        {
            ReminderLeadTimeSetting? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<ReminderLeadTimeSetting>(body.GetRawText());
            }
            catch (JsonException)
            {
                return BadRequest(new { error = "value_invalid" });
            }
            if (parsed is null || parsed.Hours <= 0) return BadRequest(new { error = "value_invalid" });

            await runtimeSettings.SetAsync(key, parsed, GetActorUserId());
            return NoContent();
        }
    }
}
