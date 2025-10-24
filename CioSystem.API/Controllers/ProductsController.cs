using Microsoft.AspNetCore.Mvc;
using CioSystem.API.Services;
using CioSystem.Models;
using CioSystem.Services;
using ValidationResult = CioSystem.Services.ValidationResult;

namespace CioSystem.API.Controllers
{
    /// <summary>
    /// 產品 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly CioSystem.Services.IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="productService">產品服務</param>
        /// <param name="logger">日誌記錄器</param>
        public ProductsController(CioSystem.Services.IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 取得所有產品
        /// </summary>
        /// <returns>產品列表</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得產品列表時發生錯誤");
                return StatusCode(500, "取得產品列表時發生內部錯誤");
            }
        }

        /// <summary>
        /// 根據 ID 取得產品
        /// </summary>
        /// <param name="id">產品 ID</param>
        /// <returns>產品</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound($"找不到 ID 為 {id} 的產品");
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得產品時發生錯誤: ID={ProductId}", id);
                return StatusCode(500, "取得產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 根據 SKU 取得產品
        /// </summary>
        /// <param name="sku">產品 SKU</param>
        /// <returns>產品</returns>
        [HttpGet("sku/{sku}")]
        public async Task<ActionResult<Product>> GetProductBySku(string sku)
        {
            try
            {
                var product = await _productService.GetProductBySkuAsync(sku);
                if (product == null)
                {
                    return NotFound($"找不到 SKU 為 {sku} 的產品");
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據 SKU 取得產品時發生錯誤: SKU={SKU}", sku);
                return StatusCode(500, "根據 SKU 取得產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 分頁查詢產品
        /// </summary>
        /// <param name="pageNumber">頁碼（從 1 開始）</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="category">產品類別（可選）</param>
        /// <param name="status">產品狀態（可選）</param>
        /// <returns>分頁結果</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<object>> GetProductsPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? category = null,
            [FromQuery] ProductStatus? status = null)
        {
            try
            {
                var (products, totalCount) = await _productService.GetProductsPagedAsync(
                    pageNumber, pageSize, searchTerm, category, status);

                var result = new
                {
                    Products = products,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分頁查詢產品時發生錯誤");
                return StatusCode(500, "分頁查詢產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 搜尋產品
        /// </summary>
        /// <param name="searchTerm">搜尋關鍵字</param>
        /// <returns>符合條件的產品列表</returns>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("搜尋關鍵字不能為空");
                }

                var products = await _productService.SearchProductsAsync(searchTerm);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋產品時發生錯誤: SearchTerm={SearchTerm}", searchTerm);
                return StatusCode(500, "搜尋產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 建立新產品
        /// </summary>
        /// <param name="product">要建立的產品</param>
        /// <returns>建立後的產品</returns>
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
        {
            try
            {
                if (product == null)
                {
                    return BadRequest("產品資料不能為空");
                }

                var createdProduct = await _productService.CreateProductAsync(product);
                return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "建立產品時發生業務邏輯錯誤");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立產品時發生錯誤");
                return StatusCode(500, "建立產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 更新產品
        /// </summary>
        /// <param name="id">產品 ID</param>
        /// <param name="product">要更新的產品資料</param>
        /// <returns>更新後的產品</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(int id, [FromBody] Product product)
        {
            try
            {
                if (product == null)
                {
                    return BadRequest("產品資料不能為空");
                }

                if (id != product.Id)
                {
                    return BadRequest("URL 中的 ID 與產品資料中的 ID 不符");
                }

                var success = await _productService.UpdateProductAsync(product);
                if (success)
                {
                    return Ok(product);
                }
                else
                {
                    return BadRequest("更新產品失敗");
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "更新產品時發生業務邏輯錯誤: ID={ProductId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新產品時發生錯誤: ID={ProductId}", id);
                return StatusCode(500, "更新產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 刪除產品（軟刪除）
        /// </summary>
        /// <param name="id">產品 ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            try
            {
                var result = await _productService.DeleteProductAsync(id);
                if (!result)
                {
                    return NotFound($"找不到 ID 為 {id} 的產品");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除產品時發生錯誤: ID={ProductId}", id);
                return StatusCode(500, "刪除產品時發生內部錯誤");
            }
        }

        /// <summary>
        /// 更新產品狀態
        /// </summary>
        /// <param name="id">產品 ID</param>
        /// <param name="status">新狀態</param>
        /// <returns>更新結果</returns>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateProductStatus(int id, [FromBody] ProductStatus status)
        {
            try
            {
                var result = await _productService.UpdateProductStatusAsync(id, status);
                if (!result)
                {
                    return NotFound($"找不到 ID 為 {id} 的產品");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新產品狀態時發生錯誤: ID={ProductId}", id);
                return StatusCode(500, "更新產品狀態時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得產品統計資訊
        /// </summary>
        /// <returns>產品統計資訊</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<ProductStatistics>> GetProductStatistics()
        {
            try
            {
                var statistics = await _productService.GetProductStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得產品統計資訊時發生錯誤");
                return StatusCode(500, "取得產品統計資訊時發生內部錯誤");
            }
        }

        /// <summary>
        /// 驗證產品資料
        /// </summary>
        /// <param name="product">要驗證的產品</param>
        /// <returns>驗證結果</returns>
        [HttpPost("validate")]
        public async Task<ActionResult<ValidationResult>> ValidateProduct([FromBody] Product product)
        {
            try
            {
                if (product == null)
                {
                    return BadRequest("產品資料不能為空");
                }

                var validationResult = await _productService.ValidateProductAsync(product);
                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證產品資料時發生錯誤");
                return StatusCode(500, "驗證產品資料時發生內部錯誤");
            }
        }
    }
}