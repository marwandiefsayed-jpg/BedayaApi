using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedayaGroup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSuppliersAdvancesEngineers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashTransactions_Advances_AdvanceId",
                table: "CashTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Suppliers_SupplierId",
                table: "Expenses");

            migrationBuilder.DropTable(
                name: "Advances");

            migrationBuilder.DropTable(
                name: "ProjectEngineers");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Engineers");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_SupplierId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_CashTransactions_AdvanceId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "AdvanceId",
                table: "CashTransactions");

            migrationBuilder.AddColumn<int>(
                name: "StorageId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_StorageId",
                table: "Expenses",
                column: "StorageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Storages_StorageId",
                table: "Expenses",
                column: "StorageId",
                principalTable: "Storages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Storages_StorageId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_StorageId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "StorageId",
                table: "Expenses");

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "Expenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AdvanceId",
                table: "CashTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Engineers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Specialization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Engineers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DefaultMaterialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Advances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    EngineerId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    AdvanceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssuedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SettlementDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Advances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Advances_Engineers_EngineerId",
                        column: x => x.EngineerId,
                        principalTable: "Engineers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Advances_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Advances_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectEngineers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EngineerId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEngineers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectEngineers_Engineers_EngineerId",
                        column: x => x.EngineerId,
                        principalTable: "Engineers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectEngineers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SupplierId",
                table: "Expenses",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_AdvanceId",
                table: "CashTransactions",
                column: "AdvanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Advances_AdvanceNumber",
                table: "Advances",
                column: "AdvanceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Advances_CreatedByUserId",
                table: "Advances",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Advances_EngineerId",
                table: "Advances",
                column: "EngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_Advances_ProjectId",
                table: "Advances",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Advances_Status",
                table: "Advances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Engineers_Code",
                table: "Engineers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEngineers_EngineerId",
                table: "ProjectEngineers",
                column: "EngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEngineers_ProjectId",
                table: "ProjectEngineers",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_ProjectId",
                table: "Suppliers",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransactions_Advances_AdvanceId",
                table: "CashTransactions",
                column: "AdvanceId",
                principalTable: "Advances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Suppliers_SupplierId",
                table: "Expenses",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
