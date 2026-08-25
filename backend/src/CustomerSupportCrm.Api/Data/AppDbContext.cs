using CustomerSupportCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
            e.HasIndex(u => u.Email).IsUnique();
        });
    }
}
