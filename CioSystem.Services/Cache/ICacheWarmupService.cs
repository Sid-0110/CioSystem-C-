using System;
using System.Threading.Tasks;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 快取預熱服務介面
    /// 負責在系統啟動時預熱關鍵快取資料
    /// </summary>
    public interface ICacheWarmupService
    {
        /// <summary>
        /// 預熱所有關鍵快取
        /// </summary>
        Task WarmupAllAsync();

        /// <summary>
        /// 預熱產品快取
        /// </summary>
        Task WarmupProductsAsync();

        /// <summary>
        /// 預熱庫存快取
        /// </summary>
        Task WarmupInventoryAsync();

        /// <summary>
        /// 預熱統計資料快取
        /// </summary>
        Task WarmupStatisticsAsync();

        /// <summary>
        /// 預熱配置快取
        /// </summary>
        Task WarmupConfigurationAsync();

        /// <summary>
        /// 預熱用戶相關快取
        /// </summary>
        Task WarmupUserDataAsync();

        /// <summary>
        /// 檢查預熱狀態
        /// </summary>
        Task<WarmupStatus> GetWarmupStatusAsync();

        /// <summary>
        /// 重新預熱指定類型的快取
        /// </summary>
        Task RewarmupAsync(string cacheType);
    }

    /// <summary>
    /// 預熱狀態
    /// </summary>
    public class WarmupStatus
    {
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
        public int TotalItems { get; set; }
        public int CompletedItems { get; set; }
        public int FailedItems { get; set; }
        public double ProgressPercentage => TotalItems > 0 ? (double)CompletedItems / TotalItems * 100 : 0;
        public string CurrentOperation { get; set; } = string.Empty;
        public string LastError { get; set; } = string.Empty;
    }
}