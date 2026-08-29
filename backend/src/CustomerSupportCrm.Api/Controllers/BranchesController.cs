using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

// Story 15 Phase 7: mirrors DepartmentsController exactly - see its summary for the
// deactivate/reactivate (not hard delete) rationale.
[ApiController]
[Route("api/branches")]
[Authorize(Policy = "RequireStaff", Roles = "Admin")]
public class BranchesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db)
    {
        var items = await db.Branches
            .OrderBy(b => b.Name)
            .Select(b => new BranchItem(b.Id, b.Name, b.IsActive, b.CreatedAtUtc))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromServices] AppDbContext db)
    {
        var item = await db.Branches
            .Where(b => b.Id == id)
            .Select(b => new BranchItem(b.Id, b.Name, b.IsActive, b.CreatedAtUtc))
            .SingleOrDefaultAsync();
        if (item is null) return NotFound(new { error = "branch_not_found" });
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BranchUpsertRequest request, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "name_required" });

        var name = request.Name.Trim();
        if (await db.Branches.AnyAsync(b => b.Name == name)) return Conflict(new { error = "name_in_use" });

        var branch = new Branch { Name = name };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var item = new BranchItem(branch.Id, branch.Name, branch.IsActive, branch.CreatedAtUtc);
        return CreatedAtAction(nameof(Get), new { id = branch.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BranchUpsertRequest request, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "name_required" });

        var branch = await db.Branches.SingleOrDefaultAsync(b => b.Id == id);
        if (branch is null) return NotFound(new { error = "branch_not_found" });

        var name = request.Name.Trim();
        if (await db.Branches.AnyAsync(b => b.Name == name && b.Id != id)) return Conflict(new { error = "name_in_use" });

        branch.Name = name;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, [FromServices] AppDbContext db)
    {
        var branch = await db.Branches.SingleOrDefaultAsync(b => b.Id == id);
        if (branch is null) return NotFound(new { error = "branch_not_found" });

        branch.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, [FromServices] AppDbContext db)
    {
        var branch = await db.Branches.SingleOrDefaultAsync(b => b.Id == id);
        if (branch is null) return NotFound(new { error = "branch_not_found" });

        branch.IsActive = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
