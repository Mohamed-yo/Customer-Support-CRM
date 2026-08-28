using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/knowledge-articles")]
[Authorize]
public class KnowledgeArticlesController : ControllerBase
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

    // Reachable by both staff and customer tokens (bare [Authorize], no policy) - Knowledge
    // Base content is read-accessible to both audiences; only writes are staff-only.
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db, [FromQuery] string? q)
    {
        var query = db.KnowledgeArticles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(a => a.Title.Contains(q) || a.Body.Contains(q));
        }

        var items = await query
            .OrderByDescending(a => a.UpdatedAtUtc ?? a.CreatedAtUtc)
            .Select(a => new KnowledgeArticleListItem(a.Id, a.Title, a.CreatedAtUtc, a.UpdatedAtUtc))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromServices] AppDbContext db)
    {
        var item = await db.KnowledgeArticles
            .Where(a => a.Id == id)
            .Select(a => new KnowledgeArticleItem(
                a.Id, a.Title, a.Body,
                a.CreatedByUserId, a.CreatedByUser!.DisplayName, a.CreatedAtUtc,
                a.UpdatedByUserId, a.UpdatedByUser == null ? null : a.UpdatedByUser.DisplayName, a.UpdatedAtUtc))
            .SingleOrDefaultAsync();

        if (item is null) return NotFound(new { error = "article_not_found" });
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = "RequireStaff")]
    public async Task<IActionResult> Create(
        [FromBody] KnowledgeArticleUpsertRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
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

        var article = new KnowledgeArticle
        {
            Title = request.Title,
            Body = request.Body,
            CreatedByUserId = actorId.Value,
        };
        db.KnowledgeArticles.Add(article);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "kb.article.create",
            Outcome = "success",
            ActorUserId = actorId,
            Details = article.Id.ToString(),
        });

        var creator = await db.Users.Where(u => u.Id == actorId.Value).Select(u => u.DisplayName).SingleAsync();
        var item = new KnowledgeArticleItem(
            article.Id, article.Title, article.Body,
            article.CreatedByUserId, creator, article.CreatedAtUtc,
            null, null, null);
        return CreatedAtAction(nameof(Get), new { id = article.Id }, item);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireStaff")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] KnowledgeArticleUpsertRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
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

        var article = await db.KnowledgeArticles.SingleOrDefaultAsync(a => a.Id == id);
        if (article is null) return NotFound(new { error = "article_not_found" });

        article.Title = request.Title;
        article.Body = request.Body;
        article.UpdatedByUserId = actorId.Value;
        article.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "kb.article.update",
            Outcome = "success",
            ActorUserId = actorId,
            Details = article.Id.ToString(),
        });

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireStaff")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        var article = await db.KnowledgeArticles.SingleOrDefaultAsync(a => a.Id == id);
        if (article is null) return NotFound(new { error = "article_not_found" });

        db.KnowledgeArticles.Remove(article);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "kb.article.delete",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            Details = id.ToString(),
        });

        return NoContent();
    }
}
