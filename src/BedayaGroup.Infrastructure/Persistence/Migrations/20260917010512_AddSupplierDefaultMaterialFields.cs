using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedayaGroup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierDefaultMaterialFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultMaterialName",
                table: "Suppliers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultUnit",
                table: "Suppliers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultMaterialName",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DefaultUnit",
                table: "Suppliers");
        }
    }
}
