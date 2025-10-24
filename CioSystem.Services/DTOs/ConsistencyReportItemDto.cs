using CioSystem.Models;

namespace CioSystem.Services.DTOs
{
    /// <summary>
    /// 庫存一致性對帳報表 DTO（以 進貨總量 - 銷售總量 對比當前庫存）
    /// </summary>
    public class ConsistencyReportItemDto
    {
        public int ProductId { get; set; }
        public string? ProductSKU { get; set; }
        public string? ProductName { get; set; }

        public int PurchasesTotal { get; set; }
        public int SalesTotal { get; set; }

        /// <summary>
        /// 期望庫存（不含手動調整與其他移動）：PurchasesTotal - SalesTotal
        /// </summary>
        public int ExpectedQuantity { get; set; }

        /// <summary>
        /// 當前庫存（Inventory.Quantity）
        /// </summary>
        public int CurrentQuantity { get; set; }

        /// <summary>
        /// 差異（Current - Expected）
        /// </summary>
        public int Difference { get; set; }
    }
}
