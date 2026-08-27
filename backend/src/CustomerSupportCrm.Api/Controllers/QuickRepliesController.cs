using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/quick-replies")]
[Authorize]
public class QuickRepliesController : ControllerBase
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
        var items = await db.QuickReplyTemplates
            .OrderBy(q => q.Title)
            .Select(q => new QuickReplyTemplateItem(
                q.Id, q.Title, q.Body, q.CreatedByUserId, q.CreatedByUser!.DisplayName, q.CreatedAtUtc, q.UpdatedAtUtc))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromServices] AppDbContext db)
    {
        var item = await db.QuickReplyTemplates
            .Where(q => q.Id == id)
            .Select(q => new QuickReplyTemplateItem(
                q.Id, q.Title, q.Body, q.CreatedByUserId, q.CreatedByUser!.DisplayName, q.CreatedAtUtc, q.UpdatedAtUtc))
            .SingleOrDefaultAsync();

        if (item is null) return NotFound(new { error = "quick_reply_not_found" });
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuickReplyTemplateUpsertRequest request, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "title_required" });
        }
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(new { error = "body_required" });
        }

        var actorId = GetActorUserId();
        if (actorId is null) return Unauthorized();

        var template = new QuickReplyTemplate
        {
            Title = request.Title,
            Body = request.Body,
            CreatedByUserId = actorId.Value,
        };
        db.QuickReplyTemplates.Add(template);
        await db.SaveChangesAsync();

        var creator = await db.Users.Where(u => u.Id == actorId.Value).Select(u => u.DisplayName).SingleAsync();
        var item = new QuickReplyTemplateItem(
            template.Id, template.Title, template.Body, template.CreatedByUserId, creator, template.CreatedAtUtc, template.UpdatedAtUtc);
        return CreatedAtAction(nameof(Get), new { id = template.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] QuickReplyTemplateUpsertRequest request, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "title_required" });
        }
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(new { error = "body_required" });
        }

        var template = await db.QuickReplyTemplates.SingleOrDefaultAsync(q => q.Id == id);
        if (template is null) return NotFound(new { error = "quick_reply_not_found" });

        template.Title = request.Title;
        template.Body = request.Body;
        template.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromServices] AppDbContext db)
    {
        var template = await db.QuickReplyTemplates.SingleOrDefaultAsync(q => q.Id == id);
        if (template is null) return NotFound(new { error = "quick_reply_not_found" });

        db.QuickReplyTemplates.Remove(template);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
