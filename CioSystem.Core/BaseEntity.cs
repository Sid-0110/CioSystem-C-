using System;

namespace CioSystem.Core
{
    /// <summary>
    /// 基礎實體類別 - 包含所有實體的通用屬性
    /// 這是學習建構模式的核心基礎類別
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 實體的唯一識別碼
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最後更新時間
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否已刪除（軟刪除標記）
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// 建立者
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// 更新者
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;
    }
}