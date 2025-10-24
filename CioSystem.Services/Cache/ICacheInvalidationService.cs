using System;
using System.Threading.Tasks;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 快取失效服務介面
    /// 負責智慧快取失效和更新策略
    /// </summary>
    public interface ICacheInvalidationService
    {
        /// <summary>
        /// 根據實體類型失效相關快取
        /// </summary>
        Task InvalidateByEntityTypeAsync<T>(T entity, string operation) where T : class;

        /// <summary>
        /// 根據標籤失效快取
        /// </summary>
        Task InvalidateByTagAsync(string tag);

        /// <summary>
        /// 根據模式失效快取
        /// </summary>
        Task InvalidateByPatternAsync(string pattern);

        /// <summary>
        /// 智慧失效（根據資料變更自動判斷）
        /// </summary>
        Task SmartInvalidateAsync<T>(T oldEntity, T newEntity, string operation) where T : class;

        /// <summary>
        /// 延遲失效（在指定時間後失效）
        /// </summary>
        Task DelayedInvalidateAsync(string key, TimeSpan delay);

        /// <summary>
        /// 條件失效（根據條件判斷是否失效）
        /// </summary>
        Task ConditionalInvalidateAsync<T>(T entity, Func<T, bool> condition, string operation) where T : class;

        /// <summary>
        /// 批量失效
        /// </summary>
        Task BatchInvalidateAsync(string[] keys);

        /// <summary>
        /// 失效統計
        /// </summary>
        Task<InvalidationStatistics> GetInvalidationStatisticsAsync();
    }

    /// <summary>
    /// 失效統計
    /// </summary>
    public class InvalidationStatistics
    {
        public long TotalInvalidations { get; set; }
        public long EntityInvalidations { get; set; }
        public long TagInvalidations { get; set; }
        public long PatternInvalidations { get; set; }
        public long SmartInvalidations { get; set; }
        public long DelayedInvalidations { get; set; }
        public long ConditionalInvalidations { get; set; }
        public long BatchInvalidations { get; set; }
        public DateTime LastInvalidation { get; set; }
        public TimeSpan AverageInvalidationTime { get; set; }
    }
}