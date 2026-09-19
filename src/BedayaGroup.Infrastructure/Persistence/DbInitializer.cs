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

            // Seed default admin user (CompanyOwner)
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            if (adminUser == null)
            {
                context.Users.Add(new User
                {
                    FullName = "مالك الشركة (أدمن)",
                    Username = "admin",
                    PasswordHash = passwordHasher.HashPassword("Admin@123456"),
                    Phone = "01000000001",
                    Role = Domain.Enums.UserRole.CompanyOwner,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (adminUser.Role == 0)
            {
                adminUser.Role = Domain.Enums.UserRole.CompanyOwner;
            }

            // Ensure no user has Role == 0
            var unassignedUsers = await context.Users.Where(u => (int)u.Role == 0).ToListAsync();
            foreach (var u in unassignedUsers)
            {
                u.Role = Domain.Enums.UserRole.CompanyOwner;
            }

            // Seed default shareholder officer user
            if (!await context.Users.AnyAsync(u => u.Username == "shareholder_officer"))
            {
                context.Users.Add(new User
                {
                    FullName = "مسؤول المساهمين",
                    Username = "shareholder_officer",
                    PasswordHash = passwordHasher.HashPassword("Share@123456"),
                    Phone = "01000000002",
                    Role = Domain.Enums.UserRole.ShareholdersOfficer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Seed default expenses officer user
            if (!await context.Users.AnyAsync(u => u.Username == "expenses_officer"))
            {
                context.Users.Add(new User
                {
                    FullName = "مسؤول المصروفات والموردين",
                    Username = "expenses_officer",
                    PasswordHash = passwordHasher.HashPassword("Expense@123456"),
                    Phone = "01000000003",
                    Role = Domain.Enums.UserRole.ExpensesOfficer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();

            // Seed exact 2 cash storages: خزينة المكتب & خزينة بنكية
            if (!await context.CashStorages.AnyAsync())
            {
                context.CashStorages.AddRange(
                    new CashStorage
                    {
                        Name = "خزينة المكتب",
                        Type = BedayaGroup.Domain.Enums.CashStorageType.Company,
                        OpeningBalance = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new CashStorage
                    {
                        Name = "خزينة بنكية",
                        Type = BedayaGroup.Domain.Enums.CashStorageType.Calculator,
                        OpeningBalance = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                );
                await context.SaveChangesAsync();
            }

            logger.LogInformation("Static accounts and cash storages verified and seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}
