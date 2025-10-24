using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CioSystem.Core.Interfaces
{
    /// <summary>
    /// 快取服務接口
    /// 提供統一的快取操作抽象
    /// </summary>
    public interface ICacheService : IService
    {
        /// <summary>
        /// 取得快取項目
        /// </summary>
        /// <typeparam name="T">快取項目類型</typeparam>
        /// <param name="key">快取鍵</param>
        /// <returns>快取項目，如果不存在則返回 null</returns>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// 設定快取項目
        /// </summary>
        /// <typeparam name="T">快取項目類型</typeparam>
        /// <param name="key">快取鍵</param>
        /// <param name="value">快取值</param>
        /// <param name="expiration">過期時間（可選）</param>
        /// <returns>是否成功</returns>
        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// 移除快取項目
        /// </summary>
        /// <param name="key">快取鍵</param>
        /// <returns>是否成功</returns>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// 移除多個快取項目
        /// </summary>
        /// <param name="keys">快取鍵列表</param>
        /// <returns>成功移除的數量</returns>
        Task<int> RemoveAsync(IEnumerable<string> keys);

        /// <summary>
        /// 檢查快取項目是否存在
        /// </summary>
        /// <param name="key">快取鍵</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// 取得或設定快取項目
        /// </summary>
        /// <typeparam name="T">快取項目類型</typeparam>
        /// <param name="key">快取鍵</param>
        /// <param name="factory">當快取不存在時的工廠方法</param>
        /// <param name="expiration">過期時間（可選）</param>
        /// <returns>快取項目</returns>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// 清除所有快取
        /// </summary>
        /// <returns>是否成功</returns>
        Task<bool> ClearAsync();

        /// <summary>
        /// 取得快取統計資訊
        /// </summary>
        /// <returns>快取統計資訊</returns>
        Task<CacheStatistics> GetStatisticsAsync();

        /// <summary>
        /// 設定快取項目（帶有標籤）
        /// </summary>
        /// <typeparam name="T">快取項目類型</typeparam>
        /// <param name="key">快取鍵</param>
        /// <param name="value">快取值</param>
        /// <param name="tags">標籤列表</param>
        /// <param name="expiration">過期時間（可選）</param>
        /// <returns>是否成功</returns>
        Task<bool> SetWithTagsAsync<T>(string key, T value, IEnumerable<string> tags, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// 根據標籤移除快取項目
        /// </summary>
        /// <param name="tag">標籤</param>
        /// <returns>成功移除的數量</returns>
        Task<int> RemoveByTagAsync(string tag);

        /// <summary>
        /// 根據標籤移除多個快取項目
        /// </summary>
        /// <param name="tags">標籤列表</param>
        /// <returns>成功移除的數量</returns>
        Task<int> RemoveByTagsAsync(IEnumerable<string> tags);
    }

    /// <summary>
    /// 快取統計資訊
    /// </summary>
    public class CacheStatistics
    {
        public long TotalItems { get; set; }
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
        public long TotalRequests => HitCount + MissCount;
        public long MemoryUsage { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}