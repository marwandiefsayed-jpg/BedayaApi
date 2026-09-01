using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace BedayaGroup.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(IApplicationDbContext context, IPasswordHasher passwordHasher, ILogger logger)
    {
        try
        {
            try
            {
                var databaseCreator = context.Database.GetService<IRelationalDatabaseCreator>();
                if (databaseCreator != null)
                {
                    if (!await databaseCreator.ExistsAsync())
                    {
                        await databaseCreator.CreateAsync();
                    }
                    if (!await databaseCreator.HasTablesAsync())
                    {
                        await databaseCreator.CreateTablesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database schema check warning: {Message}", ex.Message);
                await context.Database.EnsureCreatedAsync();
            }

            // Seed Roles
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new Role
                    {
                        Name = "CompanyOwner",
                        ArabicName = "مالك الشركة",
                        RoleType = UserRoleType.CompanyOwner,
                        Description = "الصلاحية الكاملة لإدارة كافة العمليات والمشاريع والحسابات"
                    },
                    new Role
                    {
                        Name = "ProjectOwner",
                        ArabicName = "مالك المشروع",
                        RoleType = UserRoleType.ProjectOwner,
                        Description = "إدارة ومتابعة التقارير المالية والإنشائية للمشاريع الخاصة به"
                    },
                    new Role
                    {
                        Name = "Calculator",
                        ArabicName = "مسؤول الحسابات",
                        RoleType = UserRoleType.Calculator,
                        Description = "إدخال وتدقيق المصروفات والعمليات النقدية والعهد"
                    }
                };

                context.Roles.AddRange(roles);
                await context.SaveChangesAsync();
                logger.LogInformation("Roles seeded successfully.");
            }

            // Seed Static Accounts
            var companyOwnerRole = await context.Roles.FirstAsync(r => r.RoleType == UserRoleType.CompanyOwner);
            var projectOwnerRole = await context.Roles.FirstAsync(r => r.RoleType == UserRoleType.ProjectOwner);
            var calculatorRole = await context.Roles.FirstAsync(r => r.RoleType == UserRoleType.Calculator);

            if (!await context.Users.AnyAsync(u => u.Username == "admin"))
            {
                context.Users.Add(new User
                {
                    FullName = "مالك الشركة (أدمن)",
                    Username = "admin",
                    PasswordHash = passwordHasher.HashPassword("Admin@123456"),
                    Phone = "01000000001",
                    RoleId = companyOwnerRole.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await context.Users.AnyAsync(u => u.Username == "projectowner"))
            {
                context.Users.Add(new User
                {
                    FullName = "مالك المشروع التجريبي",
                    Username = "projectowner",
                    PasswordHash = passwordHasher.HashPassword("Owner@123456"),
                    Phone = "01000000002",
                    RoleId = projectOwnerRole.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await context.Users.AnyAsync(u => u.Username == "calculator"))
            {
                context.Users.Add(new User
                {
                    FullName = "المحاسب المسؤول",
                    Username = "calculator",
                    PasswordHash = passwordHasher.HashPassword("Calc@123456"),
                    Phone = "01000000003",
                    RoleId = calculatorRole.Id,
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
