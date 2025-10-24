using CioSystem.Data.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CioSystem.Services.Background
{
    /// <summary>
    /// 資料庫優化背景服務
    /// 定期執行資料庫優化和清理任務
    /// </summary>
    public class DatabaseOptimizationBackgroundService : BackgroundService
    {
        private readonly ILogger<DatabaseOptimizationBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _optimizationInterval = TimeSpan.FromHours(6); // 每6小時優化一次
        private readonly TimeSpan _vacuumInterval = TimeSpan.FromDays(1); // 每天清理一次

        public DatabaseOptimizationBackgroundService(
            ILogger<DatabaseOptimizationBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("資料庫優化背景服務已啟動");

            var lastOptimization = DateTime.UtcNow;
            var lastVacuum = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    // 檢查是否需要執行資料庫優化
                    if (now - lastOptimization >= _optimizationInterval)
                    {
                        await PerformDatabaseOptimization();
                        lastOptimization = now;
                    }

                    // 檢查是否需要執行資料庫清理
                    if (now - lastVacuum >= _vacuumInterval)
                    {
                        await PerformDatabaseVacuum();
                        lastVacuum = now;
                    }

                    // 每分鐘檢查一次
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "資料庫優化背景服務執行時發生錯誤");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("資料庫優化背景服務已停止");
        }

        /// <summary>
        /// 執行資料庫優化
        /// </summary>
        private async Task PerformDatabaseOptimization()
        {
            try
            {
                _logger.LogInformation("開始執行資料庫優化");

                using var scope = _serviceProvider.CreateScope();
                var performanceService = scope.ServiceProvider.GetRequiredService<IDatabasePerformanceService>();

                await performanceService.OptimizeDatabaseAsync();

                _logger.LogInformation("資料庫優化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫優化執行時發生錯誤");
            }
        }

        /// <summary>
        /// 執行資料庫清理
        /// </summary>
        private async Task PerformDatabaseVacuum()
        {
            try
            {
                _logger.LogInformation("開始執行資料庫清理");

                using var scope = _serviceProvider.CreateScope();
                var performanceService = scope.ServiceProvider.GetRequiredService<IDatabasePerformanceService>();

                await performanceService.VacuumDatabaseAsync();

                _logger.LogInformation("資料庫清理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫清理執行時發生錯誤");
            }
        }
    }
}