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
    public static async Task EnsureSeedUserAsync(AppDbContext db, PasswordHasher<User> passwordHasher)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var user = new User
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
}
