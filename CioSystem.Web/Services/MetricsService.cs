using CioSystem.Services;
using CioSystem.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CioSystem.Web.Services
{
    public interface IMetricsService
    {
        Task<MetricsSummary> GetSummaryAsync(DateTime? from = null, DateTime? to = null);
        Task<IReadOnlyList<MetricsPoint>> GetSalesTrendAsync(DateTime? from = null, DateTime? to = null, string granularity = "day");
        Task<IReadOnlyList<InventoryStatusItem>> GetInventoryStatusAsync();
        Task<IReadOnlyList<InventoryTopValueItem>> GetInventoryTopValueAsync(int top = 10);
        Task<IReadOnlyList<ProductSalesItem>> GetProductSalesAsync(DateTime? from = null, DateTime? to = null, int top = 50);
    }

    public record MetricsSummary(decimal TotalRevenue, decimal TotalCost, decimal GrossProfit, int LowStockCount, decimal InventoryValue);
    public record MetricsPoint(DateTime Date, int Quantity, decimal Amount);
    public record InventoryStatusItem(string Status, int Count, decimal TotalValue);
    public record InventoryTopValueItem(int ProductId, string ProductName, string ProductSKU, int Quantity, decimal UnitPrice, decimal TotalValue, string Status);
    public record ProductSalesItem(int ProductId, string ProductName, string ProductSKU, int TotalQuantity, decimal TotalRevenue, decimal AveragePrice, int SalesCount);

    public sealed class MetricsService : IMetricsService
    {
        private readonly ISalesService _sales;
        private readonly IPurchasesService _purchases;
        private readonly IProductService _products;
        private readonly IInventoryService _inventory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MetricsService> _logger;
        private const int CacheExpirationMinutes = 5;

        public MetricsService(ISalesService sales, IPurchasesService purchases, IProductService products, IInventoryService inventory, IMemoryCache cache, ILogger<MetricsService> logger)
        {
            _sales = sales;
            _purchases = purchases;
            _products = products;
            _inventory = inventory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<MetricsSummary> GetSummaryAsync(DateTime? from = null, DateTime? to = null)
        {
            var cacheKey = $"summary_{from?.ToString("yyyyMMdd")}_{to?.ToString("yyyyMMdd")}";
            if (_cache.TryGetValue(cacheKey, out MetricsSummary? cachedSummary))
            {
                return cachedSummary!;
            }

            var sales = await _sales.GetAllSalesAsync();
            var purchases = await _purchases.GetAllPurchasesAsync();
            var products = await _products.GetAllProductsAsync();
            var inventory = await _inventory.GetAllInventoryAsync();

            if (from.HasValue) { sales = sales.Where(s => s.CreatedAt >= from.Value); purchases = purchases.Where(p => p.CreatedAt >= from.Value); }
            if (to.HasValue) { sales = sales.Where(s => s.CreatedAt <= to.Value); purchases = purchases.Where(p => p.CreatedAt <= to.Value); }

            var totalRevenue = sales.Sum(s => s.UnitPrice * s.Quantity);
            var totalCost = purchases.Sum(p => p.UnitPrice * p.Quantity);
            var grossProfit = totalRevenue - totalCost;
            var lowStockCount = inventory.Count(i => i.SafetyStock > 0 && i.Quantity <= i.SafetyStock);
            var inventoryValue = inventory.Sum(i => i.Quantity * (products.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0));

            var summary = new MetricsSummary(totalRevenue, totalCost, grossProfit, lowStockCount, inventoryValue);

            // 使用具體 Size 的快取選項，避免 SizeLimit 例外
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes))
                .SetSize(1);
            _cache.Set(cacheKey, summary, options);
            return summary;
        }

        public async Task<IReadOnlyList<MetricsPoint>> GetSalesTrendAsync(DateTime? from = null, DateTime? to = null, string granularity = "day")
        {
            var sales = await _sales.GetAllSalesAsync();
            if (from.HasValue) sales = sales.Where(s => s.CreatedAt >= from.Value);
            if (to.HasValue) sales = sales.Where(s => s.CreatedAt <= to.Value);
            var groups = sales.GroupBy(s => granularity == "month" ? new DateTime(s.CreatedAt.Year, s.CreatedAt.Month, 1) : s.CreatedAt.Date)
                .Select(g => new MetricsPoint(g.Key, g.Sum(x => x.Quantity), g.Sum(x => x.Quantity * x.UnitPrice)))
                .OrderBy(x => x.Date)
                .ToList();
            return groups;
        }

        public async Task<IReadOnlyList<InventoryStatusItem>> GetInventoryStatusAsync()
        {
            try
            {
                const string cacheKey = "inventory_status";
                if (_cache.TryGetValue(cacheKey, out IReadOnlyList<InventoryStatusItem>? cachedStatus))
                {
                    return cachedStatus!;
                }

                var products = await _products.GetAllProductsAsync();
                var inventory = await _inventory.GetAllInventoryAsync();

                // 空值保護
                if (products == null) products = new List<Product>();
                if (inventory == null) inventory = new List<Inventory>();

                var result = inventory
                    .GroupBy(i => i.Status.ToString())
                    .Select(g => new InventoryStatusItem(g.Key, g.Count(), g.Sum(i => i.Quantity * (products.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0))))
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // 使用具體 Size 的快取選項，避免 SizeLimit 例外
                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes))
                    .SetSize(Math.Max(1, result.Count));
                _cache.Set(cacheKey, result, options);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取庫存狀態時發生錯誤");
                return new List<InventoryStatusItem>();
            }
        }

        public async Task<IReadOnlyList<InventoryTopValueItem>> GetInventoryTopValueAsync(int top = 10)
        {
            try
            {
                var cacheKey = $"inventory_top_value_{top}";
                if (_cache.TryGetValue(cacheKey, out IReadOnlyList<InventoryTopValueItem>? cachedTopValue))
                {
                    return cachedTopValue!;
                }

                var products = await _products.GetAllProductsAsync();
                var inventory = await _inventory.GetAllInventoryAsync();

                // 空值保護
                if (products == null) products = new List<Product>();
                if (inventory == null) inventory = new List<Inventory>();

                var result = inventory
                    .Select(i => new InventoryTopValueItem(
                        i.ProductId,
                        products.FirstOrDefault(p => p.Id == i.ProductId)?.Name ?? "未知產品",
                        products.FirstOrDefault(p => p.Id == i.ProductId)?.SKU ?? "N/A",
                        i.Quantity,
                        products.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0,
                        i.Quantity * (products.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0),
                        i.Status.ToString()
                    ))
                    .OrderByDescending(x => x.TotalValue)
                    .Take(top)
                    .ToList();

                // 使用具體 Size 的快取選項，避免 SizeLimit 例外
                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes))
                    .SetSize(Math.Max(1, result.Count));
                _cache.Set(cacheKey, result, options);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取庫存價值排行時發生錯誤");
                return new List<InventoryTopValueItem>();
            }
        }

        public async Task<IReadOnlyList<ProductSalesItem>> GetProductSalesAsync(DateTime? from = null, DateTime? to = null, int top = 50)
        {
            var sales = await _sales.GetAllSalesAsync();
            var products = await _products.GetAllProductsAsync();
            if (from.HasValue) sales = sales.Where(s => s.CreatedAt >= from.Value);
            if (to.HasValue) sales = sales.Where(s => s.CreatedAt <= to.Value);

            var result = sales
                .GroupBy(s => s.ProductId)
                .Select(g => new ProductSalesItem(
                    g.Key,
                    products.FirstOrDefault(p => p.Id == g.Key)?.Name ?? "未知產品",
                    products.FirstOrDefault(p => p.Id == g.Key)?.SKU ?? "N/A",
                    g.Sum(x => x.Quantity),
                    g.Sum(x => x.Quantity * x.UnitPrice),
                    g.Any() ? g.Average(x => x.UnitPrice) : 0,
                    g.Count()
                ))
                .OrderByDescending(x => x.TotalRevenue)
                .Take(top)
                .ToList();

            return result;
        }
    }
}

