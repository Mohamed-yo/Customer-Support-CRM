namespace CustomerSupportCrm.Api.Controllers;

public sealed record BranchItem(Guid Id, string Name, bool IsActive, DateTime CreatedAtUtc);

public sealed class BranchUpsertRequest
{
    public string Name { get; set; } = string.Empty;
}
