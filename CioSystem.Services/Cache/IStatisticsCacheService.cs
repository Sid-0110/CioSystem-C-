using CioSystem.Core;
using CioSystem.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 統計資料快取服務介面
    /// </summary>
    public interface IStatisticsCacheService
    {
        Task<DashboardStatistics> GetDashboardStatsAsync();
        Task<InventoryStatistics> GetInventoryStatsAsync();
        Task<SalesStatistics> GetSalesStatsAsync();
        Task<PurchasesStatistics> GetPurchasesStatisticsAsync();
        void InvalidateStatsCache();
    }

    /// <summary>
    /// 統計資料快取服務實現
    /// </summary>
    public class StatisticsCacheService : IStatisticsCacheService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILogger<StatisticsCacheService> _logger;

        private const string DASHBOARD_STATS_KEY = "dashboard_stats";
        private const string INVENTORY_STATS_KEY = "inventory_stats";
        private const string SALES_STATS_KEY = "sales_stats";
        private static readonly TimeSpan StatsCacheExpiry = TimeSpan.FromMinutes(10);

        public StatisticsCacheService(
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            ILogger<StatisticsCacheService> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
        }

        public async Task<DashboardStatistics> GetDashboardStatsAsync()
        {
            return await _cache.GetOrCreateAsync(DASHBOARD_STATS_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = StatsCacheExpiry;
                _logger.LogInformation("儀表板統計快取未命中，重新計算");

                // 使用並行查詢提升效能
                var productsTask = _unitOfWork.GetRepository<Product>().CountAsync(p => !p.IsDeleted);
                var inventoryTask = _unitOfWork.GetRepository<Inventory>().CountAsync(i => !i.IsDeleted);
                var salesTask = _unitOfWork.GetRepository<Sale>().CountAsync(s => !s.IsDeleted);
                var purchasesTask = _unitOfWork.GetRepository<Purchase>().CountAsync(p => !p.IsDeleted);

                await Task.WhenAll(productsTask, inventoryTask, salesTask, purchasesTask);

                // 計算財務統計
                var sales = await _unitOfWork.GetRepository<Sale>().FindAsync(s => !s.IsDeleted);
                var purchases = await _unitOfWork.GetRepository<Purchase>().FindAsync(p => !p.IsDeleted);

                var totalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice);
                var totalCost = purchases.Sum(p => p.Quantity * p.UnitPrice);

                return new DashboardStatistics
                {
                    TotalProducts = await productsTask,
                    TotalInventory = await inventoryTask,
                    TotalSales = await salesTask,
                    TotalPurchases = await purchasesTask,
                    TotalRevenue = totalRevenue,
                    TotalCost = totalCost,
                    GrossProfit = totalRevenue - totalCost
                };
            });
        }

        public async Task<InventoryStatistics> GetInventoryStatsAsync()
        {
            return await _cache.GetOrCreateAsync(INVENTORY_STATS_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = StatsCacheExpiry;
                _logger.LogInformation("庫存統計快取未命中，重新計算");

                var inventory = await _unitOfWork.GetRepository<Inventory>().FindAsync(i => !i.IsDeleted);
                var products = await _unitOfWork.GetRepository<Product>().FindAsync(p => !p.IsDeleted);
                var productMap = products.ToDictionary(p => p.Id, p => p.Price);

                var totalValue = inventory.Sum(i => i.Quantity * (productMap.ContainsKey(i.ProductId) ? productMap[i.ProductId] : 0));
                var averageQuantity = inventory.Any() ? (decimal)inventory.Average(i => i.Quantity) : 0;

                return new InventoryStatistics
                {
                    TotalItems = inventory.Count(),
                    TotalQuantity = inventory.Sum(i => i.Quantity),
                    TotalValue = totalValue,
                    AverageQuantity = averageQuantity,
                    AvailableItems = inventory.Count(i => i.Status == InventoryStatus.Normal || i.Status == InventoryStatus.Excess),
                    UnavailableItems = inventory.Count(i => i.Status == InventoryStatus.OutOfStock || i.Status == InventoryStatus.LowStock),
                    LowStockItems = inventory.Count(i => i.Status == InventoryStatus.LowStock),
                    ExpiredItems = 0,
                    ExpiringSoonItems = 0
                };
            });
        }

        public async Task<SalesStatistics> GetSalesStatsAsync()
        {
            return await _cache.GetOrCreateAsync(SALES_STATS_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = StatsCacheExpiry;
                _logger.LogInformation("銷售統計快取未命中，重新計算");

                var sales = await _unitOfWork.GetRepository<Sale>().FindAsync(s => !s.IsDeleted);

                return new SalesStatistics
                {
                    TotalSales = sales.Count(),
                    TotalQuantity = sales.Sum(s => s.Quantity),
                    TotalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice),
                    AverageOrderValue = sales.Any() ? sales.Average(s => s.Quantity * s.UnitPrice) : 0,
                    TopCustomer = sales.GroupBy(s => s.CustomerName)
                        .OrderByDescending(g => g.Sum(s => s.Quantity * s.UnitPrice))
                        .FirstOrDefault()?.Key ?? "無"
                };
            });
        }

        public async Task<PurchasesStatistics> GetPurchasesStatisticsAsync()
        {
            return await _cache.GetOrCreateAsync("purchases_stats", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = StatsCacheExpiry;
                _logger.LogInformation("進貨統計快取未命中，重新計算");

                var purchases = await _unitOfWork.GetRepository<Purchase>().GetAllAsync();

                return new PurchasesStatistics
                {
                    TotalPurchasesCount = purchases.Count(),
                    TotalQuantity = purchases.Sum(p => p.Quantity),
                    TotalCost = purchases.Sum(p => p.Quantity * p.UnitPrice),
                    AveragePurchaseValue = purchases.Any() ? purchases.Average(p => p.Quantity * p.UnitPrice) : 0,
                    TopSupplier = purchases.GroupBy(p => p.Supplier)
                        .OrderByDescending(g => g.Sum(p => p.Quantity * p.UnitPrice))
                        .FirstOrDefault()?.Key ?? "無"
                };
            });
        }

        public void InvalidateStatsCache()
        {
            _cache.Remove(DASHBOARD_STATS_KEY);
            _cache.Remove(INVENTORY_STATS_KEY);
            _cache.Remove(SALES_STATS_KEY);
            _logger.LogInformation("統計快取已清除");
        }
    }

    /// <summary>
    /// 儀表板統計資料
    /// </summary>
    public class DashboardStatistics
    {
        public int TotalProducts { get; set; }
        public int TotalInventory { get; set; }
        public int TotalSales { get; set; }
        public int TotalPurchases { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal GrossProfit { get; set; }
    }

    /// <summary>
    /// 銷售統計資料
    /// </summary>
    public class SalesStatistics
    {
        public int TotalSales { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public string TopCustomer { get; set; } = string.Empty;
    }

    /// <summary>
    /// 進貨統計資料
    /// </summary>
    public class PurchasesStatistics
    {
        public int TotalPurchasesCount { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AveragePurchaseValue { get; set; }
        public string TopSupplier { get; set; } = string.Empty;
    }
}