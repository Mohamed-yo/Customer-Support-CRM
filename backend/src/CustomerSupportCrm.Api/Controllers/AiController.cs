using System.Text;
using CustomerSupportCrm.Api.Ai;
using CustomerSupportCrm.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

// Story 15 Phase 6: staff-facing AI features over the provider-agnostic IAiProvider.
// Every response carries {status, value, error} so the frontend can render a degraded
// state (NotConfigured) as a normal, expected response - never an HTTP error.
[ApiController]
[Route("api/ai")]
[Authorize(Policy = "RequireStaff")]
public class AiController : ControllerBase
{
    private static async Task<string?> BuildTicketContentAsync(Guid ticketId, AppDbContext db)
    {
        var ticket = await db.Tickets
            .Where(t => t.Id == ticketId)
            .Select(t => new { t.Subject, t.Description, t.Category, t.Priority })
            .SingleOrDefaultAsync();
        if (ticket is null) return null;

        var notes = await db.TicketNotes
            .Where(n => n.TicketId == ticketId)
            .OrderBy(n => n.CreatedAtUtc)
            .Select(n => n.Body)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"Subject: {ticket.Subject}");
        if (!string.IsNullOrWhiteSpace(ticket.Description)) sb.AppendLine($"Description: {ticket.Description}");
        sb.AppendLine($"Category: {ticket.Category}, Priority: {ticket.Priority}");
        foreach (var note in notes)
        {
            sb.AppendLine($"Note: {note}");
        }
        return sb.ToString();
    }

    private static object ToResponse(AiResult<string> result) => new
    {
        status = result.Status.ToString(),
        value = result.Status == AiStatus.Ok ? result.Value : null,
        error = result.Status == AiStatus.Ok ? null : result.Detail,
    };

    [HttpPost("tickets/{ticketId:guid}/summarize")]
    public async Task<IActionResult> Summarize(Guid ticketId, [FromServices] AppDbContext db, [FromServices] IAiProvider ai)
    {
        var content = await BuildTicketContentAsync(ticketId, db);
        if (content is null) return NotFound(new { error = "ticket_not_found" });

        var result = await ai.SummarizeTicketAsync(content);
        return Ok(ToResponse(result));
    }

    [HttpPost("tickets/{ticketId:guid}/suggest-reply")]
    public async Task<IActionResult> SuggestReply(Guid ticketId, [FromServices] AppDbContext db, [FromServices] IAiProvider ai)
    {
        var content = await BuildTicketContentAsync(ticketId, db);
        if (content is null) return NotFound(new { error = "ticket_not_found" });

        var result = await ai.SuggestReplyAsync(content);
        return Ok(ToResponse(result));
    }

    [HttpPost("tickets/{ticketId:guid}/suggest-category")]
    public async Task<IActionResult> SuggestCategory(Guid ticketId, [FromServices] AppDbContext db, [FromServices] IAiProvider ai)
    {
        var content = await BuildTicketContentAsync(ticketId, db);
        if (content is null) return NotFound(new { error = "ticket_not_found" });

        var result = await ai.SuggestCategoryAsync(content, TicketsController.AllowedCategories);
        return Ok(ToResponse(result));
    }

    [HttpPost("tickets/{ticketId:guid}/suggest-kb-articles")]
    public async Task<IActionResult> SuggestKbArticles(Guid ticketId, [FromServices] AppDbContext db, [FromServices] IAiProvider ai)
    {
        var content = await BuildTicketContentAsync(ticketId, db);
        if (content is null) return NotFound(new { error = "ticket_not_found" });

        var candidates = await db.KnowledgeArticles
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(50)
            .Select(a => new KbArticleCandidate(a.Id, a.Title))
            .ToListAsync();

        var result = await ai.SuggestKbArticlesAsync(content, candidates);
        if (result.Status != AiStatus.Ok || result.Value is null)
        {
            return Ok(new { status = result.Status.ToString(), value = (object?)null, error = result.Detail });
        }

        var suggestedIds = result.Value;
        var articles = await db.KnowledgeArticles
            .Where(a => suggestedIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Title })
            .ToListAsync();

        return Ok(new { status = result.Status.ToString(), value = articles, error = (string?)null });
    }
}
