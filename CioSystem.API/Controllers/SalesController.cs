using Microsoft.AspNetCore.Mvc;
using CioSystem.API.Services;
using CioSystem.Models;
using CioSystem.Services;
using ValidationResult = CioSystem.Services.ValidationResult;

namespace CioSystem.API.Controllers
{
    /// <summary>
    /// 銷售 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly CioSystem.Services.ISalesService _salesService;
        private readonly ILogger<SalesController> _logger;

        public SalesController(CioSystem.Services.ISalesService salesService, ILogger<SalesController> logger)
        {
            _salesService = salesService ?? throw new ArgumentNullException(nameof(salesService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 取得所有銷售記錄
        /// </summary>
        /// <returns>銷售記錄列表</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sale>>> GetSales()
        {
            try
            {
                _logger.LogInformation("API: 取得所有銷售記錄");
                var sales = await _salesService.GetAllSalesAsync();
                return Ok(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 取得銷售記錄時發生錯誤");
                return StatusCode(500, new { message = "取得銷售記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 根據ID取得銷售記錄
        /// </summary>
        /// <param name="id">銷售記錄ID</param>
        /// <returns>銷售記錄</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Sale>> GetSale(int id)
        {
            try
            {
                _logger.LogInformation("API: 取得銷售記錄: Id={Id}", id);
                var sale = await _salesService.GetSaleByIdAsync(id);

                if (sale == null)
                {
                    return NotFound(new { message = $"找不到ID為 {id} 的銷售記錄" });
                }

                return Ok(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 取得銷售記錄時發生錯誤: Id={Id}", id);
                return StatusCode(500, new { message = "取得銷售記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 創建銷售記錄
        /// </summary>
        /// <param name="sale">銷售記錄</param>
        /// <returns>創建的銷售記錄</returns>
        [HttpPost]
        public async Task<ActionResult<Sale>> CreateSale([FromBody] Sale sale)
        {
            try
            {
                if (sale == null)
                {
                    return BadRequest(new { message = "銷售記錄不能為空" });
                }

                _logger.LogInformation("API: 創建銷售記錄");
                var result = await _salesService.CreateSaleAsync(sale);

                if (result.IsValid)
                {
                    return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, sale);
                }
                else
                {
                    return BadRequest(new { message = "創建銷售記錄失敗", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 創建銷售記錄時發生錯誤");
                return StatusCode(500, new { message = "創建銷售記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 更新銷售記錄
        /// </summary>
        /// <param name="id">銷售記錄ID</param>
        /// <param name="sale">銷售記錄</param>
        /// <returns>更新結果</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSale(int id, [FromBody] Sale sale)
        {
            try
            {
                if (sale == null)
                {
                    return BadRequest(new { message = "銷售記錄不能為空" });
                }

                if (id != sale.Id)
                {
                    return BadRequest(new { message = "ID不匹配" });
                }

                _logger.LogInformation("API: 更新銷售記錄: Id={Id}", id);
                var result = await _salesService.UpdateSaleAsync(sale);

                if (result.IsValid)
                {
                    return NoContent();
                }
                else
                {
                    return BadRequest(new { message = "更新銷售記錄失敗", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 更新銷售記錄時發生錯誤: Id={Id}", id);
                return StatusCode(500, new { message = "更新銷售記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 刪除銷售記錄（軟刪除）
        /// </summary>
        /// <param name="id">銷售記錄ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSale(int id)
        {
            try
            {
                _logger.LogInformation("API: 刪除銷售記錄: Id={Id}", id);
                var success = await _salesService.DeleteSaleAsync(id);

                if (success)
                {
                    return NoContent();
                }
                else
                {
                    return NotFound(new { message = $"找不到ID為 {id} 的銷售記錄" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 刪除銷售記錄時發生錯誤: Id={Id}", id);
                return StatusCode(500, new { message = "刪除銷售記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 檢查銷售記錄是否存在
        /// </summary>
        /// <param name="id">銷售記錄ID</param>
        /// <returns>是否存在</returns>
        [HttpHead("{id}")]
        public async Task<IActionResult> SaleExists(int id)
        {
            try
            {
                var exists = await _salesService.SaleExistsAsync(id);
                return exists ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: 檢查銷售記錄是否存在時發生錯誤: Id={Id}", id);
                return StatusCode(500);
            }
        }
    }
}