using Microsoft.AspNetCore.Mvc;
using CioSystem.API.Services;
using CioSystem.Models;
using CioSystem.Services;
using ValidationResult = CioSystem.Services.ValidationResult;

namespace CioSystem.API.Controllers
{
    /// <summary>
    /// 進貨 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PurchasesController : ControllerBase
    {
        private readonly CioSystem.Services.IPurchasesService _purchasesService;
        private readonly ILogger<PurchasesController> _logger;

        public PurchasesController(CioSystem.Services.IPurchasesService purchasesService, ILogger<PurchasesController> logger)
        {
            _purchasesService = purchasesService ?? throw new ArgumentNullException(nameof(purchasesService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 取得所有進貨記錄
        /// </summary>
        /// <returns>進貨記錄列表</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchases()
        {
            try
            {
                _logger.LogInformation("API: 取得所有進貨記錄");
                var purchases = await _purchasesService.GetAllPurchasesAsync();
                return Ok(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 取得進貨記錄時發生錯誤");
                return StatusCode(500, new { message = "取得進貨記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 根據ID取得進貨記錄
        /// </summary>
        /// <param name="id">進貨記錄ID</param>
        /// <returns>進貨記錄</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Purchase>> GetPurchase(int id)
        {
            try
            {
                _logger.LogInformation("API: 取得進貨記錄: Id={Id}", id);
                var purchase = await _purchasesService.GetPurchaseByIdAsync(id);

                if (purchase == null)
                {
                    return NotFound(new { message = $"找不到ID為 {id} 的進貨記錄" });
                }

                return Ok(purchase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 取得進貨記錄時發生錯誤: Id={Id}", id);
                return StatusCode(500, new { message = "取得進貨記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 創建進貨記錄
        /// </summary>
        /// <param name="purchase">進貨記錄</param>
        /// <returns>創建的進貨記錄</returns>
        [HttpPost]
        public async Task<ActionResult<Purchase>> CreatePurchase([FromBody] Purchase purchase)
        {
            try
            {
                if (purchase == null)
                {
                    return BadRequest(new { message = "進貨記錄不能為空" });
                }

                _logger.LogInformation("API: 創建進貨記錄");
                var result = await _purchasesService.CreatePurchaseAsync(purchase);

                if (result.IsValid)
                {
                    return CreatedAtAction(nameof(GetPurchase), new { id = purchase.Id }, purchase);
                }
                else
                {
                    return BadRequest(new { message = "創建進貨記錄失敗", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 創建進貨記錄時發生錯誤");
                return StatusCode(500, new { message = "創建進貨記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 更新進貨記錄
        /// </summary>
        /// <param name="id">進貨記錄ID</param>
        /// <param name="purchase">進貨記錄</param>
        /// <returns>更新結果</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePurchase(int id, [FromBody] Purchase purchase)
        {
            try
            {
                if (purchase == null)
                {
                    return BadRequest(new { message = "進貨記錄不能為空" });
                }

                if (id != purchase.Id)
                {
                    return BadRequest(new { message = "ID不匹配" });
                }

                _logger.LogInformation("API: 更新進貨記錄: Id={Id}", id);
                var result = await _purchasesService.UpdatePurchaseAsync(purchase);

                if (result.IsValid)
                {
                    return NoContent();
                }
                else
                {
                    return BadRequest(new { message = "更新進貨記錄失敗", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 更新進貨記錄時發生錯誤: Id={Id}", id);
                return StatusCode(500, new { message = "更新進貨記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 刪除進貨記錄（軟刪除）
        /// </summary>
        /// <param name="id">進貨記錄ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchase(int id)
        {
            try
            {
                _logger.LogInformation("API: 刪除進貨記錄: Id={Id}", id);
                var success = await _purchasesService.DeletePurchaseAsync(id);

                if (success)
                {
                    return NoContent();
                }
                else
                {
                    return NotFound(new { message = $"找不到ID為 {id} 的進貨記錄" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 刪除進貨記錄時發生錯誤: Id={Id}", id);
                return StatusCode(500, new { message = "刪除進貨記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 檢查進貨記錄是否存在
        /// </summary>
        /// <param name="id">進貨記錄ID</param>
        /// <returns>是否存在</returns>
        [HttpHead("{id}")]
        public async Task<IActionResult> PurchaseExists(int id)
        {
            try
            {
                var exists = await _purchasesService.PurchaseExistsAsync(id);
                return exists ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 檢查進貨記錄是否存在時發生錯誤: Id={Id}", id);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// 取得分頁進貨記錄
        /// </summary>
        /// <param name="pageNumber">頁碼（從1開始）</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="productId">產品ID篩選（可選）</param>
        /// <returns>分頁進貨記錄</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<object>> GetPurchasesPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] int? productId = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                _logger.LogInformation("API: 取得分頁進貨記錄 - 頁碼={PageNumber}, 每頁大小={PageSize}, 產品ID={ProductId}",
                    pageNumber, pageSize, productId);

                var (purchases, totalCount) = await _purchasesService.GetPurchasesPagedAsync(pageNumber, pageSize, productId);

                var result = new
                {
                    Data = purchases,
                    Pagination = new
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                        HasPrevious = pageNumber > 1,
                        HasNext = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 取得分頁進貨記錄時發生錯誤");
                return StatusCode(500, new { message = "取得分頁進貨記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 重新排序進貨記錄ID為連續序號
        /// </summary>
        /// <param name="includeDeleted">是否包含已刪除的記錄</param>
        /// <returns>重新排序結果</returns>
        [HttpPost("reorder-ids")]
        public async Task<ActionResult> ReorderPurchaseIds([FromQuery] bool includeDeleted = false)
        {
            try
            {
                _logger.LogInformation("API: 開始重新排序進貨記錄ID，包含已刪除記錄: {IncludeDeleted}", includeDeleted);

                var result = await _purchasesService.ReorderPurchaseIdsAsync(includeDeleted);

                if (result.IsValid)
                {
                    _logger.LogInformation("API: 重新排序成功 - {Message}", string.Join(", ", result.Errors));
                    return Ok(new
                    {
                        success = true,
                        message = "重新排序成功",
                        details = result.Errors
                    });
                }
                else
                {
                    _logger.LogError("API: 重新排序失敗 - {Message}", string.Join(", ", result.Errors));
                    return BadRequest(new
                    {
                        success = false,
                        message = "重新排序失敗",
                        errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 執行重新排序時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "執行重新排序時發生錯誤",
                    error = ex.Message
                });
            }
        }
    }
}