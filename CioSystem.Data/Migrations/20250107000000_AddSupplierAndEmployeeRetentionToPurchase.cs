using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CioSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierAndEmployeeRetentionToPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "Purchases",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeRetention",
                table: "Purchases",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "EmployeeRetention",
                table: "Purchases");
        }
    }
}

