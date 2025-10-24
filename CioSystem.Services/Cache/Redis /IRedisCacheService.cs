using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CioSystem.Services.Cache.Redis
{
    /// <summary>
    /// Redis 分散式快取服務介面
    /// </summary>
    public interface IRedisCacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
        Task<bool> ExistsAsync(string key);
        Task<long> IncrementAsync(string key, long value = 1);
        Task<long> DecrementAsync(string key, long value = 1);
        Task<TimeSpan?> GetExpirationAsync(string key);
        Task SetExpirationAsync(string key, TimeSpan expiration);
    }

    /// <summary>
    /// Redis 分散式快取服務實現
    /// </summary>
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;

        public RedisCacheService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisCacheService> logger)
        {
            _distributedCache = distributedCache;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
            _database = _connectionMultiplexer.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var cachedData = await _distributedCache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogDebug("Redis 快取未命中: {Key}", key);
                    return null;
                }

                _logger.LogDebug("Redis 快取命中: {Key}", key);
                return System.Text.Json.JsonSerializer.Deserialize<T>(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 快取取得失敗: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var serializedData = System.Text.Json.JsonSerializer.Serialize(value);
                var options = new DistributedCacheEntryOptions();

                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                }

                await _distributedCache.SetStringAsync(key, serializedData, options);
                _logger.LogDebug("Redis 快取設定成功: {Key}, 過期時間: {Expiration}", key, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 快取設定失敗: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
                _logger.LogDebug("Redis 快取移除成功: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 快取移除失敗: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern);

                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }

                _logger.LogDebug("Redis 模式快取移除成功: {Pattern}, 移除數量: {Count}", pattern, keys.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 模式快取移除失敗: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 快取存在檢查失敗: {Key}", key);
                return false;
            }
        }

        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            try
            {
                return await _database.StringIncrementAsync(key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 計數器增加失敗: {Key}", key);
                return 0;
            }
        }

        public async Task<long> DecrementAsync(string key, long value = 1)
        {
            try
            {
                return await _database.StringDecrementAsync(key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 計數器減少失敗: {Key}", key);
                return 0;
            }
        }

        public async Task<TimeSpan?> GetExpirationAsync(string key)
        {
            try
            {
                var ttl = await _database.KeyTimeToLiveAsync(key);
                return ttl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 快取過期時間取得失敗: {Key}", key);
                return null;
            }
        }

        public async Task SetExpirationAsync(string key, TimeSpan expiration)
        {
            try
            {
                await _database.KeyExpireAsync(key, expiration);
                _logger.LogDebug("Redis 快取過期時間設定成功: {Key}, 過期時間: {Expiration}", key, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 快取過期時間設定失敗: {Key}", key);
            }
        }
    }
}