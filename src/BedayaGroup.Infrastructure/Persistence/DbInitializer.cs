using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BedayaGroup.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(IApplicationDbContext context, IPasswordHasher passwordHasher, ILogger logger)
    {
        try
        {
            // Apply all pending EF migrations automatically on startup.
            // This is idempotent — already-applied migrations are skipped.
            // It also creates the database if it does not exist.
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");

            // Seed default admin user
            if (!await context.Users.AnyAsync(u => u.Username == "admin"))
            {
                context.Users.Add(new User
                {
                    FullName = "مالك الشركة (أدمن)",
                    Username = "admin",
                    PasswordHash = passwordHasher.HashPassword("Admin@123456"),
                    Phone = "01000000001",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Seed default calculator user
            if (!await context.Users.AnyAsync(u => u.Username == "calculator"))
            {
                context.Users.Add(new User
                {
                    FullName = "المحاسب المسؤول",
                    Username = "calculator",
                    PasswordHash = passwordHasher.HashPassword("Calc@123456"),
                    Phone = "01000000003",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Static accounts verified and seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}
