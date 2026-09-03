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

                await context.Database.ExecuteSqlRawAsync(@"
                    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Shareholders')
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Shareholders') AND name = 'ProjectId')
                        BEGIN
                            ALTER TABLE [Shareholders] ADD [ProjectId] int NULL;
                        END
                    END

                    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Suppliers')
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'ProjectId')
                        BEGIN
                            ALTER TABLE [Suppliers] ADD [ProjectId] int NULL;
                        END
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
                    BEGIN
                        CREATE TABLE [__EFMigrationsHistory] (
                            [MigrationId] nvarchar(150) NOT NULL,
                            [ProductVersion] nvarchar(32) NOT NULL,
                            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                        );
                    END

                    IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260901164111_InitialCreate')
                    BEGIN
                        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                        VALUES ('20260901164111_InitialCreate', '9.0.0');
                    END
                ");
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

            // Seed Mock Data for Testing
            if (!await context.Projects.AnyAsync())
            {
                var adminUser = await context.Users.FirstAsync(u => u.Username == "admin");

                // 1. Projects
                var project1 = new Project
                {
                    Code = "PRJ-001",
                    Name = "برج الأمل السكني",
                    Location = "القاهرة الجديدة - التجمع الخامس",
                    Description = "مشروع إنشائي لبرج سكني مكون من 10 طوابق ومواقف سيارات",
                    Budget = 15000000m,
                    StartDate = DateTime.UtcNow.AddMonths(-6),
                    ExpectedEndDate = DateTime.UtcNow.AddMonths(18),
                    Status = ProjectStatus.Active,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var project2 = new Project
                {
                    Code = "PRJ-002",
                    Name = "كمبوند بداية ريزيدنس",
                    Location = "الشيخ زايد - 6 أكتوبر",
                    Description = "مجمع سكني وتجاري متكامل على مساحة 5 فدان",
                    Budget = 35000000m,
                    StartDate = DateTime.UtcNow.AddMonths(-2),
                    ExpectedEndDate = DateTime.UtcNow.AddMonths(24),
                    Status = ProjectStatus.Planning,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Projects.AddRange(project1, project2);
                await context.SaveChangesAsync();

                // 2. Floors
                var floors = new List<Floor>
                {
                    new Floor { ProjectId = project1.Id, FloorNumber = -1, Name = "البدروم / الجراج", Description = "جراج تحت الأرض وحجرة الصيانة", CreatedAt = DateTime.UtcNow },
                    new Floor { ProjectId = project1.Id, FloorNumber = 0, Name = "الدور الأرضي", Description = "محلات تجارية والمدخل الرئيسي", CreatedAt = DateTime.UtcNow },
                    new Floor { ProjectId = project1.Id, FloorNumber = 1, Name = "الدور الأول العلوي", Description = "مكاتب إدارية وشقق عيادات", CreatedAt = DateTime.UtcNow },
                    new Floor { ProjectId = project1.Id, FloorNumber = 2, Name = "الدور الثاني العلوي", Description = "وحدات سكنية", CreatedAt = DateTime.UtcNow },
                    new Floor { ProjectId = project2.Id, FloorNumber = 0, Name = "المبنى الإداري - الأرضي", Description = "المقر الإداري للكمبوند", CreatedAt = DateTime.UtcNow }
                };
                context.Floors.AddRange(floors);

                // 3. Engineers
                var eng1 = new Engineer { Code = "ENG-001", FullName = "مهندس/ أحمد محمود", Phone = "01112345678", Email = "ahmed.m@bedayagroup.com", Specialization = "موقع وإنشاءات", Notes = "مهندس موقع رئيسي لبرج الأمل", IsActive = true, CreatedAt = DateTime.UtcNow };
                var eng2 = new Engineer { Code = "ENG-002", FullName = "مهندسة/ سارة حسن", Phone = "01287654321", Email = "sara.h@bedayagroup.com", Specialization = "تشطيبات وديكور", Notes = "مهندسة تشطيبات متميزة", IsActive = true, CreatedAt = DateTime.UtcNow };
                context.Engineers.AddRange(eng1, eng2);
                await context.SaveChangesAsync();

                // 4. Project Engineers
                context.ProjectEngineers.AddRange(
                    new ProjectEngineer { ProjectId = project1.Id, EngineerId = eng1.Id, Role = "مهندس موقع رئيسي", StartDate = DateTime.UtcNow.AddMonths(-6), CreatedAt = DateTime.UtcNow },
                    new ProjectEngineer { ProjectId = project1.Id, EngineerId = eng2.Id, Role = "مهندسة جودة وتشطيبات", StartDate = DateTime.UtcNow.AddMonths(-3), CreatedAt = DateTime.UtcNow },
                    new ProjectEngineer { ProjectId = project2.Id, EngineerId = eng1.Id, Role = "استشاري إنشائي", StartDate = DateTime.UtcNow.AddMonths(-1), CreatedAt = DateTime.UtcNow }
                );

                // 5. Suppliers
                var sup1 = new Supplier { Code = "SUP-001", Name = "شركة الأهرام لمواد البناء (حديد وسمنت)", Type = SupplierType.Supplier, Phone = "0223456789", Address = "طريق السويس - القاهرة", OpeningBalance = 250000m, Notes = "مورد حديد التسليح الرئيسي", IsActive = true, CreatedAt = DateTime.UtcNow };
                var sup2 = new Supplier { Code = "SUP-002", Name = "الخلاطة الجاهزة للخرسانة", Type = SupplierType.Contractor, Phone = "0234567890", Address = "المنطقة الصناعية - أبو رواش", OpeningBalance = 100000m, Notes = "توريد خرسانةجاهزة", IsActive = true, CreatedAt = DateTime.UtcNow };
                var sup3 = new Supplier { Code = "SUP-003", Name = "مؤسسة النور للكهرباء والكابلات", Type = SupplierType.ServiceProvider, Phone = "01099887766", Address = "العتبة - القاهرة", OpeningBalance = 0m, Notes = "توريد أدوات وكابلات كهربائية", IsActive = true, CreatedAt = DateTime.UtcNow };
                context.Suppliers.AddRange(sup1, sup2, sup3);

                // 6. Cash Storages (الخزن والبنك)
                var cashStorageMain = new CashStorage { Name = "الخزينة الرئيسية - المقر", Type = CashStorageType.Company, OpeningBalance = 1000000m, Location = "المقر الرئيسي", IsActive = true, CreatedAt = DateTime.UtcNow };
                var cashStorageProject1 = new CashStorage { Name = "خزينة موقع برج الأمل", Type = CashStorageType.Project, OpeningBalance = 50000m, Location = "موقع مشروع برج الأمل", ProjectId = project1.Id, IsActive = true, CreatedAt = DateTime.UtcNow };
                var bankAccount = new CashStorage { Name = "حساب بنك مصر - الشركة", Type = CashStorageType.Company, OpeningBalance = 5000000m, Location = "فرع التجمع الخامس", IsActive = true, CreatedAt = DateTime.UtcNow };
                context.CashStorages.AddRange(cashStorageMain, cashStorageProject1, bankAccount);

                // 7. Physical Storages (مخازن المواد)
                var storageMain = new Storage { Name = "المخزن المركزي", Type = StorageType.Company, Location = "العاشر من رمضان", Description = "المخزن المركزي لجميع المواد والمعدات", IsActive = true, CreatedAt = DateTime.UtcNow };
                var storageProject1 = new Storage { Name = "مخزن موقع برج الأمل", Type = StorageType.Project, ProjectId = project1.Id, Location = "موقع برج الأمل", Description = "مخزن مؤقت للمواد والمعدات بموقع المشروع", IsActive = true, CreatedAt = DateTime.UtcNow };
                context.Storages.AddRange(storageMain, storageProject1);

                // 8. Shareholders (المساهمون/الشركاء)
                var sh1 = new Shareholder { Code = "SH-001", Name = "الحاج/ محمد عبد الفتاح", Phone = "01011112222", OwnershipPercentage = 40.00m, RequiredContribution = 20000000m, ProjectId = project1.Id, Notes = "شريك مؤسس بنسبة 40%", IsActive = true, CreatedAt = DateTime.UtcNow };
                var sh2 = new Shareholder { Code = "SH-002", Name = "الدكتور/ خالد عبد العزيز", Phone = "01033334444", OwnershipPercentage = 35.00m, RequiredContribution = 17500000m, ProjectId = project1.Id, Notes = "شريك بنسبة 35%", IsActive = true, CreatedAt = DateTime.UtcNow };
                var sh3 = new Shareholder { Code = "SH-003", Name = "المهندس/ طارق زياد", Phone = "01055556666", OwnershipPercentage = 25.00m, RequiredContribution = 12500000m, ProjectId = project2.Id, Notes = "شريك بنسبة 25%", IsActive = true, CreatedAt = DateTime.UtcNow };
                context.Shareholders.AddRange(sh1, sh2, sh3);

                await context.SaveChangesAsync();

                // 9. Expenses (المصروفات)
                var exp1 = new Expense
                {
                    ExpenseNumber = "EXP-2026-0001",
                    ProjectId = project1.Id,
                    FloorId = floors[0].Id, // البدروم
                    SupplierId = sup1.Id,
                    ExpenseDate = DateTime.UtcNow.AddMonths(-3),
                    Description = "شراء 20 طن حديد تسليح 12 مم",
                    TotalAmount = 800000m,
                    CreatedByUserId = adminUser.Id,
                    Status = ExpenseStatus.Paid,
                    Notes = "تم التوريد والسداد بالكامل",
                    CreatedAt = DateTime.UtcNow
                };

                var exp2 = new Expense
                {
                    ExpenseNumber = "EXP-2026-0002",
                    ProjectId = project1.Id,
                    FloorId = floors[1].Id, // الدور الأرضي
                    SupplierId = sup2.Id,
                    ExpenseDate = DateTime.UtcNow.AddMonths(-1),
                    Description = "صب خرسانة جاهزة لسقف الدور الأرضي",
                    TotalAmount = 350000m,
                    CreatedByUserId = adminUser.Id,
                    Status = ExpenseStatus.PartiallyPaid,
                    Notes = "تم دفع جزء ومتبقي مستحق",
                    CreatedAt = DateTime.UtcNow
                };

                context.Expenses.AddRange(exp1, exp2);
                await context.SaveChangesAsync();

                // 10. Advances (العهد الهندسية)
                var adv1 = new Advance
                {
                    AdvanceNumber = "ADV-2026-0001",
                    EngineerId = eng1.Id,
                    ProjectId = project1.Id,
                    IssuedAmount = 50000m,
                    IssueDate = DateTime.UtcNow.AddMonths(-2),
                    Status = AdvanceStatus.Open,
                    Notes = "عهدة مؤقتة لشراء مصاريف نثريات وحراسة الموقع",
                    CreatedByUserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Advances.Add(adv1);
                await context.SaveChangesAsync();

                // 11. Cash Transactions (المعاملات المالية والنقدية)
                var cashTx1 = new CashTransaction
                {
                    TransactionNumber = "CTX-2026-0001",
                    TransactionDate = DateTime.UtcNow.AddMonths(-3),
                    Type = CashTransactionType.ExpensePayment,
                    Amount = 800000m,
                    CashStorageId = bankAccount.Id,
                    ProjectId = project1.Id,
                    ExpenseId = exp1.Id,
                    Description = "سداد فاتورة توريد حديد التسليح - شركة الأهرام",
                    ReferenceNumber = "TRF-998877",
                    CreatedByUserId = adminUser.Id,
                    Notes = "تحويل بنكي",
                    CreatedAt = DateTime.UtcNow
                };

                var cashTx2 = new CashTransaction
                {
                    TransactionNumber = "CTX-2026-0002",
                    TransactionDate = DateTime.UtcNow.AddMonths(-2),
                    Type = CashTransactionType.AdvanceGiven,
                    Amount = 50000m,
                    CashStorageId = cashStorageMain.Id,
                    ProjectId = project1.Id,
                    AdvanceId = adv1.Id,
                    Description = "صرف عهدة للمهندس أحمد محمود لموقع برج الأمل",
                    ReferenceNumber = "REC-00123",
                    CreatedByUserId = adminUser.Id,
                    Notes = "صرف نقدي من الخزينة الرئيسية",
                    CreatedAt = DateTime.UtcNow
                };

                context.CashTransactions.AddRange(cashTx1, cashTx2);

                // 12. Shareholder Contributions (مساهمات المساهمين)
                context.ShareholderContributions.AddRange(
                    new ShareholderContribution
                    {
                        ShareholderId = sh1.Id,
                        ProjectId = project1.Id,
                        Amount = 5000000m,
                        ContributionDate = DateTime.UtcNow.AddMonths(-5),
                        Description = "دفعة أولى من رأس مال مشروع برج الأمل",
                        CreatedByUserId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ShareholderContribution
                    {
                        ShareholderId = sh2.Id,
                        ProjectId = project1.Id,
                        Amount = 4000000m,
                        ContributionDate = DateTime.UtcNow.AddMonths(-5),
                        Description = "دفعة أولى من المساهمة لمشروع برج الأمل",
                        CreatedByUserId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow
                    }
                );

                // 13. Storage Transactions (حركات المخزن)
                context.StorageTransactions.AddRange(
                    new StorageTransaction
                    {
                        StorageId = storageProject1.Id,
                        ProjectId = project1.Id,
                        TransactionDate = DateTime.UtcNow.AddMonths(-3),
                        MaterialName = "حديد تسليح 12 مم",
                        Unit = "طن",
                        Quantity = 20m,
                        Type = StorageTransactionType.Purchase,
                        ReferenceNumber = "IN-881",
                        Description = "استلام شحنة حديد من شركة الأهرام",
                        CreatedByUserId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    new StorageTransaction
                    {
                        StorageId = storageProject1.Id,
                        ProjectId = project1.Id,
                        TransactionDate = DateTime.UtcNow.AddMonths(-2),
                        MaterialName = "حديد تسليح 12 مم",
                        Unit = "طن",
                        Quantity = 5m,
                        Type = StorageTransactionType.IssueToProject,
                        ReferenceNumber = "OUT-101",
                        Description = "صرف كمية حديد للأعمال الإنشائية بالدور الأرضي",
                        CreatedByUserId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow
                    }
                );

                await context.SaveChangesAsync();
                logger.LogInformation("Comprehensive test mock data seeded successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}
