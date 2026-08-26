namespace CustomerSupportCrm.Api.Domain;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "Admin", "Agent"
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
