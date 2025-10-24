using System;
using System.ComponentModel.DataAnnotations;
using CioSystem.Core;

namespace CioSystem.Models
{
    /// <summary>
    /// 產品實體模型
    /// 這是學習資料模型設計的核心類別
    /// </summary>
    public class Product : BaseEntity
    {
        /// <summary>
        /// 產品名稱
        /// </summary>
        [Required(ErrorMessage = "產品名稱不能為空")]
        [StringLength(200, ErrorMessage = "產品名稱長度不能超過200個字元")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 產品描述
        /// </summary>
        [StringLength(1000, ErrorMessage = "產品描述長度不能超過1000個字元")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 產品成本價
        /// </summary>
        [Required(ErrorMessage = "產品成本價不能為空")]
        [Range(0.01, double.MaxValue, ErrorMessage = "產品成本價必須大於0")]
        public decimal CostPrice { get; set; }

        /// <summary>
        /// 產品售價
        /// </summary>
        [Required(ErrorMessage = "產品售價不能為空")]
        [Range(0.01, double.MaxValue, ErrorMessage = "產品售價必須大於0")]
        public decimal Price { get; set; }

        /// <summary>
        /// 產品類別
        /// </summary>
        [Required(ErrorMessage = "產品類別不能為空")]
        [StringLength(100, ErrorMessage = "產品類別長度不能超過100個字元")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 產品編號（SKU）
        /// </summary>
        [Required(ErrorMessage = "產品編號不能為空")]
        [StringLength(50, ErrorMessage = "產品編號長度不能超過50個字元")]
        public string SKU { get; set; } = string.Empty;

        /// <summary>
        /// 產品品牌
        /// </summary>
        [StringLength(100, ErrorMessage = "產品品牌長度不能超過100個字元")]
        public string Brand { get; set; } = string.Empty;

        /// <summary>
        /// 產品重量（克）
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// 產品尺寸（長x寬x高，公分）
        /// </summary>
        public string? Dimensions { get; set; }

        /// <summary>
        /// 產品顏色
        /// </summary>
        [StringLength(50, ErrorMessage = "產品顏色長度不能超過50個字元")]
        public string? Color { get; set; }

        /// <summary>
        /// 產品狀態
        /// </summary>
        public ProductStatus Status { get; set; } = ProductStatus.Active;

        /// <summary>
        /// 產品圖片URL
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// 最低庫存警戒值
        /// </summary>
        public int MinStockLevel { get; set; } = 10;

        /// <summary>
        /// 最高庫存限制
        /// </summary>
        public int? MaxStockLevel { get; set; }

        /// <summary>
        /// 供應商ID
        /// </summary>
        public int? SupplierId { get; set; }

        /// <summary>
        /// 產品標籤（用逗號分隔）
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// 產品備註
        /// </summary>
        [StringLength(500, ErrorMessage = "產品備註長度不能超過500個字元")]
        public string? Notes { get; set; }

        /// <summary>
        /// 導航屬性：庫存記錄
        /// </summary>
        public virtual ICollection<Inventory> InventoryItems { get; set; } = new List<Inventory>();

        /// <summary>
        /// 導航屬性：採購記錄
        /// </summary>
        public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

        /// <summary>
        /// 導航屬性：銷售記錄
        /// </summary>
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }

    /// <summary>
    /// 產品狀態枚舉
    /// </summary>
    public enum ProductStatus
    {
        /// <summary>
        /// 活躍（正常銷售）
        /// </summary>
        Active = 1,

        /// <summary>
        /// 停用（暫停銷售）
        /// </summary>
        Inactive = 2,

        /// <summary>
        /// 缺貨
        /// </summary>
        OutOfStock = 3,

        /// <summary>
        /// 停產
        /// </summary>
        Discontinued = 4
    }

    /// <summary>
    /// 暫時定義相關實體（實際會在各自的檔案中定義）
    /// </summary>
    public class Purchase : BaseEntity
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public string? EmployeeRetention { get; set; }

        /// <summary>
        /// 導航屬性：產品
        /// </summary>
        public virtual Product? Product { get; set; }
    }

    public class Sale : BaseEntity
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? EmployeeRetention { get; set; }

        /// <summary>
        /// 導航屬性：產品
        /// </summary>
        public virtual Product? Product { get; set; }
    }
}