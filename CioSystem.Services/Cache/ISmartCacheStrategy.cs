using System;
using System.Threading.Tasks;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 智慧快取策略介面
    /// 根據資料特性自動選擇最佳快取策略
    /// </summary>
    public interface ISmartCacheStrategy
    {
        /// <summary>
        /// 根據資料類型選擇快取層級
        /// </summary>
        CacheLayer SelectCacheLayer<T>(string key, T data, CacheContext context) where T : class;

        /// <summary>
        /// 計算最佳過期時間
        /// </summary>
        TimeSpan CalculateExpiration<T>(string key, T data, CacheContext context) where T : class;

        /// <summary>
        /// 判斷是否需要預熱
        /// </summary>
        bool ShouldWarmup(string key, CacheContext context);

        /// <summary>
        /// 判斷是否需要自動失效
        /// </summary>
        bool ShouldAutoInvalidate(string key, CacheContext context);

        /// <summary>
        /// 取得快取優先級
        /// </summary>
        CachePriority GetCachePriority<T>(string key, T data, CacheContext context) where T : class;

        /// <summary>
        /// 分析快取模式
        /// </summary>
        CachePattern AnalyzeCachePattern(string key, CacheContext context);
    }

    /// <summary>
    /// 快取上下文
    /// </summary>
    public class CacheContext
    {
        public string DataType { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public DateTime AccessTime { get; set; } = DateTime.UtcNow;
        public int AccessCount { get; set; } = 0;
        public TimeSpan LastAccessInterval { get; set; } = TimeSpan.Zero;
        public bool IsHotData { get; set; } = false;
        public bool IsCriticalData { get; set; } = false;
        public long DataSize { get; set; } = 0;
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 快取優先級
    /// </summary>
    public enum CachePriority
    {
        /// <summary>
        /// 低優先級
        /// </summary>
        Low = 1,

        /// <summary>
        /// 普通優先級
        /// </summary>
        Normal = 2,

        /// <summary>
        /// 高優先級
        /// </summary>
        High = 3,

        /// <summary>
        /// 關鍵優先級
        /// </summary>
        Critical = 4
    }

    /// <summary>
    /// 快取模式
    /// </summary>
    public enum CachePattern
    {
        /// <summary>
        /// 讀取密集型
        /// </summary>
        ReadHeavy,

        /// <summary>
        /// 寫入密集型
        /// </summary>
        WriteHeavy,

        /// <summary>
        /// 混合型
        /// </summary>
        Mixed,

        /// <summary>
        /// 一次性
        /// </summary>
        OneTime,

        /// <summary>
        /// 定期更新
        /// </summary>
        Periodic
    }
}