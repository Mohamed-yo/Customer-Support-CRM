using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
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
        var items = await db.Customers
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CustomerListItem(c.Id, c.FullName, c.Email, c.Phone, c.CreatedAtUtc))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromServices] AppDbContext db)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == id);
        if (customer is null) return NotFound(new { error = "customer_not_found" });

        return Ok(new CustomerListItem(customer.Id, customer.FullName, customer.Email, customer.Phone, customer.CreatedAtUtc));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CustomerUpsertRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { error = "name_required" });
        }

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "customer.create",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = null,
            Details = customer.Id.ToString(),
        });

        var item = new CustomerListItem(customer.Id, customer.FullName, customer.Email, customer.Phone, customer.CreatedAtUtc);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CustomerUpsertRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { error = "name_required" });
        }

        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == id);
        if (customer is null) return NotFound(new { error = "customer_not_found" });

        customer.FullName = request.FullName;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "customer.update",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = null,
            Details = customer.Id.ToString(),
        });

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] AppDbContext db,
        [FromServices] AuditLogger audit)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == id);
        if (customer is null) return NotFound(new { error = "customer_not_found" });

        db.Customers.Remove(customer);
        await db.SaveChangesAsync();

        await audit.WriteAsync(new AuditLog
        {
            Action = "customer.delete",
            Outcome = "success",
            ActorUserId = GetActorUserId(),
            TargetUserId = null,
            Details = id.ToString(),
        });

        return NoContent();
    }
}
