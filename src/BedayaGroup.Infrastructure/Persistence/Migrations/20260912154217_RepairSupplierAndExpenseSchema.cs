using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedayaGroup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairSupplierAndExpenseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Previous migrations updated the EF model snapshot but did not add this
            // column to deployed databases. Supplier reads join the project, so its
            // absence causes every suppliers/expenses request to fail at SQL level.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[Suppliers]', N'ProjectId') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Suppliers] ADD [ProjectId] int NULL;
                    CREATE INDEX [IX_Suppliers_ProjectId] ON [dbo].[Suppliers] ([ProjectId]);
                    ALTER TABLE [dbo].[Suppliers] ADD CONSTRAINT [FK_Suppliers_Projects_ProjectId]
                        FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects] ([Id]) ON DELETE SET NULL;
                END
                """);

            migrationBuilder.AddColumn<string>(
                name: "MaterialName",
                table: "Expenses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "Expenses",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Expenses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "Expenses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaterialName",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "Expenses");

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[Suppliers]', N'ProjectId') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Suppliers_Projects_ProjectId')
                        ALTER TABLE [dbo].[Suppliers] DROP CONSTRAINT [FK_Suppliers_Projects_ProjectId];
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Suppliers_ProjectId' AND object_id = OBJECT_ID(N'[dbo].[Suppliers]'))
                        DROP INDEX [IX_Suppliers_ProjectId] ON [dbo].[Suppliers];
                    ALTER TABLE [dbo].[Suppliers] DROP COLUMN [ProjectId];
                END
                """);
        }
    }
}
