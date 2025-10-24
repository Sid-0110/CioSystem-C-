using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CioSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSKUUniqueConstraintForSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 刪除原有的SKU唯一索引
            migrationBuilder.DropIndex(
                name: "IX_Products_SKU",
                table: "Products");

            // 創建新的條件唯一索引，只對未刪除的記錄生效
            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU_Active",
                table: "Products",
                column: "SKU",
                unique: true,
                filter: "IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 刪除條件唯一索引
            migrationBuilder.DropIndex(
                name: "IX_Products_SKU_Active",
                table: "Products");

            // 恢復原有的SKU唯一索引
            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true);
        }
    }
}
