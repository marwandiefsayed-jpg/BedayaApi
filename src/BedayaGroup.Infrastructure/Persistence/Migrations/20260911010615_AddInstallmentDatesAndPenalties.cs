using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedayaGroup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallmentDatesAndPenalties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "ProjectInstallments",
                newName: "StartDate");

            migrationBuilder.AlterColumn<int>(
                name: "ShareId",
                table: "Shareholders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "ProjectInstallments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ShareholderInstallmentPenalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShareholderId = table.Column<int>(type: "int", nullable: false),
                    ProjectInstallmentId = table.Column<int>(type: "int", nullable: false),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareholderInstallmentPenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareholderInstallmentPenalties_ProjectInstallments_ProjectInstallmentId",
                        column: x => x.ProjectInstallmentId,
                        principalTable: "ProjectInstallments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareholderInstallmentPenalties_Shareholders_ShareholderId",
                        column: x => x.ShareholderId,
                        principalTable: "Shareholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShareholderInstallmentPenalties_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderInstallmentPenalties_CreatedByUserId",
                table: "ShareholderInstallmentPenalties",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderInstallmentPenalties_ProjectInstallmentId",
                table: "ShareholderInstallmentPenalties",
                column: "ProjectInstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderInstallmentPenalties_ShareholderId",
                table: "ShareholderInstallmentPenalties",
                column: "ShareholderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShareholderInstallmentPenalties");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "ProjectInstallments");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "ProjectInstallments",
                newName: "DueDate");

            migrationBuilder.AlterColumn<int>(
                name: "ShareId",
                table: "Shareholders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
