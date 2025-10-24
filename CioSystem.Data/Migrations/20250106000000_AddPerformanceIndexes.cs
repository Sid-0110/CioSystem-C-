using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CioSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 產品表索引
            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted",
                table: "Products",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Status",
                table: "Products",
                column: "Status");

            // 庫存表索引
            migrationBuilder.CreateIndex(
                name: "IX_Inventory_IsDeleted",
                table: "Inventory",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_ProductId",
                table: "Inventory",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_ProductSKU",
                table: "Inventory",
                column: "ProductSKU");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_Status",
                table: "Inventory",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_Type",
                table: "Inventory",
                column: "Type");

            // 銷售表索引
            migrationBuilder.CreateIndex(
                name: "IX_Sales_IsDeleted",
                table: "Sales",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ProductId",
                table: "Sales",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerName",
                table: "Sales",
                column: "CustomerName");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales",
                column: "SaleDate");

            // 進貨表索引
            migrationBuilder.CreateIndex(
                name: "IX_Purchases_IsDeleted",
                table: "Purchases",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ProductId",
                table: "Purchases",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SupplierName",
                table: "Purchases",
                column: "SupplierName");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_PurchaseDate",
                table: "Purchases",
                column: "PurchaseDate");

            // 複合索引 - 常用查詢組合
            migrationBuilder.CreateIndex(
                name: "IX_Inventory_ProductId_Status",
                table: "Inventory",
                columns: new[] { "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ProductId_SaleDate",
                table: "Sales",
                columns: new[] { "ProductId", "SaleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ProductId_PurchaseDate",
                table: "Purchases",
                columns: new[] { "ProductId", "PurchaseDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 刪除索引
            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SKU",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Category",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Status",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_IsDeleted",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_ProductId",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_ProductSKU",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_Status",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_Type",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Sales_IsDeleted",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ProductId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_CustomerName",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_IsDeleted",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_ProductId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_SupplierName",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_PurchaseDate",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_ProductId_Status",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ProductId_SaleDate",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_ProductId_PurchaseDate",
                table: "Purchases");
        }
    }
}