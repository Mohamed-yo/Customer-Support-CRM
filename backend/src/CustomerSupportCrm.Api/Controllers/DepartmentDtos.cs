namespace CustomerSupportCrm.Api.Controllers;

public sealed record DepartmentItem(Guid Id, string Name, bool IsActive, DateTime CreatedAtUtc);

public sealed class DepartmentUpsertRequest
{
    public string Name { get; set; } = string.Empty;
}
