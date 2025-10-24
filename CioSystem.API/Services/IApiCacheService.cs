namespace CioSystem.API.Services
{
    /// <summary>
    /// API 快取服務介面
    /// 提供 API 響應快取功能
    /// </summary>
    public interface IApiCacheService
    {
        /// <summary>
        /// 取得快取的 API 響應
        /// </summary>
        /// <typeparam name="T">響應類型</typeparam>
        /// <param name="cacheKey">快取鍵</param>
        /// <returns>快取的響應或 null</returns>
        Task<T?> GetCachedResponseAsync<T>(string cacheKey);

        /// <summary>
        /// 設定 API 響應快取
        /// </summary>
        /// <typeparam name="T">響應類型</typeparam>
        /// <param name="cacheKey">快取鍵</param>
        /// <param name="response">響應資料</param>
        /// <param name="expiration">過期時間</param>
        Task SetCachedResponseAsync<T>(string cacheKey, T response, TimeSpan expiration);

        /// <summary>
        /// 移除快取項目
        /// </summary>
        /// <param name="cacheKey">快取鍵</param>
        Task RemoveCachedResponseAsync(string cacheKey);

        /// <summary>
        /// 清除所有快取
        /// </summary>
        Task ClearAllCacheAsync();

        /// <summary>
        /// 生成快取鍵
        /// </summary>
        /// <param name="controller">控制器名稱</param>
        /// <param name="action">動作名稱</param>
        /// <param name="parameters">參數</param>
        /// <returns>快取鍵</returns>
        string GenerateCacheKey(string controller, string action, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// 取得快取統計
        /// </summary>
        /// <returns>快取統計</returns>
        Task<ApiCacheStats> GetCacheStatsAsync();
    }

    /// <summary>
    /// API 快取統計
    /// </summary>
    public class ApiCacheStats
    {
        public int TotalCacheHits { get; set; }
        public int TotalCacheMisses { get; set; }
        public double CacheHitRate { get; set; }
        public int TotalCachedItems { get; set; }
        public long TotalCacheSize { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}