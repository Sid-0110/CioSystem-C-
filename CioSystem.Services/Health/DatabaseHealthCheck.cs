using CioSystem.Core;
using CioSystem.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace CioSystem.Services.Health
{
    /// <summary>
    /// 資料庫健康檢查
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(IUnitOfWork unitOfWork, ILogger<DatabaseHealthCheck> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // 測試資料庫連線
                await _unitOfWork.GetRepository<Product>().CountAsync(p => !p.IsDeleted);

                var data = new Dictionary<string, object>
                {
                    { "database", "connected" },
                    { "timestamp", DateTime.UtcNow }
                };

                return HealthCheckResult.Healthy("資料庫連線正常", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫健康檢查失敗");
                return HealthCheckResult.Unhealthy("資料庫連線失敗", ex, new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "timestamp", DateTime.UtcNow }
                });
            }
        }
    }

    /// <summary>
    /// 快取健康檢查
    /// </summary>
    public class CacheHealthCheck : IHealthCheck
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheHealthCheck> _logger;

        public CacheHealthCheck(IMemoryCache cache, ILogger<CacheHealthCheck> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // 測試快取功能
                var testKey = "health_check_test";
                var testValue = DateTime.UtcNow.ToString();

                var cacheEntryOptions = new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
                    Size = 1
                };
                _cache.Set(testKey, testValue, cacheEntryOptions);
                var retrievedValue = _cache.Get<string>(testKey);

                if (retrievedValue == testValue)
                {
                    _cache.Remove(testKey);
                    return Task.FromResult(HealthCheckResult.Healthy("快取功能正常"));
                }
                else
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("快取功能異常"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快取健康檢查失敗");
                return Task.FromResult(HealthCheckResult.Unhealthy("快取功能失敗", ex));
            }
        }
    }

    /// <summary>
    /// 系統資源健康檢查
    /// </summary>
    public class SystemResourcesHealthCheck : IHealthCheck
    {
        private readonly ILogger<SystemResourcesHealthCheck> _logger;

        public SystemResourcesHealthCheck(ILogger<SystemResourcesHealthCheck> logger)
        {
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB
                var cpuUsage = process.TotalProcessorTime.TotalMilliseconds;

                var data = new Dictionary<string, object>
                {
                    { "memory_usage_mb", memoryUsage },
                    { "cpu_time_ms", cpuUsage },
                    { "timestamp", DateTime.UtcNow }
                };

                // 檢查記憶體使用量（警告：超過 500MB）
                if (memoryUsage > 500)
                {
                    return Task.FromResult(HealthCheckResult.Degraded("記憶體使用量過高", data: data));
                }

                return Task.FromResult(HealthCheckResult.Healthy("系統資源正常", data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "系統資源健康檢查失敗");
                return Task.FromResult(HealthCheckResult.Unhealthy("系統資源檢查失敗", ex));
            }
        }
    }
}