using CioSystem.Models;

namespace CioSystem.Services.DTOs
{
    /// <summary>
    /// 帶序號的進貨記錄 DTO
    /// </summary>
    public class PurchaseWithSequenceDto
    {
        /// <summary>
        /// 進貨記錄
        /// </summary>
        public Purchase Purchase { get; set; } = null!;

        /// <summary>
        /// 連續序號
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// 總記錄數
        /// </summary>
        public int TotalCount { get; set; }
    }
}