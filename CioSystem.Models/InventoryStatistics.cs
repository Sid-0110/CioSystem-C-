using System;

namespace CioSystem.Models
{
    /// <summary>
    /// 庫存統計資訊
    /// </summary>
    public class InventoryStatistics
    {
        /// <summary>
        /// 總庫存項目數量
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// 可用項目數量
        /// </summary>
        public int AvailableItems { get; set; }

        /// <summary>
        /// 不可用項目數量
        /// </summary>
        public int UnavailableItems { get; set; }

        /// <summary>
        /// 低庫存項目數量
        /// </summary>
        public int LowStockItems { get; set; }

        /// <summary>
        /// 總庫存數量
        /// </summary>
        public int TotalQuantity { get; set; }

        /// <summary>
        /// 總庫存價值
        /// </summary>
        public decimal TotalValue { get; set; }

        /// <summary>
        /// 平均庫存數量
        /// </summary>
        public decimal AverageQuantity { get; set; }

        /// <summary>
        /// 即將到期的項目數量
        /// </summary>
        public int ExpiringSoonItems { get; set; }

        /// <summary>
        /// 已過期的項目數量
        /// </summary>
        public int ExpiredItems { get; set; }
    }
}