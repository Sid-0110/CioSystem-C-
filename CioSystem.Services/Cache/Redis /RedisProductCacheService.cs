using CioSystem.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;


namespace CioSystem.Services.Cache.Redis
{
    /// <summary>
    /// Redis 產品快取服務
    /// </summary>
    public class RedisProductCacheService : IProductCacheService
    {
        private readonly IProductService _productService;
        private readonly IRedisCacheService _redisCache;
        private readonly ILogger<RedisProductCacheService> _logger;

        private const string ALL_PRODUCTS_KEY = "all_products";
        private const string PRODUCT_BY_ID_KEY = "product_{0}";
        private const string PRODUCT_BY_SKU_KEY = "product_sku_{0}";
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);

        public RedisProductCacheService(
            IProductService productService,
            IRedisCacheService redisCache,
            ILogger<RedisProductCacheService> logger)
        {
            _productService = productService;
            _redisCache = redisCache;
            _logger = logger;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _redisCache.GetAsync<IEnumerable<Product>>(ALL_PRODUCTS_KEY) ??
                   await LoadAndCacheAllProductsAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            var cacheKey = string.Format(PRODUCT_BY_ID_KEY, id);
            return await _redisCache.GetAsync<Product>(cacheKey) ??
                   await LoadAndCacheProductByIdAsync(id);
        }

        public async Task<Product?> GetProductBySKUAsync(string sku)
        {
            var cacheKey = string.Format(PRODUCT_BY_SKU_KEY, sku);
            return await _redisCache.GetAsync<Product>(cacheKey) ??
                   await LoadAndCacheProductBySKUAsync(sku);
        }

        public async void InvalidateCache()
        {
            await _redisCache.RemoveByPatternAsync("product_*");
            await _redisCache.RemoveAsync(ALL_PRODUCTS_KEY);
            _logger.LogInformation("Redis 產品快取已清除");
        }

        public async void InvalidateProduct(int productId)
        {
            var cacheKey = string.Format(PRODUCT_BY_ID_KEY, productId);
            await _redisCache.RemoveAsync(cacheKey);
            await _redisCache.RemoveAsync(ALL_PRODUCTS_KEY);
            _logger.LogInformation("Redis 產品快取已清除: {ProductId}", productId);
        }

        private async Task<IEnumerable<Product>> LoadAndCacheAllProductsAsync()
        {
            _logger.LogInformation("Redis 產品快取未命中，從資料庫載入所有產品");
            var products = await _productService.GetAllProductsAsync();
            await _redisCache.SetAsync(ALL_PRODUCTS_KEY, products, CacheExpiry);
            return products;
        }

        private async Task<Product?> LoadAndCacheProductByIdAsync(int id)
        {
            _logger.LogInformation("Redis 產品快取未命中，從資料庫載入產品: {ProductId}", id);
            var product = await _productService.GetProductByIdAsync(id);
            if (product != null)
            {
                var cacheKey = string.Format(PRODUCT_BY_ID_KEY, id);
                await _redisCache.SetAsync(cacheKey, product, CacheExpiry);
            }
            return product;
        }

        private async Task<Product?> LoadAndCacheProductBySKUAsync(string sku)
        {
            _logger.LogInformation("Redis 產品快取未命中，從資料庫載入產品: {SKU}", sku);
            var products = await _productService.GetAllProductsAsync();
            var product = products.FirstOrDefault(p => p.SKU == sku);
            if (product != null)
            {
                var cacheKey = string.Format(PRODUCT_BY_SKU_KEY, sku);
                await _redisCache.SetAsync(cacheKey, product, CacheExpiry);
            }
            return product;
        }
    }
}