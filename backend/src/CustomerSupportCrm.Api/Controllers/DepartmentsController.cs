using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

// Story 15 Phase 7: structural shape borrowed from QuickRepliesController - Admin-only CRUD,
// but "delete" is deactivate/reactivate (not a hard delete) since Users/Tickets reference a
// Department via a SetNull FK and a lookup value already in use should not vanish silently.
[ApiController]
[Route("api/departments")]
[Authorize(Policy = "RequireStaff", Roles = "Admin")]
public class DepartmentsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db)
    {
        var items = await db.Departments
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentItem(d.Id, d.Name, d.IsActive, d.CreatedAtUtc))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromServices] AppDbContext db)
    {
        var item = await db.Departments
            .Where(d => d.Id == id)
            .Select(d => new DepartmentItem(d.Id, d.Name, d.IsActive, d.CreatedAtUtc))
            .SingleOrDefaultAsync();
        if (item is null) return NotFound(new { error = "department_not_found" });
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DepartmentUpsertRequest request, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "name_required" });

        var name = request.Name.Trim();
        if (await db.Departments.AnyAsync(d => d.Name == name)) return Conflict(new { error = "name_in_use" });

        var department = new Department { Name = name };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var item = new DepartmentItem(department.Id, department.Name, department.IsActive, department.CreatedAtUtc);
        return CreatedAtAction(nameof(Get), new { id = department.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DepartmentUpsertRequest request, [FromServices] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "name_required" });

        var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == id);
        if (department is null) return NotFound(new { error = "department_not_found" });

        var name = request.Name.Trim();
        if (await db.Departments.AnyAsync(d => d.Name == name && d.Id != id)) return Conflict(new { error = "name_in_use" });

        department.Name = name;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, [FromServices] AppDbContext db)
    {
        var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == id);
        if (department is null) return NotFound(new { error = "department_not_found" });

        department.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, [FromServices] AppDbContext db)
    {
        var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == id);
        if (department is null) return NotFound(new { error = "department_not_found" });

        department.IsActive = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
