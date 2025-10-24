using CioSystem.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 產品快取服務介面
    /// </summary>
    public interface IProductCacheService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductBySKUAsync(string sku);
        void InvalidateCache();
        void InvalidateProduct(int productId);
    }

    /// <summary>
    /// 產品快取服務實現
    /// </summary>
    public class ProductCacheService : IProductCacheService
    {
        private readonly IProductService _productService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProductCacheService> _logger;

        private const string ALL_PRODUCTS_KEY = "all_products";
        private const string PRODUCT_BY_ID_KEY = "product_{0}";
        private const string PRODUCT_BY_SKU_KEY = "product_sku_{0}";
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);

        public ProductCacheService(
            IProductService productService,
            IMemoryCache cache,
            ILogger<ProductCacheService> logger)
        {
            _productService = productService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _cache.GetOrCreateAsync(ALL_PRODUCTS_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
                _logger.LogInformation("產品快取未命中，從資料庫載入所有產品");
                return await _productService.GetAllProductsAsync();
            });
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            var cacheKey = string.Format(PRODUCT_BY_ID_KEY, id);
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
                _logger.LogInformation("產品快取未命中，從資料庫載入產品: {ProductId}", id);
                return await _productService.GetProductByIdAsync(id);
            });
        }

        public async Task<Product?> GetProductBySKUAsync(string sku)
        {
            var cacheKey = string.Format(PRODUCT_BY_SKU_KEY, sku);
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
                _logger.LogInformation("產品快取未命中，從資料庫載入產品: {SKU}", sku);
                // 暫時使用 GetAllProductsAsync 然後篩選，直到 GetProductBySKUAsync 實作
                var products = await _productService.GetAllProductsAsync();
                return products.FirstOrDefault(p => p.SKU == sku);
            });
        }

        public void InvalidateCache()
        {
            _cache.Remove(ALL_PRODUCTS_KEY);
            _logger.LogInformation("產品快取已清除");
        }

        public void InvalidateProduct(int productId)
        {
            var cacheKey = string.Format(PRODUCT_BY_ID_KEY, productId);
            _cache.Remove(cacheKey);
            _cache.Remove(ALL_PRODUCTS_KEY);
            _logger.LogInformation("產品快取已清除: {ProductId}", productId);
        }
    }
}