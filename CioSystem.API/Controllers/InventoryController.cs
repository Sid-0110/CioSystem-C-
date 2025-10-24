using CioSystem.Models;
using CioSystem.API.Services;
using CioSystem.Services;
using CioSystem.Services.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ValidationResult = CioSystem.Services.ValidationResult;

namespace CioSystem.API.Controllers
{
    /// <summary>
    /// 庫存 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly CioSystem.Services.IInventoryService _inventoryService;
        private readonly ILogger<InventoryController> _logger;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="inventoryService">庫存服務</param>
        /// <param name="logger">日誌記錄器</param>
        public InventoryController(CioSystem.Services.IInventoryService inventoryService, ILogger<InventoryController> logger)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 取得所有庫存
        /// </summary>
        /// <returns>庫存列表</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetInventory()
        {
            var inventory = await _inventoryService.GetAllInventoryAsync();
            return Ok(inventory);
        }

        /// <summary>
        /// 根據 ID 取得庫存項目
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <returns>庫存項目</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Inventory>> GetInventoryItem(int id)
        {
            var inventory = await _inventoryService.GetInventoryByIdAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }
            return Ok(inventory);
        }

        /// <summary>
        /// 創建新的庫存項目
        /// </summary>
        /// <param name="inventory">庫存項目</param>
        /// <returns>創建的庫存項目</returns>
        [HttpPost]
        public async Task<ActionResult<Inventory>> CreateInventory(Inventory inventory)
        {
            try
            {
                var createdInventory = await _inventoryService.CreateInventoryAsync(inventory);
                return CreatedAtAction(nameof(GetInventoryItem), new { id = createdInventory.Id }, createdInventory);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message); // Product not found
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message); // Inventory for product already exists
            }
            catch (Exception)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while creating the inventory item.");
            }
        }

        /// <summary>
        /// 更新庫存項目
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <param name="inventory">更新的庫存項目</param>
        /// <returns>更新後的庫存項目</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<Inventory>> UpdateInventory(int id, Inventory inventory)
        {
            try
            {
                var updatedInventory = await _inventoryService.UpdateInventoryAsync(id, inventory);
                return Ok(updatedInventory);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while updating the inventory item.");
            }
        }

        /// <summary>
        /// 刪除庫存項目
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <returns>無內容</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteInventory(int id)
        {
            try
            {
                await _inventoryService.DeleteInventoryAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while deleting the inventory item.");
            }
        }

        /// <summary>
        /// 調整庫存數量
        /// </summary>
        /// <param name="inventoryId">庫存 ID</param>
        /// <param name="adjustment">調整資訊</param>
        /// <returns>更新後的庫存項目</returns>
        [HttpPost("{inventoryId}/adjust")]
        public async Task<ActionResult> AdjustInventoryQuantity(int inventoryId, [FromBody] InventoryAdjustmentDto adjustment)
        {
            try
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(inventoryId);
                if (inventory == null)
                {
                    return NotFound(new { message = "找不到指定的庫存項目" });
                }
                var success = await _inventoryService.UpdateInventoryQuantityAsync(inventory.ProductId, adjustment.QuantityChange);
                if (success)
                {
                    return Ok(new { message = "庫存調整成功" });
                }
                else
                {
                    return BadRequest("庫存調整失敗");
                }
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while adjusting inventory quantity.");
            }
        }

        /// <summary>
        /// 取得庫存變動記錄
        /// </summary>
        /// <param name="inventoryId">庫存 ID</param>
        /// <returns>庫存變動記錄列表</returns>
        [HttpGet("{inventoryId}/movements")]
        public async Task<ActionResult<IEnumerable<InventoryMovement>>> GetInventoryMovements(int inventoryId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var movements = await _inventoryService.GetInventoryMovementsAsync(inventoryId, startDate, endDate);
                return Ok(movements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得庫存變動記錄時發生錯誤: InventoryId={InventoryId}, StartDate={StartDate}, EndDate={EndDate}", inventoryId, startDate, endDate);
                return StatusCode(500, "An error occurred while retrieving inventory movements.");
            }
        }

        /// <summary>
        /// 取得總庫存數量
        /// </summary>
        /// <returns>總庫存數量</returns>
        [HttpGet("total-quantity")]
        public async Task<ActionResult<int>> GetTotalStockQuantity()
        {
            var stats = await _inventoryService.GetInventoryStatisticsAsync();
            return Ok(stats?.TotalQuantity ?? 0);
        }

        /// <summary>
        /// 取得總庫存價值
        /// </summary>
        /// <returns>總庫存價值</returns>
        [HttpGet("total-value")]
        public async Task<ActionResult<decimal>> GetTotalStockValue()
        {
            var stats = await _inventoryService.GetInventoryStatisticsAsync();
            return Ok(stats?.TotalValue ?? 0);
        }

        /// <summary>
        /// 依據進貨與銷售對帳庫存，回傳每個產品的差異
        /// </summary>
        [HttpGet("consistency-report")]
        public async Task<ActionResult<IEnumerable<ConsistencyReportItemDto>>> GetInventoryConsistencyReport([FromQuery] int? productId = null, [FromQuery] string? sku = null)
        {
            try
            {
                var report = await _inventoryService.GetInventoryConsistencyReportAsync();
                if (productId.HasValue)
                {
                    report = report.Where(r => r.ProductId == productId.Value);
                }
                if (!string.IsNullOrWhiteSpace(sku))
                {
                    report = report.Where(r => string.Equals(r.ProductSKU, sku, StringComparison.OrdinalIgnoreCase));
                }
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得庫存一致性對帳報表時發生錯誤");
                return StatusCode(500, "An error occurred while generating consistency report.");
            }
        }
    }

    /// <summary>
    /// 庫存調整資料傳輸物件
    /// </summary>
    public class InventoryAdjustmentDto
    {
        /// <summary>
        /// 數量變化
        /// </summary>
        public int QuantityChange { get; set; }

        /// <summary>
        /// 調整原因
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}