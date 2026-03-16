using Microsoft.EntityFrameworkCore;
using VideoGamesStore.Models;

namespace VideoGamesStore.Services;

public static class DbInitializer
{
    public static async Task SeedAsync(VideoGamesStoreContext context, IPasswordHasher hasher)
    {
        await context.Database.EnsureCreatedAsync();

        await context.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('Reviews', 'IsVisible') IS NULL
BEGIN
    ALTER TABLE Reviews ADD IsVisible bit NOT NULL CONSTRAINT DF_Reviews_IsVisible DEFAULT(1);
END");

        var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (userRole is null)
        {
            userRole = new Role { Name = "User" };
            context.Roles.Add(userRole);
        }

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole is null)
        {
            adminRole = new Role { Name = "Admin" };
            context.Roles.Add(adminRole);
        }

        await context.SaveChangesAsync();

        var hasAdmin = await context.Users.AnyAsync(u => u.RoleId == adminRole.Id);
        if (!hasAdmin)
        {
            context.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@videogamestore.local",
                PasswordHash = hasher.HashPassword("Admin123!"),
                RoleId = adminRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }
}
