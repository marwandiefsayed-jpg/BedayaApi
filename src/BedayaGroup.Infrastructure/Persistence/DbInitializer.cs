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

            // Clean all domain data except Users to allow clean testing
            context.ShareholderPaymentAllocations.RemoveRange(context.ShareholderPaymentAllocations);
            context.ShareholderContributions.RemoveRange(context.ShareholderContributions);
            context.ShareholderInstallmentPenalties.RemoveRange(context.ShareholderInstallmentPenalties);
            context.ProjectInstallments.RemoveRange(context.ProjectInstallments);
            context.Shares.RemoveRange(context.Shares);
            context.Shareholders.RemoveRange(context.Shareholders);
            context.StorageTransactions.RemoveRange(context.StorageTransactions);
            context.Storages.RemoveRange(context.Storages);
            context.CashTransactions.RemoveRange(context.CashTransactions);
            context.CashStorages.RemoveRange(context.CashStorages);
            context.Expenses.RemoveRange(context.Expenses);
            context.Advances.RemoveRange(context.Advances);
            context.ProjectEngineers.RemoveRange(context.ProjectEngineers);
            context.Engineers.RemoveRange(context.Engineers);
            context.Suppliers.RemoveRange(context.Suppliers);
            context.Projects.RemoveRange(context.Projects);
            context.AuditLogs.RemoveRange(context.AuditLogs);
            await context.SaveChangesAsync();

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
