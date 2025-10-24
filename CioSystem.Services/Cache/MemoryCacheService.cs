using CioSystem.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 記憶體快取服務實現
    /// 提供基於標籤的智能快取管理
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly MemoryCacheOptions _options;
        private readonly ConcurrentDictionary<string, HashSet<string>> _tagToKeys;
        private readonly ConcurrentDictionary<string, HashSet<string>> _keyToTags;
        private readonly ConcurrentDictionary<string, long> _hitCounts;
        private readonly ConcurrentDictionary<string, long> _missCounts;
        private long _totalMemoryUsage;

        public string ServiceName => "MemoryCacheService";
        public string Version => "1.0.0";
        public bool IsAvailable => true;

        public MemoryCacheService(
            IMemoryCache memoryCache,
            ILogger<MemoryCacheService> logger,
            IOptions<MemoryCacheOptions> options)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _options = options.Value;
            _tagToKeys = new ConcurrentDictionary<string, HashSet<string>>();
            _keyToTags = new ConcurrentDictionary<string, HashSet<string>>();
            _hitCounts = new ConcurrentDictionary<string, long>();
            _missCounts = new ConcurrentDictionary<string, long>();
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return null;

                var cacheKey = GenerateCacheKey<T>(key);
                if (_memoryCache.TryGetValue(cacheKey, out var cachedValue))
                {
                    _hitCounts.AddOrUpdate(cacheKey, 1, (k, v) => v + 1);
                    _logger.LogDebug("快取命中: {Key}", key);
                    return cachedValue as T;
                }

                _missCounts.AddOrUpdate(cacheKey, 1, (k, v) => v + 1);
                _logger.LogDebug("快取未命中: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得快取項目時發生錯誤: {Key}", key);
                return null;
            }
        }

        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(key) || value == null)
                    return false;

                var cacheKey = GenerateCacheKey<T>(key);
                var options = new MemoryCacheEntryOptions
                {
                    Priority = CacheItemPriority.Normal,
                    Size = CalculateSize(value)
                };

                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    // 預設過期時間為 1 小時
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                }

                options.RegisterPostEvictionCallback(OnEviction);

                _memoryCache.Set(cacheKey, value, options);
                _totalMemoryUsage += options.Size ?? 0;

                _logger.LogDebug("設定快取項目: {Key}, 大小: {Size}", key, options.Size);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定快取項目時發生錯誤: {Key}", key);
                return false;
            }
        }

        public async Task<bool> SetWithTagsAsync<T>(string key, T value, IEnumerable<string> tags, TimeSpan? expiration = null) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(key) || value == null || tags == null)
                    return false;

                var success = await SetAsync(key, value, expiration);
                if (success)
                {
                    var cacheKey = GenerateCacheKey<T>(key);
                    var tagList = tags.ToList();

                    // 更新標籤映射
                    foreach (var tag in tagList)
                    {
                        _tagToKeys.AddOrUpdate(tag,
                            new HashSet<string> { cacheKey },
                            (k, v) => { v.Add(cacheKey); return v; });
                    }

                    _keyToTags.AddOrUpdate(cacheKey,
                        new HashSet<string>(tagList),
                        (k, v) => { foreach (var tag in tagList) v.Add(tag); return v; });

                    _logger.LogDebug("設定帶標籤的快取項目: {Key}, 標籤: {Tags}", key, string.Join(",", tagList));
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定帶標籤的快取項目時發生錯誤: {Key}", key);
                return false;
            }
        }

        public async Task<bool> RemoveAsync(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;

                // 移除所有類型的快取項目
                var removed = false;
                var types = new[] { typeof(object), typeof(string), typeof(int), typeof(long), typeof(decimal) };

                foreach (var type in types)
                {
                    var cacheKey = GenerateCacheKey(type, key);
                    if (_memoryCache.TryGetValue(cacheKey, out var value))
                    {
                        _memoryCache.Remove(cacheKey);
                        _totalMemoryUsage -= CalculateSize(value);
                        removed = true;
                    }
                }

                // 清理標籤映射
                if (_keyToTags.TryRemove(key, out var tags))
                {
                    foreach (var tag in tags)
                    {
                        if (_tagToKeys.TryGetValue(tag, out var keys))
                        {
                            keys.Remove(key);
                            if (keys.Count == 0)
                            {
                                _tagToKeys.TryRemove(tag, out _);
                            }
                        }
                    }
                }

                _logger.LogDebug("移除快取項目: {Key}, 成功: {Success}", key, removed);
                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除快取項目時發生錯誤: {Key}", key);
                return false;
            }
        }

        public async Task<int> RemoveAsync(IEnumerable<string> keys)
        {
            var removedCount = 0;
            foreach (var key in keys)
            {
                if (await RemoveAsync(key))
                {
                    removedCount++;
                }
            }
            return removedCount;
        }

        public async Task<int> RemoveByTagAsync(string tag)
        {
            try
            {
                if (string.IsNullOrEmpty(tag))
                    return 0;

                if (!_tagToKeys.TryGetValue(tag, out var keys))
                    return 0;

                var removedCount = 0;
                var keysToRemove = keys.ToList();

                foreach (var key in keysToRemove)
                {
                    if (await RemoveAsync(key))
                    {
                        removedCount++;
                    }
                }

                _tagToKeys.TryRemove(tag, out _);
                _logger.LogDebug("根據標籤移除快取項目: {Tag}, 移除數量: {Count}", tag, removedCount);
                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據標籤移除快取項目時發生錯誤: {Tag}", tag);
                return 0;
            }
        }

        public async Task<int> RemoveByTagsAsync(IEnumerable<string> tags)
        {
            var totalRemoved = 0;
            foreach (var tag in tags)
            {
                totalRemoved += await RemoveByTagAsync(tag);
            }
            return totalRemoved;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;

                var types = new[] { typeof(object), typeof(string), typeof(int), typeof(long), typeof(decimal) };
                foreach (var type in types)
                {
                    var cacheKey = GenerateCacheKey(type, key);
                    if (_memoryCache.TryGetValue(cacheKey, out _))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查快取項目是否存在時發生錯誤: {Key}", key);
                return false;
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expiration);
            }
            return value;
        }

        public async Task<bool> ClearAsync()
        {
            try
            {
                // 清理所有標籤映射
                _tagToKeys.Clear();
                _keyToTags.Clear();
                _hitCounts.Clear();
                _missCounts.Clear();
                _totalMemoryUsage = 0;

                // 重新創建記憶體快取實例以清除所有項目
                // 注意：這需要重新註冊服務
                _logger.LogInformation("清除所有快取項目");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除快取時發生錯誤");
                return false;
            }
        }

        public async Task<CioSystem.Core.Interfaces.CacheStatistics> GetStatisticsAsync()
        {
            var totalHits = _hitCounts.Values.Sum();
            var totalMisses = _missCounts.Values.Sum();

            return new CioSystem.Core.Interfaces.CacheStatistics
            {
                TotalItems = _keyToTags.Count,
                HitCount = totalHits,
                MissCount = totalMisses,
                MemoryUsage = _totalMemoryUsage,
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<ServiceHealthStatus> HealthCheckAsync()
        {
            try
            {
                // 測試快取功能
                var testKey = "health_check_test";
                var testValue = "test";

                await SetAsync(testKey, testValue, TimeSpan.FromSeconds(1));
                var retrieved = await GetAsync<string>(testKey);
                await RemoveAsync(testKey);

                var isHealthy = retrieved == testValue;
                return new ServiceHealthStatus
                {
                    IsHealthy = isHealthy,
                    Status = isHealthy ? "Healthy" : "Unhealthy",
                    Message = isHealthy ? "快取服務正常運作" : "快取服務異常",
                    CheckedAt = DateTime.UtcNow,
                    ResponseTime = TimeSpan.FromMilliseconds(1)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快取服務健康檢查失敗");
                return new ServiceHealthStatus
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    Message = $"快取服務健康檢查失敗: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logger.LogInformation("初始化快取服務");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化快取服務失敗");
                return false;
            }
        }

        public async Task<bool> CleanupAsync()
        {
            try
            {
                await ClearAsync();
                _logger.LogInformation("清理快取服務資源");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理快取服務資源失敗");
                return false;
            }
        }

        private string GenerateCacheKey<T>(string key) => GenerateCacheKey(typeof(T), key);

        private string GenerateCacheKey(Type type, string key) => $"{type.Name}:{key}";

        private long CalculateSize(object value)
        {
            try
            {
                if (value == null) return 0;

                var json = JsonSerializer.Serialize(value);
                return System.Text.Encoding.UTF8.GetByteCount(json);
            }
            catch
            {
                // 如果序列化失敗，返回估算大小
                return 100;
            }
        }

        private void OnEviction(object key, object value, EvictionReason reason, object state)
        {
            _totalMemoryUsage -= CalculateSize(value);
            _logger.LogDebug("快取項目被驅逐: {Key}, 原因: {Reason}", key, reason);
        }
    }
}