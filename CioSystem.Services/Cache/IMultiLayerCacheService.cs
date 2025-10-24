using System;
using System.Threading.Tasks;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 多層快取服務介面
    /// 實現 L1(記憶體) -> L2(Redis) -> L3(資料庫) 的快取策略
    /// </summary>
    public interface IMultiLayerCacheService
    {
        /// <summary>
        /// 取得快取資料（多層查詢）
        /// </summary>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// 設定快取資料（多層寫入）
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CacheLayer preferredLayer = CacheLayer.L1) where T : class;

        /// <summary>
        /// 移除快取資料（多層清除）
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// 根據標籤清除快取
        /// </summary>
        Task RemoveByTagAsync(string tag);

        /// <summary>
        /// 清除所有快取
        /// </summary>
        Task ClearAllAsync();

        /// <summary>
        /// 取得快取統計資訊
        /// </summary>
        Task<CacheStatistics> GetStatisticsAsync();

        /// <summary>
        /// 檢查快取是否存在
        /// </summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// 設定快取標籤
        /// </summary>
        Task SetTagAsync(string key, string tag);

        /// <summary>
        /// 預熱快取
        /// </summary>
        Task WarmupAsync<T>(string key, Func<Task<T>> dataFactory, TimeSpan? expiration = null) where T : class;
    }

    /// <summary>
    /// 快取層級
    /// </summary>
    public enum CacheLayer
    {
        /// <summary>
        /// L1: 記憶體快取（最快）
        /// </summary>
        L1 = 1,

        /// <summary>
        /// L2: Redis 分散式快取（跨實例）
        /// </summary>
        L2 = 2,

        /// <summary>
        /// L3: 資料庫查詢快取（持久化）
        /// </summary>
        L3 = 3
    }

    /// <summary>
    /// 快取統計資訊
    /// </summary>
    public class CacheStatistics
    {
        public long L1Hits { get; set; }
        public long L1Misses { get; set; }
        public long L2Hits { get; set; }
        public long L2Misses { get; set; }
        public long L3Hits { get; set; }
        public long L3Misses { get; set; }
        public long TotalHits => L1Hits + L2Hits + L3Hits;
        public long TotalMisses => L1Misses + L2Misses + L3Misses;
        public double HitRatio => TotalHits + TotalMisses > 0 ? (double)TotalHits / (TotalHits + TotalMisses) : 0;
        public long L1Size { get; set; }
        public long L2Size { get; set; }
        public long L3Size { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}