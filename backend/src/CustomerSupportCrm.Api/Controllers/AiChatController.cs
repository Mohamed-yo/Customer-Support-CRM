using CustomerSupportCrm.Api.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupportCrm.Api.Controllers;

// Story 15 Phase 6: a distinct, anonymous self-service AI chat entry point - deliberately
// does NOT reuse ChatHub (that's the staff/customer live-chat SignalR hub from Story 12;
// this is a stateless request/response AI conversation, unrelated to it).
[ApiController]
[Route("api/ai/chat")]
[AllowAnonymous]
public class AiChatController : ControllerBase
{
    public record ChatRequest(string SessionId, string Message);

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, [FromServices] IAiProvider ai)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "session_id_and_message_required" });
        }

        var result = await ai.ChatAsync(request.SessionId, request.Message);
        return Ok(new
        {
            status = result.Status.ToString(),
            value = result.Status == AiStatus.Ok ? result.Value : null,
            error = result.Status == AiStatus.Ok ? null : result.Detail,
        });
    }
}
