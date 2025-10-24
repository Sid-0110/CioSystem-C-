using System;

namespace CioSystem.Models
{
    /// <summary>
    /// 產品統計資訊
    /// </summary>
    public class ProductStatistics
    {
        /// <summary>
        /// 總產品數量
        /// </summary>
        public int TotalProducts { get; set; }

        /// <summary>
        /// 啟用的產品數量
        /// </summary>
        public int ActiveProducts { get; set; }

        /// <summary>
        /// 停用的產品數量
        /// </summary>
        public int InactiveProducts { get; set; }

        /// <summary>
        /// 低庫存產品數量
        /// </summary>
        public int LowStockProducts { get; set; }

        /// <summary>
        /// 總產品分類數量
        /// </summary>
        public int TotalCategories { get; set; }

        /// <summary>
        /// 平均價格
        /// </summary>
        public decimal AveragePrice { get; set; }

        /// <summary>
        /// 最高價格
        /// </summary>
        public decimal MaxPrice { get; set; }

        /// <summary>
        /// 最低價格
        /// </summary>
        public decimal MinPrice { get; set; }

        /// <summary>
        /// 總價值
        /// </summary>
        public decimal TotalValue { get; set; }
    }
}