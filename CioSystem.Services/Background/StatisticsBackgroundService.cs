using CioSystem.Core;
using CioSystem.Services.Cache;
using CioSystem.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CioSystem.Services.Background
{
    /// <summary>
    /// 統計資料背景服務
    /// 定期重新計算統計資料並更新快取
    /// </summary>
    public class StatisticsBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StatisticsBackgroundService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(5);

        public StatisticsBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<StatisticsBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("統計背景服務已啟動");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateStatisticsAsync();
                    await Task.Delay(_updateInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("統計背景服務已停止");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "統計背景服務執行時發生錯誤");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // 錯誤時等待1分鐘再重試
                }
            }
        }

        private async Task UpdateStatisticsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var statisticsCacheService = scope.ServiceProvider.GetRequiredService<IStatisticsCacheService>();

            try
            {
                _logger.LogInformation("開始更新統計資料");

                // 並行更新所有統計資料
                var dashboardTask = statisticsCacheService.GetDashboardStatsAsync();
                var inventoryTask = statisticsCacheService.GetInventoryStatsAsync();
                var salesTask = statisticsCacheService.GetSalesStatsAsync();

                await Task.WhenAll(dashboardTask, inventoryTask, salesTask);

                _logger.LogInformation("統計資料更新完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新統計資料時發生錯誤");
            }
        }
    }

    /// <summary>
    /// 快取清理背景服務
    /// 定期清理過期快取項目
    /// </summary>
    public class CacheCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheCleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

        public CacheCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<CacheCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("快取清理背景服務已啟動");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupCacheAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("快取清理背景服務已停止");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "快取清理背景服務執行時發生錯誤");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task CleanupCacheAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var statisticsCacheService = scope.ServiceProvider.GetRequiredService<IStatisticsCacheService>();

            try
            {
                _logger.LogInformation("開始清理過期快取");

                // 清除統計快取，強制重新計算
                statisticsCacheService.InvalidateStatsCache();

                _logger.LogInformation("快取清理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理快取時發生錯誤");
            }
        }
    }

    /// <summary>
    /// 資料庫維護背景服務
    /// 定期執行資料庫維護任務
    /// </summary>
    public class DatabaseMaintenanceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseMaintenanceBackgroundService> _logger;
        private readonly TimeSpan _maintenanceInterval = TimeSpan.FromDays(1);

        public DatabaseMaintenanceBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DatabaseMaintenanceBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("資料庫維護背景服務已啟動");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformMaintenanceAsync();
                    await Task.Delay(_maintenanceInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("資料庫維護背景服務已停止");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "資料庫維護背景服務執行時發生錯誤");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task PerformMaintenanceAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                _logger.LogInformation("開始執行資料庫維護");

                // 清理軟刪除的記錄（超過30天）
                var cutoffDate = DateTime.Now.AddDays(-30);

                var deletedProducts = await unitOfWork.GetRepository<Product>().FindAsync(p => p.IsDeleted && p.UpdatedAt < cutoffDate);
                var deletedInventory = await unitOfWork.GetRepository<Inventory>().FindAsync(i => i.IsDeleted && i.UpdatedAt < cutoffDate);
                var deletedSales = await unitOfWork.GetRepository<Sale>().FindAsync(s => s.IsDeleted && s.UpdatedAt < cutoffDate);
                var deletedPurchases = await unitOfWork.GetRepository<Purchase>().FindAsync(p => p.IsDeleted && p.UpdatedAt < cutoffDate);

                // 這裡可以實作實際的清理邏輯
                _logger.LogInformation("找到 {DeletedProducts} 個已刪除產品, {DeletedInventory} 個已刪除庫存項目, {DeletedSales} 個已刪除銷售記錄, {DeletedPurchases} 個已刪除進貨記錄",
                    deletedProducts.Count(), deletedInventory.Count(), deletedSales.Count(), deletedPurchases.Count());

                _logger.LogInformation("資料庫維護完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行資料庫維護時發生錯誤");
            }
        }
    }
}