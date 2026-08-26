using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Data;

/// <summary>
/// Development-only convenience seed. Never runs outside the Development environment
/// (see the call site in Program.cs) — non-Development environments must provision the
/// first user out-of-band.
/// </summary>
public static class SeedData
{
    private static readonly string[] SeedRoleNames = { "Admin", "Agent" };

    public static async Task EnsureSeedRolesAsync(AppDbContext db)
    {
        foreach (var name in SeedRoleNames)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == name))
            {
                db.Roles.Add(new Role { Id = Guid.NewGuid(), Name = name });
            }
        }

        await db.SaveChangesAsync();
    }

    public static async Task EnsureSeedUserAsync(AppDbContext db, PasswordHasher<User> passwordHasher)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .SingleOrDefaultAsync(u => u.Email == "admin@example.com");

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@example.com",
                DisplayName = "Admin",
                CreatedAtUtc = DateTime.UtcNow,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, "Passw0rd!");

            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var adminRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole is not null && !user.UserRoles.Any(ur => ur.RoleId == adminRole.Id))
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
            await db.SaveChangesAsync();
        }
    }
}
