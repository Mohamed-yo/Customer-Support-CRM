using System.ComponentModel.DataAnnotations;

namespace CustomerSupportCrm.Api.Controllers;

public record CustomerListItem(Guid Id, string FullName, string Email, string? Phone, DateTime CreatedAtUtc);

public class CustomerUpsertRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Phone { get; set; }
}
