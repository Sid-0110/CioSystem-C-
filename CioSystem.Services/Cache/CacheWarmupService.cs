using CioSystem.Core;
using CioSystem.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 快取預熱服務實現
    /// 負責在系統啟動時預熱關鍵快取資料
    /// </summary>
    public class CacheWarmupService : ICacheWarmupService
    {
        private readonly IMultiLayerCacheService _multiLayerCache;
        private readonly IProductCacheService _productCacheService;
        private readonly IStatisticsCacheService _statisticsCacheService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CacheWarmupService> _logger;
        private WarmupStatus _status;

        public CacheWarmupService(
            IMultiLayerCacheService multiLayerCache,
            IProductCacheService productCacheService,
            IStatisticsCacheService statisticsCacheService,
            IUnitOfWork unitOfWork,
            ILogger<CacheWarmupService> logger)
        {
            _multiLayerCache = multiLayerCache;
            _productCacheService = productCacheService;
            _statisticsCacheService = statisticsCacheService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _status = new WarmupStatus { StartTime = DateTime.UtcNow };
        }

        /// <summary>
        /// 預熱所有關鍵快取
        /// </summary>
        public async Task WarmupAllAsync()
        {
            try
            {
                _logger.LogInformation("開始預熱所有快取");
                _status.StartTime = DateTime.UtcNow;
                _status.IsCompleted = false;
                _status.TotalItems = 5; // 產品、庫存、統計、配置、用戶資料
                _status.CompletedItems = 0;
                _status.FailedItems = 0;

                var tasks = new List<Task>
                {
                    WarmupProductsAsync(),
                    WarmupInventoryAsync(),
                    WarmupStatisticsAsync(),
                    WarmupConfigurationAsync(),
                    WarmupUserDataAsync()
                };

                await Task.WhenAll(tasks);

                _status.EndTime = DateTime.UtcNow;
                _status.IsCompleted = true;

                _logger.LogInformation("所有快取預熱完成，耗時: {Duration}", _status.Duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "預熱所有快取失敗");
                _status.LastError = ex.Message;
                throw;
            }
        }

        /// <summary>
        /// 預熱產品快取
        /// </summary>
        public async Task WarmupProductsAsync()
        {
            try
            {
                _status.CurrentOperation = "預熱產品快取";
                _logger.LogInformation("開始預熱產品快取");

                // 預熱所有產品
                var products = await _productCacheService.GetAllProductsAsync();
                if (products != null)
                {
                    await _multiLayerCache.SetAsync("all_products", products, TimeSpan.FromHours(1), CacheLayer.L1);
                    await _multiLayerCache.SetTagAsync("all_products", "products");
                }

                // 預熱活躍產品
                var activeProducts = products?.Where(p => p.Status == ProductStatus.Active).ToList();
                if (activeProducts != null)
                {
                    await _multiLayerCache.SetAsync("active_products", activeProducts, TimeSpan.FromHours(2), CacheLayer.L1);
                    await _multiLayerCache.SetTagAsync("active_products", "products");
                }

                // 預熱產品分類
                var categories = products?.Select(p => p.Category).Distinct().ToList();
                if (categories != null)
                {
                    await _multiLayerCache.SetAsync("product_categories", categories, TimeSpan.FromHours(4), CacheLayer.L2);
                    await _multiLayerCache.SetTagAsync("product_categories", "products");
                }

                _status.CompletedItems++;
                _logger.LogInformation("產品快取預熱完成，產品數量: {Count}", products?.Count() ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "預熱產品快取失敗");
                _status.FailedItems++;
                _status.LastError = ex.Message;
            }
        }

        /// <summary>
        /// 預熱庫存快取
        /// </summary>
        public async Task WarmupInventoryAsync()
        {
            try
            {
                _status.CurrentOperation = "預熱庫存快取";
                _logger.LogInformation("開始預熱庫存快取");

                // 預熱庫存統計
                var inventoryStats = await _statisticsCacheService.GetInventoryStatsAsync();
                await _multiLayerCache.SetAsync("inventory_stats", inventoryStats, TimeSpan.FromMinutes(30), CacheLayer.L1);
                await _multiLayerCache.SetTagAsync("inventory_stats", "inventory");

                // 預熱低庫存警告
                var lowStockItems = await _unitOfWork.GetRepository<Inventory>()
                    .FindAsync(i => i.Quantity <= i.SafetyStock && !i.IsDeleted);
                if (lowStockItems != null)
                {
                    await _multiLayerCache.SetAsync("low_stock_items", lowStockItems, TimeSpan.FromMinutes(15), CacheLayer.L1);
                    await _multiLayerCache.SetTagAsync("low_stock_items", "inventory");
                }

                _status.CompletedItems++;
                _logger.LogInformation("庫存快取預熱完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "預熱庫存快取失敗");
                _status.FailedItems++;
                _status.LastError = ex.Message;
            }
        }

        /// <summary>
        /// 預熱統計資料快取
        /// </summary>
        public async Task WarmupStatisticsAsync()
        {
            try
            {
                _status.CurrentOperation = "預熱統計資料快取";
                _logger.LogInformation("開始預熱統計資料快取");

                // 預熱儀表板統計
                var dashboardStats = await _statisticsCacheService.GetDashboardStatsAsync();
                await _multiLayerCache.SetAsync("dashboard_stats", dashboardStats, TimeSpan.FromMinutes(10), CacheLayer.L1);
                await _multiLayerCache.SetTagAsync("dashboard_stats", "statistics");

                // 預熱銷售統計
                var salesStats = await _statisticsCacheService.GetSalesStatsAsync();
                await _multiLayerCache.SetAsync("sales_stats", salesStats, TimeSpan.FromMinutes(15), CacheLayer.L1);
                await _multiLayerCache.SetTagAsync("sales_stats", "statistics");

                // 預熱進貨統計
                var purchasesStats = await _statisticsCacheService.GetPurchasesStatisticsAsync();
                await _multiLayerCache.SetAsync("purchases_stats", purchasesStats, TimeSpan.FromMinutes(15), CacheLayer.L1);
                await _multiLayerCache.SetTagAsync("purchases_stats", "statistics");

                _status.CompletedItems++;
                _logger.LogInformation("統計資料快取預熱完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "預熱統計資料快取失敗");
                _status.FailedItems++;
                _status.LastError = ex.Message;
            }
        }

        /// <summary>
        /// 預熱配置快取
        /// </summary>
        public async Task WarmupConfigurationAsync()
        {
            try
            {
                _status.CurrentOperation = "預熱配置快取";
                _logger.LogInformation("開始預熱配置快取");

                // 預熱系統配置
                var systemConfig = new Dictionary<string, object>
                {
                    { "cache_enabled", true },
                    { "max_page_size", 100 },
                    { "default_page_size", 25 },
                    { "session_timeout", 30 },
                    { "auto_backup_enabled", true }
                };

                await _multiLayerCache.SetAsync("system_config", systemConfig, TimeSpan.FromHours(24), CacheLayer.L2);
                await _multiLayerCache.SetTagAsync("system_config", "configuration");

                // 預熱快取配置
                var cacheConfig = new CacheConfiguration
                {
                    SizeLimit = 1000,
                    DefaultExpirationMinutes = 30,
                    EnableStatistics = true
                };

                await _multiLayerCache.SetAsync("cache_config", cacheConfig, TimeSpan.FromHours(12), CacheLayer.L2);
                await _multiLayerCache.SetTagAsync("cache_config", "configuration");

                _status.CompletedItems++;
                _logger.LogInformation("配置快取預熱完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "預熱配置快取失敗");
                _status.FailedItems++;
                _status.LastError = ex.Message;
            }
        }

        /// <summary>
        /// 預熱用戶相關快取
        /// </summary>
        public async Task WarmupUserDataAsync()
        {
            try
            {
                _status.CurrentOperation = "預熱用戶相關快取";
                _logger.LogInformation("開始預熱用戶相關快取");

                // 預熱用戶權限
                var userPermissions = new Dictionary<string, List<string>>
                {
                    { "admin", new List<string> { "read", "write", "delete", "manage" } },
                    { "user", new List<string> { "read", "write" } },
                    { "viewer", new List<string> { "read" } }
                };

                await _multiLayerCache.SetAsync("user_permissions", userPermissions, TimeSpan.FromHours(8), CacheLayer.L2);
                await _multiLayerCache.SetTagAsync("user_permissions", "user_data");

                // 預熱用戶偏好設定
                var userPreferences = new Dictionary<string, object>
                {
                    { "theme", "light" },
                    { "language", "zh-TW" },
                    { "timezone", "Asia/Taipei" },
                    { "date_format", "yyyy-MM-dd" }
                };

                await _multiLayerCache.SetAsync("user_preferences", userPreferences, TimeSpan.FromHours(4), CacheLayer.L1);
                await _multiLayerCache.SetTagAsync("user_preferences", "user_data");

                _status.CompletedItems++;
                _logger.LogInformation("用戶相關快取預熱完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "預熱用戶相關快取失敗");
                _status.FailedItems++;
                _status.LastError = ex.Message;
            }
        }

        /// <summary>
        /// 檢查預熱狀態
        /// </summary>
        public async Task<WarmupStatus> GetWarmupStatusAsync()
        {
            return await Task.FromResult(_status);
        }

        /// <summary>
        /// 重新預熱指定類型的快取
        /// </summary>
        public async Task RewarmupAsync(string cacheType)
        {
            try
            {
                _logger.LogInformation("重新預熱快取類型: {CacheType}", cacheType);

                switch (cacheType.ToLower())
                {
                    case "products":
                        await WarmupProductsAsync();
                        break;
                    case "inventory":
                        await WarmupInventoryAsync();
                        break;
                    case "statistics":
                        await WarmupStatisticsAsync();
                        break;
                    case "configuration":
                        await WarmupConfigurationAsync();
                        break;
                    case "user_data":
                        await WarmupUserDataAsync();
                        break;
                    default:
                        _logger.LogWarning("未知的快取類型: {CacheType}", cacheType);
                        break;
                }

                _logger.LogInformation("重新預熱完成: {CacheType}", cacheType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新預熱失敗: {CacheType}", cacheType);
                throw;
            }
        }
    }
}