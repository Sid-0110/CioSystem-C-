using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CioSystem.Services.Cache.Redis;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 多層快取服務實現
    /// 實現 L1(記憶體) -> L2(Redis) -> L3(資料庫) 的快取策略
    /// </summary>
    public class MultiLayerCacheService : IMultiLayerCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<MultiLayerCacheService> _logger;
        private readonly CacheConfiguration _config;
        private readonly ConcurrentDictionary<string, long> _l1Hits;
        private readonly ConcurrentDictionary<string, long> _l1Misses;
        private readonly ConcurrentDictionary<string, long> _l2Hits;
        private readonly ConcurrentDictionary<string, long> _l2Misses;
        private readonly ConcurrentDictionary<string, long> _l3Hits;
        private readonly ConcurrentDictionary<string, long> _l3Misses;
        private readonly ConcurrentDictionary<string, string> _keyToTag;
        private readonly ConcurrentDictionary<string, HashSet<string>> _tagToKeys;

        public MultiLayerCacheService(
            IMemoryCache memoryCache,
            IRedisCacheService redisCacheService,
            ILogger<MultiLayerCacheService> logger,
            CacheConfiguration config)
        {
            _memoryCache = memoryCache;
            _redisCacheService = redisCacheService;
            _logger = logger;
            _config = config;
            _l1Hits = new ConcurrentDictionary<string, long>();
            _l1Misses = new ConcurrentDictionary<string, long>();
            _l2Hits = new ConcurrentDictionary<string, long>();
            _l2Misses = new ConcurrentDictionary<string, long>();
            _l3Hits = new ConcurrentDictionary<string, long>();
            _l3Misses = new ConcurrentDictionary<string, long>();
            _keyToTag = new ConcurrentDictionary<string, string>();
            _tagToKeys = new ConcurrentDictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// 取得快取資料（多層查詢）
        /// </summary>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                _logger.LogDebug("開始多層快取查詢: {Key}", key);

                // L1: 記憶體快取查詢
                if (_memoryCache.TryGetValue(key, out T? l1Value) && l1Value != null)
                {
                    _l1Hits.AddOrUpdate(key, 1, (k, v) => v + 1);
                    _logger.LogDebug("L1 快取命中: {Key}", key);
                    return l1Value;
                }
                _l1Misses.AddOrUpdate(key, 1, (k, v) => v + 1);

                // L2: Redis 快取查詢
                var l2Value = await _redisCacheService.GetAsync<T>(key);
                if (l2Value != null)
                {
                    _l2Hits.AddOrUpdate(key, 1, (k, v) => v + 1);
                    _logger.LogDebug("L2 快取命中: {Key}", key);

                    // 回寫到 L1 快取
                    await SetL1CacheAsync(key, l2Value, TimeSpan.FromMinutes(5));
                    return l2Value;
                }
                _l2Misses.AddOrUpdate(key, 1, (k, v) => v + 1);

                // L3: 資料庫查詢快取（這裡可以實現資料庫查詢快取邏輯）
                _l3Misses.AddOrUpdate(key, 1, (k, v) => v + 1);
                _logger.LogDebug("所有快取層都未命中: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "多層快取查詢失敗: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// 設定快取資料（多層寫入）
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CacheLayer preferredLayer = CacheLayer.L1) where T : class
        {
            try
            {
                var exp = expiration ?? _config.DefaultExpiration;
                _logger.LogDebug("設定多層快取: {Key}, Layer: {Layer}, Expiration: {Expiration}",
                    key, preferredLayer, exp);

                // 根據偏好層級決定寫入策略
                switch (preferredLayer)
                {
                    case CacheLayer.L1:
                        await SetL1CacheAsync(key, value, exp);
                        break;
                    case CacheLayer.L2:
                        await _redisCacheService.SetAsync(key, value, exp);
                        // 同時寫入 L1 作為熱點快取
                        await SetL1CacheAsync(key, value, TimeSpan.FromMinutes(5));
                        break;
                    case CacheLayer.L3:
                        // L3 通常是資料庫查詢快取，這裡可以實現持久化邏輯
                        await _redisCacheService.SetAsync(key, value, exp);
                        await SetL1CacheAsync(key, value, TimeSpan.FromMinutes(5));
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "多層快取設定失敗: {Key}", key);
            }
        }

        /// <summary>
        /// 移除快取資料（多層清除）
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                _logger.LogDebug("移除多層快取: {Key}", key);

                // 移除 L1 快取
                _memoryCache.Remove(key);

                // 移除 L2 快取
                await _redisCacheService.RemoveAsync(key);

                // 清除統計資料
                _l1Hits.TryRemove(key, out _);
                _l1Misses.TryRemove(key, out _);
                _l2Hits.TryRemove(key, out _);
                _l2Misses.TryRemove(key, out _);
                _l3Hits.TryRemove(key, out _);
                _l3Misses.TryRemove(key, out _);

                // 清除標籤關聯
                if (_keyToTag.TryRemove(key, out var tag))
                {
                    if (_tagToKeys.TryGetValue(tag, out var keys))
                    {
                        keys.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "多層快取移除失敗: {Key}", key);
            }
        }

        /// <summary>
        /// 根據標籤清除快取
        /// </summary>
        public async Task RemoveByTagAsync(string tag)
        {
            try
            {
                _logger.LogDebug("根據標籤移除快取: {Tag}", tag);

                if (_tagToKeys.TryGetValue(tag, out var keys))
                {
                    foreach (var key in keys)
                    {
                        await RemoveAsync(key);
                    }
                    _tagToKeys.TryRemove(tag, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據標籤移除快取失敗: {Tag}", tag);
            }
        }

        /// <summary>
        /// 清除所有快取
        /// </summary>
        public async Task ClearAllAsync()
        {
            try
            {
                _logger.LogInformation("清除所有多層快取");

                // 清除 L1 快取
                if (_memoryCache is MemoryCache mc)
                {
                    mc.Compact(1.0);
                }

                // 清除 L2 快取（Redis）
                await _redisCacheService.RemoveByPatternAsync("*");

                // 清除統計資料
                _l1Hits.Clear();
                _l1Misses.Clear();
                _l2Hits.Clear();
                _l2Misses.Clear();
                _l3Hits.Clear();
                _l3Misses.Clear();
                _keyToTag.Clear();
                _tagToKeys.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除所有快取失敗");
            }
        }

        /// <summary>
        /// 取得快取統計資訊
        /// </summary>
        public async Task<CacheStatistics> GetStatisticsAsync()
        {
            try
            {
                var stats = new CacheStatistics
                {
                    L1Hits = _l1Hits.Values.Sum(),
                    L1Misses = _l1Misses.Values.Sum(),
                    L2Hits = _l2Hits.Values.Sum(),
                    L2Misses = _l2Misses.Values.Sum(),
                    L3Hits = _l3Hits.Values.Sum(),
                    L3Misses = _l3Misses.Values.Sum(),
                    L1Size = _l1Hits.Count,
                    L2Size = await GetL2SizeAsync(),
                    L3Size = 0, // L3 大小需要根據實際實現計算
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogDebug("快取統計: L1命中率={L1HitRatio:F2}%, L2命中率={L2HitRatio:F2}%, 總命中率={TotalHitRatio:F2}%",
                    stats.L1Hits + stats.L1Misses > 0 ? (double)stats.L1Hits / (stats.L1Hits + stats.L1Misses) * 100 : 0,
                    stats.L2Hits + stats.L2Misses > 0 ? (double)stats.L2Hits / (stats.L2Hits + stats.L2Misses) * 100 : 0,
                    stats.HitRatio * 100);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得快取統計失敗");
                return new CacheStatistics();
            }
        }

        /// <summary>
        /// 檢查快取是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                // 檢查 L1
                if (_memoryCache.TryGetValue(key, out _))
                {
                    return true;
                }

                // 檢查 L2
                return await _redisCacheService.ExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查快取存在性失敗: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 設定快取標籤
        /// </summary>
        public async Task SetTagAsync(string key, string tag)
        {
            try
            {
                _keyToTag.AddOrUpdate(key, tag, (k, v) => tag);
                _tagToKeys.AddOrUpdate(tag,
                    new HashSet<string> { key },
                    (t, keys) => { keys.Add(key); return keys; });

                _logger.LogDebug("設定快取標籤: {Key} -> {Tag}", key, tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定快取標籤失敗: {Key} -> {Tag}", key, tag);
            }
        }

        /// <summary>
        /// 預熱快取
        /// </summary>
        public async Task WarmupAsync<T>(string key, Func<Task<T>> dataFactory, TimeSpan? expiration = null) where T : class
        {
            try
            {
                _logger.LogDebug("開始預熱快取: {Key}", key);

                // 檢查是否已存在
                if (await ExistsAsync(key))
                {
                    _logger.LogDebug("快取已存在，跳過預熱: {Key}", key);
                    return;
                }

                // 載入資料
                var data = await dataFactory();
                if (data != null)
                {
                    await SetAsync(key, data, expiration, CacheLayer.L1);
                    _logger.LogDebug("快取預熱完成: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快取預熱失敗: {Key}", key);
            }
        }

        /// <summary>
        /// 設定 L1 快取
        /// </summary>
        private async Task SetL1CacheAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            try
            {
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration,
                    Priority = CacheItemPriority.Normal,
                    Size = 1
                };

                _memoryCache.Set(key, value, options);
                _logger.LogDebug("L1 快取設定完成: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "L1 快取設定失敗: {Key}", key);
            }
        }

        /// <summary>
        /// 取得 L2 快取大小
        /// </summary>
        private async Task<long> GetL2SizeAsync()
        {
            try
            {
                // 這裡可以實現 Redis 快取大小查詢
                // 暫時返回統計資料中的 L2 大小
                return _l2Hits.Count + _l2Misses.Count;
            }
            catch
            {
                return 0;
            }
        }
    }
}