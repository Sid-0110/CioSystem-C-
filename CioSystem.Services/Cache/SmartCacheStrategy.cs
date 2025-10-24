using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 智慧快取策略實現
    /// 根據資料特性自動選擇最佳快取策略
    /// </summary>
    public class SmartCacheStrategy : ISmartCacheStrategy
    {
        private readonly ILogger<SmartCacheStrategy> _logger;
        private readonly Dictionary<string, CachePattern> _patternCache;
        private readonly Dictionary<string, int> _accessCounts;
        private readonly Dictionary<string, DateTime> _lastAccessTimes;

        public SmartCacheStrategy(ILogger<SmartCacheStrategy> logger)
        {
            _logger = logger;
            _patternCache = new Dictionary<string, CachePattern>();
            _accessCounts = new Dictionary<string, int>();
            _lastAccessTimes = new Dictionary<string, DateTime>();
        }

        /// <summary>
        /// 根據資料類型選擇快取層級
        /// </summary>
        public CacheLayer SelectCacheLayer<T>(string key, T data, CacheContext context) where T : class
        {
            try
            {
                // 更新存取統計
                UpdateAccessStatistics(key);

                // 根據資料特性選擇層級
                if (IsHotData(key, context))
                {
                    _logger.LogDebug("熱點資料選擇 L1 快取: {Key}", key);
                    return CacheLayer.L1;
                }

                if (IsSharedData(context))
                {
                    _logger.LogDebug("共享資料選擇 L2 快取: {Key}", key);
                    return CacheLayer.L2;
                }

                if (IsPersistentData(context))
                {
                    _logger.LogDebug("持久化資料選擇 L3 快取: {Key}", key);
                    return CacheLayer.L3;
                }

                // 預設選擇 L1
                return CacheLayer.L1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "選擇快取層級失敗: {Key}", key);
                return CacheLayer.L1;
            }
        }

        /// <summary>
        /// 計算最佳過期時間
        /// </summary>
        public TimeSpan CalculateExpiration<T>(string key, T data, CacheContext context) where T : class
        {
            try
            {
                var pattern = AnalyzeCachePattern(key, context);

                return pattern switch
                {
                    CachePattern.ReadHeavy => TimeSpan.FromHours(24), // 讀取密集型，長時間快取
                    CachePattern.WriteHeavy => TimeSpan.FromMinutes(5), // 寫入密集型，短時間快取
                    CachePattern.Mixed => TimeSpan.FromHours(1), // 混合型，中等時間快取
                    CachePattern.OneTime => TimeSpan.FromMinutes(1), // 一次性，短時間快取
                    CachePattern.Periodic => TimeSpan.FromMinutes(30), // 定期更新，中等時間快取
                    _ => TimeSpan.FromMinutes(30)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "計算過期時間失敗: {Key}", key);
                return TimeSpan.FromMinutes(30);
            }
        }

        /// <summary>
        /// 判斷是否需要預熱
        /// </summary>
        public bool ShouldWarmup(string key, CacheContext context)
        {
            try
            {
                // 關鍵資料需要預熱
                if (context.IsCriticalData)
                {
                    return true;
                }

                // 熱點資料需要預熱
                if (IsHotData(key, context))
                {
                    return true;
                }

                // 根據存取模式判斷
                var pattern = AnalyzeCachePattern(key, context);
                return pattern == CachePattern.ReadHeavy || pattern == CachePattern.Periodic;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "判斷預熱需求失敗: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 判斷是否需要自動失效
        /// </summary>
        public bool ShouldAutoInvalidate(string key, CacheContext context)
        {
            try
            {
                // 寫入密集型資料需要自動失效
                var pattern = AnalyzeCachePattern(key, context);
                return pattern == CachePattern.WriteHeavy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "判斷自動失效需求失敗: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 取得快取優先級
        /// </summary>
        public CachePriority GetCachePriority<T>(string key, T data, CacheContext context) where T : class
        {
            try
            {
                if (context.IsCriticalData)
                {
                    return CachePriority.Critical;
                }

                if (IsHotData(key, context))
                {
                    return CachePriority.High;
                }

                if (IsSharedData(context))
                {
                    return CachePriority.Normal;
                }

                return CachePriority.Low;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得快取優先級失敗: {Key}", key);
                return CachePriority.Normal;
            }
        }

        /// <summary>
        /// 分析快取模式
        /// </summary>
        public CachePattern AnalyzeCachePattern(string key, CacheContext context)
        {
            try
            {
                // 檢查快取模式
                if (_patternCache.TryGetValue(key, out var cachedPattern))
                {
                    return cachedPattern;
                }

                var pattern = DetermineCachePattern(key, context);
                _patternCache[key] = pattern;

                _logger.LogDebug("分析快取模式: {Key} -> {Pattern}", key, pattern);
                return pattern;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析快取模式失敗: {Key}", key);
                return CachePattern.Mixed;
            }
        }

        /// <summary>
        /// 更新存取統計
        /// </summary>
        private void UpdateAccessStatistics(string key)
        {
            _accessCounts[key] = _accessCounts.GetValueOrDefault(key, 0) + 1;
            _lastAccessTimes[key] = DateTime.UtcNow;
        }

        /// <summary>
        /// 判斷是否為熱點資料
        /// </summary>
        private bool IsHotData(string key, CacheContext context)
        {
            var accessCount = _accessCounts.GetValueOrDefault(key, 0);
            var isRecentlyAccessed = _lastAccessTimes.TryGetValue(key, out var lastAccess) &&
                                   DateTime.UtcNow - lastAccess < TimeSpan.FromMinutes(5);

            return accessCount > 10 || isRecentlyAccessed || context.IsHotData;
        }

        /// <summary>
        /// 判斷是否為共享資料
        /// </summary>
        private bool IsSharedData(CacheContext context)
        {
            return context.DataType.Contains("Product") ||
                   context.DataType.Contains("Category") ||
                   context.DataType.Contains("Configuration");
        }

        /// <summary>
        /// 判斷是否為持久化資料
        /// </summary>
        private bool IsPersistentData(CacheContext context)
        {
            return context.DataType.Contains("Statistics") ||
                   context.DataType.Contains("Report") ||
                   context.DataType.Contains("Analytics");
        }

        /// <summary>
        /// 確定快取模式
        /// </summary>
        private CachePattern DetermineCachePattern(string key, CacheContext context)
        {
            var accessCount = _accessCounts.GetValueOrDefault(key, 0);
            var timeSinceLastAccess = _lastAccessTimes.TryGetValue(key, out var lastAccess)
                ? DateTime.UtcNow - lastAccess
                : TimeSpan.MaxValue;

            // 根據存取模式判斷
            if (accessCount > 50 && timeSinceLastAccess < TimeSpan.FromMinutes(10))
            {
                return CachePattern.ReadHeavy;
            }

            if (context.Operation.Contains("Write") || context.Operation.Contains("Update"))
            {
                return CachePattern.WriteHeavy;
            }

            if (context.DataType.Contains("Statistics") || context.DataType.Contains("Report"))
            {
                return CachePattern.Periodic;
            }

            if (accessCount == 1)
            {
                return CachePattern.OneTime;
            }

            return CachePattern.Mixed;
        }
    }
}