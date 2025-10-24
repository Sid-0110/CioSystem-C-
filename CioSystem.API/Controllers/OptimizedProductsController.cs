using Microsoft.AspNetCore.Mvc;
using CioSystem.API.Services;
using CioSystem.Models;
using CioSystem.Services;
using ValidationResult = CioSystem.Services.ValidationResult;

namespace CioSystem.API.Controllers
{
    /// <summary>
    /// 優化的產品 API 控制器
    /// 使用快取、效能監控和版本控制
    /// </summary>
    [ApiController]
    [Route("api/v2/[controller]")]
    public class OptimizedProductsController : ControllerBase
    {
        private readonly CioSystem.Services.IProductService _productService;
        private readonly IApiCacheService _cacheService;
        private readonly IApiPerformanceService _performanceService;
        private readonly ILogger<OptimizedProductsController> _logger;

        public OptimizedProductsController(
            CioSystem.Services.IProductService productService,
            IApiCacheService cacheService,
            IApiPerformanceService performanceService,
            ILogger<OptimizedProductsController> logger)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 取得所有產品（優化版本）
        /// </summary>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="search">搜尋關鍵字</param>
        /// <returns>產品列表</returns>
        [HttpGet]
        public async Task<ActionResult<object>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey("products", "getall", new Dictionary<string, object>
                {
                    ["page"] = page,
                    ["pageSize"] = pageSize,
                    ["search"] = search ?? ""
                });

                // 嘗試從快取取得
                var cachedResult = await _cacheService.GetCachedResponseAsync<object>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("返回快取的產品列表");
                    return Ok(cachedResult);
                }

                // 快取未命中，從資料庫取得
                var products = await _productService.GetAllProductsAsync();

                // 應用搜尋過濾
                if (!string.IsNullOrEmpty(search))
                {
                    products = products.Where(p =>
                        p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        p.SKU.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                    );
                }

                // 計算分頁
                var totalCount = products.Count();
                var pagedProducts = products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new
                {
                    Data = pagedProducts,
                    Pagination = new
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    CachedAt = DateTime.UtcNow
                };

                // 快取結果（5分鐘）
                await _cacheService.SetCachedResponseAsync(cacheKey, result, TimeSpan.FromMinutes(5));

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得產品列表時發生錯誤");
                return StatusCode(500, "取得產品列表時發生內部錯誤");
            }
        }

        /// <summary>
        /// 根據 ID 取得產品（優化版本）
        /// </summary>
        /// <param name="id">產品 ID</param>
        /// <returns>產品</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey("products", "getbyid", new Dictionary<string, object>
                {
                    ["id"] = id
                });

                // 嘗試從快取取得
                var cachedProduct = await _cacheService.GetCachedResponseAsync<Product>(cacheKey);
                if (cachedProduct != null)
                {
                    _logger.LogDebug("返回快取的產品: ID={ProductId}", id);
                    return Ok(cachedProduct);
                }

                // 快取未命中，從資料庫取得
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound($"找不到 ID 為 {id} 的產品");
                }

                // 快取結果（10分鐘）
                await _cacheService.SetCachedResponseAsync(cacheKey, product, TimeSpan.FromMinutes(10));

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得產品時發生錯誤: ID={ProductId}", id);
                return StatusCode(500, "取得產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 根據 SKU 取得產品（優化版本）
        /// </summary>
        /// <param name="sku">產品 SKU</param>
        /// <returns>產品</returns>
        [HttpGet("sku/{sku}")]
        public async Task<ActionResult<Product>> GetProductBySku(string sku)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey("products", "getbysku", new Dictionary<string, object>
                {
                    ["sku"] = sku
                });

                // 嘗試從快取取得
                var cachedProduct = await _cacheService.GetCachedResponseAsync<Product>(cacheKey);
                if (cachedProduct != null)
                {
                    _logger.LogDebug("返回快取的產品: SKU={Sku}", sku);
                    return Ok(cachedProduct);
                }

                // 快取未命中，從資料庫取得
                var products = await _productService.GetAllProductsAsync();
                var product = products.FirstOrDefault(p => p.SKU == sku);

                if (product == null)
                {
                    return NotFound($"找不到 SKU 為 {sku} 的產品");
                }

                // 快取結果（10分鐘）
                await _cacheService.SetCachedResponseAsync(cacheKey, product, TimeSpan.FromMinutes(10));

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據 SKU 取得產品時發生錯誤: SKU={Sku}", sku);
                return StatusCode(500, "取得產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 建立新產品（優化版本）
        /// </summary>
        /// <param name="product">產品資料</param>
        /// <returns>建立的產品</returns>
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
        {
            try
            {
                // 驗證產品資料
                var validationResult = await _productService.ValidateProductAsync(product);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult);
                }

                // 建立產品
                var createdProduct = await _productService.CreateProductAsync(product);

                // 清除相關快取
                await ClearProductRelatedCache();

                _logger.LogInformation("成功建立產品: ID={ProductId}, SKU={Sku}", createdProduct.Id, createdProduct.SKU);
                return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立產品時發生錯誤");
                return StatusCode(500, "建立產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 更新產品（優化版本）
        /// </summary>
        /// <param name="id">產品 ID</param>
        /// <param name="product">產品資料</param>
        /// <returns>更新結果</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateProduct(int id, [FromBody] Product product)
        {
            try
            {
                if (id != product.Id)
                {
                    return BadRequest("ID 不匹配");
                }

                // 驗證產品資料
                var validationResult = await _productService.ValidateProductAsync(product);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult);
                }

                // 更新產品
                await _productService.UpdateProductAsync(product);

                // 清除相關快取
                await ClearProductRelatedCache();

                _logger.LogInformation("成功更新產品: ID={ProductId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新產品時發生錯誤: ID={ProductId}", id);
                return StatusCode(500, "更新產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 刪除產品（優化版本）
        /// </summary>
        /// <param name="id">產品 ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound($"找不到 ID 為 {id} 的產品");
                }

                await _productService.DeleteProductAsync(id);

                // 清除相關快取
                await ClearProductRelatedCache();

                _logger.LogInformation("成功刪除產品: ID={ProductId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除產品時發生錯誤: ID={ProductId}", id);
                return StatusCode(500, "刪除產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 清除產品相關快取
        /// </summary>
        private async Task ClearProductRelatedCache()
        {
            try
            {
                // 清除產品列表快取
                var listCacheKey = _cacheService.GenerateCacheKey("products", "getall", new Dictionary<string, object>());
                await _cacheService.RemoveCachedResponseAsync(listCacheKey);

                _logger.LogDebug("已清除產品相關快取");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清除產品快取時發生錯誤");
            }
        }
    }
}