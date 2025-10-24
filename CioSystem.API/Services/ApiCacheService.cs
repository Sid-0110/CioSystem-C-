using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace CioSystem.API.Services
{
    /// <summary>
    /// API 快取服務實現
    /// 提供 API 響應快取功能
    /// </summary>
    public class ApiCacheService : IApiCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<ApiCacheService> _logger;
        private readonly ApiCacheStats _stats;
        private readonly object _statsLock = new object();

        public ApiCacheService(IMemoryCache cache, ILogger<ApiCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _stats = new ApiCacheStats();
        }

        public async Task<T?> GetCachedResponseAsync<T>(string cacheKey)
        {
            try
            {
                if (_cache.TryGetValue(cacheKey, out var cachedValue))
                {
                    lock (_statsLock)
                    {
                        _stats.TotalCacheHits++;
                        UpdateCacheHitRate();
                    }

                    _logger.LogDebug("快取命中: {CacheKey}", cacheKey);
                    return (T)cachedValue;
                }

                lock (_statsLock)
                {
                    _stats.TotalCacheMisses++;
                    UpdateCacheHitRate();
                }

                _logger.LogDebug("快取未命中: {CacheKey}", cacheKey);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得快取響應時發生錯誤: {CacheKey}", cacheKey);
                return default(T);
            }
        }

        public async Task SetCachedResponseAsync<T>(string cacheKey, T response, TimeSpan expiration)
        {
            try
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration,
                    Priority = CacheItemPriority.Normal,
                    Size = CalculateSize(response)
                };

                _cache.Set(cacheKey, response, cacheEntryOptions);

                lock (_statsLock)
                {
                    _stats.TotalCachedItems++;
                    _stats.TotalCacheSize += CalculateSize(response);
                    _stats.LastUpdated = DateTime.UtcNow;
                }

                _logger.LogDebug("設定快取響應: {CacheKey}, 過期時間: {Expiration}", cacheKey, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定快取響應時發生錯誤: {CacheKey}", cacheKey);
            }
        }

        public async Task RemoveCachedResponseAsync(string cacheKey)
        {
            try
            {
                _cache.Remove(cacheKey);
                _logger.LogDebug("移除快取項目: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除快取項目時發生錯誤: {CacheKey}", cacheKey);
            }
        }

        public async Task ClearAllCacheAsync()
        {
            try
            {
                if (_cache is MemoryCache memoryCache)
                {
                    memoryCache.Compact(1.0); // 清除所有快取
                }

                lock (_statsLock)
                {
                    _stats.TotalCachedItems = 0;
                    _stats.TotalCacheSize = 0;
                    _stats.LastUpdated = DateTime.UtcNow;
                }

                _logger.LogInformation("清除所有 API 快取");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除所有快取時發生錯誤");
            }
        }

        public string GenerateCacheKey(string controller, string action, Dictionary<string, object>? parameters = null)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append($"api:{controller.ToLower()}:{action.ToLower()}");

            if (parameters != null && parameters.Count > 0)
            {
                var sortedParams = parameters.OrderBy(p => p.Key);
                foreach (var param in sortedParams)
                {
                    keyBuilder.Append($":{param.Key}={param.Value}");
                }
            }

            var key = keyBuilder.ToString();

            // 如果鍵太長，使用雜湊
            if (key.Length > 200)
            {
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                key = $"api:hash:{Convert.ToBase64String(hashBytes)[..16]}";
            }

            return key;
        }

        public async Task<ApiCacheStats> GetCacheStatsAsync()
        {
            lock (_statsLock)
            {
                return new ApiCacheStats
                {
                    TotalCacheHits = _stats.TotalCacheHits,
                    TotalCacheMisses = _stats.TotalCacheMisses,
                    CacheHitRate = _stats.CacheHitRate,
                    TotalCachedItems = _stats.TotalCachedItems,
                    TotalCacheSize = _stats.TotalCacheSize,
                    LastUpdated = _stats.LastUpdated
                };
            }
        }

        private void UpdateCacheHitRate()
        {
            var total = _stats.TotalCacheHits + _stats.TotalCacheMisses;
            _stats.CacheHitRate = total > 0 ? (double)_stats.TotalCacheHits / total : 0;
        }

        private static long CalculateSize<T>(T obj)
        {
            try
            {
                var json = JsonSerializer.Serialize(obj);
                return Encoding.UTF8.GetByteCount(json);
            }
            catch
            {
                return 1024; // 預設大小
            }
        }
    }
}