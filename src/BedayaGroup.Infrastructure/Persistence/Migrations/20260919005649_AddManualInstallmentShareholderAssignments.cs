using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedayaGroup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualInstallmentShareholderAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectInstallmentShareholders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectInstallmentId = table.Column<int>(type: "int", nullable: false),
                    ShareholderId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInstallmentShareholders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectInstallmentShareholders_ProjectInstallments_ProjectInstallmentId",
                        column: x => x.ProjectInstallmentId,
                        principalTable: "ProjectInstallments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectInstallmentShareholders_Shareholders_ShareholderId",
                        column: x => x.ShareholderId,
                        principalTable: "Shareholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInstallmentShareholders_ProjectInstallmentId",
                table: "ProjectInstallmentShareholders",
                column: "ProjectInstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInstallmentShareholders_ShareholderId",
                table: "ProjectInstallmentShareholders",
                column: "ShareholderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectInstallmentShareholders");
        }
    }
}
