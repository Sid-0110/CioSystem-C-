using CioSystem.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CioSystem.Services.Background
{
    /// <summary>
    /// 快取清理背景服務
    /// 定期清理過期的快取項目
    /// </summary>
    public class CacheCleanupService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30);

        public CacheCleanupService(IServiceProvider serviceProvider, ILogger<CacheCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("快取清理服務已啟動");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "快取清理過程中發生錯誤");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("快取清理服務已停止");
        }

        private async Task PerformCleanupAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();
            var loggingService = scope.ServiceProvider.GetService<ILoggingService>();

            if (cacheService == null || loggingService == null)
            {
                _logger.LogWarning("無法取得快取服務或日誌服務");
                return;
            }

            try
            {
                // 清理過期日誌
                var cutoffTime = DateTime.UtcNow.AddHours(-24);
                var cleanedLogs = await loggingService.CleanupAsync(cutoffTime);

                // 取得快取統計資訊
                var cacheStats = await cacheService.GetStatisticsAsync();

                _logger.LogInformation("快取清理完成 - 清理日誌: {CleanedLogs}, 快取項目: {CacheItems}, 命中率: {HitRatio:P2}",
                    cleanedLogs, cacheStats.TotalItems, cacheStats.HitRatio);

                // 記錄性能指標
                await loggingService.LogPerformanceAsync("CacheCleanup", TimeSpan.FromMilliseconds(100),
                    new Dictionary<string, object>
                    {
                        ["CleanedLogs"] = cleanedLogs,
                        ["CacheItems"] = cacheStats.TotalItems,
                        ["HitRatio"] = cacheStats.HitRatio,
                        ["MemoryUsage"] = cacheStats.MemoryUsage
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行快取清理時發生錯誤");
            }
        }
    }
}