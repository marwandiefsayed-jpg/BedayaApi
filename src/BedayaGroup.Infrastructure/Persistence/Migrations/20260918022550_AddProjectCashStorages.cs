using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedayaGroup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCashStorages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill a dedicated project cash storage (Type = 2) for every existing project.
            migrationBuilder.Sql(@"
INSERT INTO [CashStorages] ([Name], [Type], [OpeningBalance], [IsActive], [CreatedAt], [ProjectId])
SELECT CONCAT(N'خزينة مشروع: ', p.[Name]), 2, 0, 1, SYSUTCDATETIME(), p.[Id]
FROM [Projects] p
WHERE NOT EXISTS (
    SELECT 1 FROM [CashStorages] cs
    WHERE cs.[Type] = 2 AND cs.[ProjectId] = p.[Id]
);");

            // Backfill a dedicated project material warehouse (Type = 2) for every existing project.
            migrationBuilder.Sql(@"
INSERT INTO [Storages] ([Name], [Type], [ProjectId], [IsActive], [CreatedAt])
SELECT CONCAT(N'مخزن مشروع: ', p.[Name]), 2, p.[Id], 1, SYSUTCDATETIME()
FROM [Projects] p
WHERE NOT EXISTS (
    SELECT 1 FROM [Storages] s
    WHERE s.[Type] = 2 AND s.[ProjectId] = p.[Id]
);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove backfilled project material warehouses that never received movements.
            migrationBuilder.Sql(@"
DELETE s FROM [Storages] s
WHERE s.[Type] = 2
  AND NOT EXISTS (SELECT 1 FROM [StorageTransactions] st WHERE st.[StorageId] = s.[Id]);");

            // Remove backfilled project storages that never received transactions.
            migrationBuilder.Sql(@"
DELETE cs FROM [CashStorages] cs
WHERE cs.[Type] = 2
  AND NOT EXISTS (SELECT 1 FROM [CashTransactions] ct WHERE ct.[CashStorageId] = cs.[Id]);");
        }
    }
}
