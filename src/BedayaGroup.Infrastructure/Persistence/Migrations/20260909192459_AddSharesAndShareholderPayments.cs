using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedayaGroup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSharesAndShareholderPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OwnershipPercentage",
                table: "Shareholders");

            migrationBuilder.DropColumn(
                name: "RequiredContribution",
                table: "Shareholders");

            migrationBuilder.AddColumn<decimal>(
                name: "NumberOfShares",
                table: "Shareholders",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ShareId",
                table: "Shareholders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProjectInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AmountPerShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectInstallments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShareholderPaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShareholderContributionId = table.Column<int>(type: "int", nullable: false),
                    ProjectInstallmentId = table.Column<int>(type: "int", nullable: false),
                    AmountAllocated = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareholderPaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareholderPaymentAllocations_ProjectInstallments_ProjectInstallmentId",
                        column: x => x.ProjectInstallmentId,
                        principalTable: "ProjectInstallments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareholderPaymentAllocations_ShareholderContributions_ShareholderContributionId",
                        column: x => x.ShareholderContributionId,
                        principalTable: "ShareholderContributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shareholders_ShareId",
                table: "Shareholders",
                column: "ShareId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInstallments_ProjectId",
                table: "ProjectInstallments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderPaymentAllocations_ProjectInstallmentId",
                table: "ShareholderPaymentAllocations",
                column: "ProjectInstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderPaymentAllocations_ShareholderContributionId",
                table: "ShareholderPaymentAllocations",
                column: "ShareholderContributionId");

            // Seed a default Share so existing Shareholders can satisfy the FK constraint
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [Shares])
                BEGIN
                    SET IDENTITY_INSERT [Shares] ON;
                    INSERT INTO [Shares] ([Id], [Name], [StartDate], [IsActive], [CreatedAt])
                    VALUES (1, N'السهم الافتراضي', GETUTCDATE(), 1, GETUTCDATE());
                    SET IDENTITY_INSERT [Shares] OFF;
                END
            ");

            // Point all existing Shareholders to the default Share
            migrationBuilder.Sql(@"
                UPDATE [Shareholders] SET [ShareId] = 1 WHERE [ShareId] = 0;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Shareholders_Shares_ShareId",
                table: "Shareholders",
                column: "ShareId",
                principalTable: "Shares",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shareholders_Shares_ShareId",
                table: "Shareholders");

            migrationBuilder.DropTable(
                name: "ShareholderPaymentAllocations");

            migrationBuilder.DropTable(
                name: "Shares");

            migrationBuilder.DropTable(
                name: "ProjectInstallments");

            migrationBuilder.DropIndex(
                name: "IX_Shareholders_ShareId",
                table: "Shareholders");

            migrationBuilder.DropColumn(
                name: "NumberOfShares",
                table: "Shareholders");

            migrationBuilder.DropColumn(
                name: "ShareId",
                table: "Shareholders");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OwnershipPercentage",
                table: "Shareholders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RequiredContribution",
                table: "Shareholders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArabicName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleType = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
