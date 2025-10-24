using System;
using System.ComponentModel.DataAnnotations;
using CioSystem.Core;

namespace CioSystem.Models
{
    /// <summary>
    /// 庫存實體模型
    /// 這是學習庫存管理系統的核心類別
    /// </summary>
    public class Inventory : BaseEntity
    {
        /// <summary>
        /// 產品ID（外鍵）
        /// </summary>
        [Required(ErrorMessage = "產品ID不能為空")]
        public int ProductId { get; set; }

        /// <summary>
        /// 庫存數量
        /// </summary>
        [Required(ErrorMessage = "庫存數量不能為空")]
        [Range(0, int.MaxValue, ErrorMessage = "庫存數量不能為負數")]
        public int Quantity { get; set; }

        /// <summary>
        /// 產品編號（從產品表自動帶入）
        /// </summary>
        [StringLength(50, ErrorMessage = "產品編號長度不能超過50個字元")]
        public string? ProductSKU { get; set; }

        /// <summary>
        /// 庫存類型
        /// </summary>
        public InventoryType Type { get; set; } = InventoryType.Stock;

        /// <summary>
        /// 庫存狀態
        /// </summary>
        public InventoryStatus Status { get; set; } = InventoryStatus.Normal;

        /// <summary>
        /// 預留數量
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "預留數量不能為負數")]
        public int ReservedQuantity { get; set; } = 0;

        /// <summary>
        /// 生產日期
        /// </summary>
        public DateTime? ProductionDate { get; set; }

        /// <summary>
        /// 安全庫存
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "安全庫存不能為負數")]
        public int SafetyStock { get; set; } = 0;


        /// <summary>
        /// 庫存警告數量
        /// </summary>
        public int? WarningLevel { get; set; }

        /// <summary>
        /// 庫存備註
        /// </summary>
        [StringLength(500, ErrorMessage = "庫存備註長度不能超過500個字元")]
        public string? Notes { get; set; }

        /// <summary>
        /// 員工自留
        /// </summary>
        [StringLength(50, ErrorMessage = "員工自留長度不能超過50個字元")]
        public string? EmployeeRetention { get; set; }

        /// <summary>
        /// 最後盤點日期
        /// </summary>
        public DateTime? LastCountDate { get; set; }

        /// <summary>
        /// 導航屬性：產品
        /// </summary>
        public virtual Product Product { get; set; } = null!;

        /// <summary>
        /// 導航屬性：庫存移動記錄
        /// </summary>
        public virtual ICollection<InventoryMovement> Movements { get; set; } = new List<InventoryMovement>();

        /// <summary>
        /// 檢查庫存是否足夠
        /// </summary>
        /// <param name="requiredQuantity">需要的數量</param>
        /// <returns>是否足夠</returns>
        public bool HasEnoughStock(int requiredQuantity)
        {
            return Status != InventoryStatus.OutOfStock && Quantity >= requiredQuantity;
        }

        /// <summary>
        /// 檢查是否接近警告水平
        /// </summary>
        /// <returns>是否接近警告水平</returns>
        public bool IsNearWarningLevel()
        {
            return Quantity < SafetyStock;
        }

        /// <summary>
        /// 計算庫存狀態
        /// </summary>
        /// <returns>庫存狀態</returns>
        public InventoryStatus CalculateStatus()
        {
            if (Quantity == 0)
                return InventoryStatus.OutOfStock;
            else if (Quantity < SafetyStock)
                return InventoryStatus.LowStock;
            else if (Quantity > SafetyStock * 2)
                return InventoryStatus.Excess;
            else
                return InventoryStatus.Normal;
        }
    }

    /// <summary>
    /// 庫存類型枚舉
    /// </summary>
    public enum InventoryType
    {
        /// <summary>
        /// 一般庫存
        /// </summary>
        Stock = 1,

        /// <summary>
        /// 在途庫存
        /// </summary>
        InTransit = 2,

        /// <summary>
        /// 保留庫存
        /// </summary>
        Reserved = 3,

        /// <summary>
        /// 損壞庫存
        /// </summary>
        Damaged = 4,

        /// <summary>
        /// 退貨庫存
        /// </summary>
        Returned = 5,

        /// <summary>
        /// 員工自留
        /// </summary>
        EmployeeRetention = 6
    }

    /// <summary>
    /// 庫存狀態枚舉
    /// </summary>
    public enum InventoryStatus
    {
        /// <summary>
        /// 正常: 庫存數量 ≥ 安全庫存
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 低庫存: 庫存數量 < 安全庫存
        /// </summary>
        LowStock = 2,

        /// <summary>
        /// 缺貨: 庫存數量 = 0
        /// </summary>
        OutOfStock = 3,

        /// <summary>
        /// 過剩: 庫存數量 > 安全庫存 × 2
        /// </summary>
        Excess = 4,

        /// <summary>
        /// 員工預留
        /// </summary>
        EmployeeReserved = 5
    }

    /// <summary>
    /// 庫存移動記錄
    /// </summary>
    public class InventoryMovement : BaseEntity
    {
        /// <summary>
        /// 庫存ID
        /// </summary>
        [Required]
        public int InventoryId { get; set; }

        /// <summary>
        /// 移動類型
        /// </summary>
        public MovementType Type { get; set; }

        /// <summary>
        /// 移動數量
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        /// 移動前數量
        /// </summary>
        [Required]
        public int PreviousQuantity { get; set; }

        /// <summary>
        /// 移動後數量
        /// </summary>
        [Required]
        public int NewQuantity { get; set; }

        /// <summary>
        /// 移動原因
        /// </summary>
        [StringLength(200)]
        public string? Reason { get; set; }

        /// <summary>
        /// 相關訂單ID
        /// </summary>
        public int? RelatedOrderId { get; set; }

        /// <summary>
        /// 導航屬性：庫存
        /// </summary>
        public virtual Inventory Inventory { get; set; } = null!;
    }

    /// <summary>
    /// 移動類型枚舉
    /// </summary>
    public enum MovementType
    {
        /// <summary>
        /// 進貨
        /// </summary>
        Inbound = 1,

        /// <summary>
        /// 出貨
        /// </summary>
        Outbound = 2,

        /// <summary>
        /// 調整
        /// </summary>
        Adjustment = 3,

        /// <summary>
        /// 轉移
        /// </summary>
        Transfer = 4,

        /// <summary>
        /// 盤點
        /// </summary>
        Count = 5
    }
}