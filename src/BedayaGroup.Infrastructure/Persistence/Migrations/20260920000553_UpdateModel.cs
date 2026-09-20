using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedayaGroup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectInstallmentShareholders_Shareholders_ShareholderId",
                table: "ProjectInstallmentShareholders");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareholderInstallmentPenalties_Shareholders_ShareholderId",
                table: "ShareholderInstallmentPenalties");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareholderPaymentAllocations_ShareholderContributions_ShareholderContributionId",
                table: "ShareholderPaymentAllocations");

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderInstallmentPenalties_ShareholderId_ProjectInstallmentId",
                table: "ShareholderInstallmentPenalties",
                columns: new[] { "ShareholderId", "ProjectInstallmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShareholderContributions_ContributionDate",
                table: "ShareholderContributions",
                column: "ContributionDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInstallmentShareholders_ProjectInstallmentId_ShareholderId",
                table: "ProjectInstallmentShareholders",
                columns: new[] { "ProjectInstallmentId", "ShareholderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInstallments_EndDate",
                table: "ProjectInstallments",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInstallments_IsActive",
                table: "ProjectInstallments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInstallments_StartDate",
                table: "ProjectInstallments",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_CashStorageId_TransactionDate",
                table: "CashTransactions",
                columns: new[] { "CashStorageId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_ProjectId_TransactionDate",
                table: "CashTransactions",
                columns: new[] { "ProjectId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectInstallmentShareholders_Shareholders_ShareholderId",
                table: "ProjectInstallmentShareholders",
                column: "ShareholderId",
                principalTable: "Shareholders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareholderInstallmentPenalties_Shareholders_ShareholderId",
                table: "ShareholderInstallmentPenalties",
                column: "ShareholderId",
                principalTable: "Shareholders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareholderPaymentAllocations_ShareholderContributions_ShareholderContributionId",
                table: "ShareholderPaymentAllocations",
                column: "ShareholderContributionId",
                principalTable: "ShareholderContributions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectInstallmentShareholders_Shareholders_ShareholderId",
                table: "ProjectInstallmentShareholders");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareholderInstallmentPenalties_Shareholders_ShareholderId",
                table: "ShareholderInstallmentPenalties");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareholderPaymentAllocations_ShareholderContributions_ShareholderContributionId",
                table: "ShareholderPaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_ShareholderInstallmentPenalties_ShareholderId_ProjectInstallmentId",
                table: "ShareholderInstallmentPenalties");

            migrationBuilder.DropIndex(
                name: "IX_ShareholderContributions_ContributionDate",
                table: "ShareholderContributions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectInstallmentShareholders_ProjectInstallmentId_ShareholderId",
                table: "ProjectInstallmentShareholders");

            migrationBuilder.DropIndex(
                name: "IX_ProjectInstallments_EndDate",
                table: "ProjectInstallments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectInstallments_IsActive",
                table: "ProjectInstallments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectInstallments_StartDate",
                table: "ProjectInstallments");

            migrationBuilder.DropIndex(
                name: "IX_CashTransactions_CashStorageId_TransactionDate",
                table: "CashTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CashTransactions_ProjectId_TransactionDate",
                table: "CashTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectInstallmentShareholders_Shareholders_ShareholderId",
                table: "ProjectInstallmentShareholders",
                column: "ShareholderId",
                principalTable: "Shareholders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareholderInstallmentPenalties_Shareholders_ShareholderId",
                table: "ShareholderInstallmentPenalties",
                column: "ShareholderId",
                principalTable: "Shareholders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareholderPaymentAllocations_ShareholderContributions_ShareholderContributionId",
                table: "ShareholderPaymentAllocations",
                column: "ShareholderContributionId",
                principalTable: "ShareholderContributions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
