using CioSystem.Core;
using CioSystem.Core.Interfaces;
using CioSystem.Data;
using CioSystem.Models;
using CioSystem.Services.Cache;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CioSystem.Services
{
    /// <summary>
    /// 增強版庫存服務
    /// 整合快取、日誌和監控功能
    /// </summary>
    public class EnhancedInventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EnhancedInventoryService> _logger;
        private readonly ILoggingService _loggingService;
        private readonly IMonitoringService _monitoringService;
        private readonly CacheDecorator<IInventoryService> _cacheDecorator;

        public EnhancedInventoryService(
            IUnitOfWork unitOfWork,
            ILogger<EnhancedInventoryService> logger,
            ILoggingService loggingService,
            IMonitoringService monitoringService,
            CacheDecorator<IInventoryService> cacheDecorator)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _loggingService = loggingService;
            _monitoringService = monitoringService;
            _cacheDecorator = cacheDecorator;
        }

        public async Task<IEnumerable<Inventory>> GetAllInventoryAsync()
        {
            const string operation = "GetAllInventory";
            var startTime = DateTime.UtcNow;

            try
            {
                await _loggingService.LogBusinessAsync(operation, "Inventory", "All", "開始取得所有庫存項目");

                var result = await _cacheDecorator.ExecuteWithCacheAsync(
                    "inventory:all",
                    async () =>
                    {
                        Expression<Func<Inventory, bool>> predicate = i => !i.IsDeleted;
                        return await _unitOfWork.GetRepository<Inventory>().FindAsync(predicate);
                    },
                    TimeSpan.FromMinutes(15),
                    new[] { "inventory", "all" }
                );

                var duration = DateTime.UtcNow - startTime;
                await _monitoringService.RecordTimerAsync($"inventory.{operation}", duration);
                await _loggingService.LogPerformanceAsync(operation, duration,
                    new Dictionary<string, object> { ["Count"] = result.Count() });

                await _loggingService.LogBusinessAsync(operation, "Inventory", "All",
                    $"成功取得 {result.Count()} 個庫存項目");

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                await _monitoringService.RecordExceptionAsync(ex,
                    new Dictionary<string, object> { ["Operation"] = operation, ["Duration"] = duration.TotalMilliseconds });

                await _loggingService.LogErrorAsync($"取得所有庫存項目時發生錯誤: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Inventory?> GetInventoryByIdAsync(int id)
        {
            const string operation = "GetInventoryById";
            var startTime = DateTime.UtcNow;

            try
            {
                await _loggingService.LogBusinessAsync(operation, "Inventory", id.ToString(), "開始根據ID取得庫存項目");

                var result = await _cacheDecorator.ExecuteWithCacheAsync(
                    $"inventory:id:{id}",
                    async () => await _unitOfWork.GetRepository<Inventory>().GetByIdAsync(id),
                    TimeSpan.FromMinutes(30),
                    new[] { "inventory", $"id:{id}" }
                );

                var duration = DateTime.UtcNow - startTime;
                await _monitoringService.RecordTimerAsync($"inventory.{operation}", duration);
                await _loggingService.LogPerformanceAsync(operation, duration,
                    new Dictionary<string, object> { ["InventoryId"] = id, ["Found"] = result != null });

                await _loggingService.LogBusinessAsync(operation, "Inventory", id.ToString(),
                    result != null ? "成功取得庫存項目" : "庫存項目不存在");

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                await _monitoringService.RecordExceptionAsync(ex,
                    new Dictionary<string, object> { ["Operation"] = operation, ["InventoryId"] = id, ["Duration"] = duration.TotalMilliseconds });

                await _loggingService.LogErrorAsync($"根據ID取得庫存項目時發生錯誤: {ex.Message}", ex,
                    new Dictionary<string, object> { ["InventoryId"] = id });
                throw;
            }
        }

        public async Task<Inventory?> GetInventoryByProductIdAsync(int productId)
        {
            const string operation = "GetInventoryByProductId";
            var startTime = DateTime.UtcNow;

            try
            {
                await _loggingService.LogBusinessAsync(operation, "Inventory", productId.ToString(), "開始根據產品ID取得庫存項目");

                var result = await _cacheDecorator.ExecuteWithCacheAsync(
                    $"inventory:product:{productId}",
                    async () =>
                    {
                        Expression<Func<Inventory, bool>> predicate = i => !i.IsDeleted && i.ProductId == productId;
                        var inventory = await _unitOfWork.GetRepository<Inventory>().FindAsync(predicate);
                        return inventory?.FirstOrDefault();
                    },
                    TimeSpan.FromMinutes(15),
                    new[] { "inventory", $"product:{productId}" }
                );

                var duration = DateTime.UtcNow - startTime;
                await _monitoringService.RecordTimerAsync($"inventory.{operation}", duration);
                await _loggingService.LogPerformanceAsync(operation, duration,
                    new Dictionary<string, object> { ["ProductId"] = productId, ["Found"] = result != null });

                await _loggingService.LogBusinessAsync(operation, "Inventory", productId.ToString(),
                    result != null ? "成功取得庫存項目" : "庫存項目不存在");

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                await _monitoringService.RecordExceptionAsync(ex,
                    new Dictionary<string, object> { ["Operation"] = operation, ["ProductId"] = productId, ["Duration"] = duration.TotalMilliseconds });

                await _loggingService.LogErrorAsync($"根據產品ID取得庫存項目時發生錯誤: {ex.Message}", ex,
                    new Dictionary<string, object> { ["ProductId"] = productId });
                throw;
            }
        }

        public async Task<Inventory> CreateInventoryAsync(Inventory inventory)
        {
            const string operation = "CreateInventory";
            var startTime = DateTime.UtcNow;

            try
            {
                await _loggingService.LogBusinessAsync(operation, "Inventory", "New", "開始創建庫存項目");

                inventory.CreatedAt = DateTime.Now;
                inventory.UpdatedAt = DateTime.Now;
                inventory.CreatedBy = "System";
                inventory.UpdatedBy = "System";

                var createdInventory = await _unitOfWork.GetRepository<Inventory>().AddAsync(inventory);
                await _unitOfWork.SaveChangesAsync();

                // 使相關快取失效
                await _cacheDecorator.InvalidateCacheByTagAsync("inventory");
                await _cacheDecorator.InvalidateCacheByTagAsync("all");

                var duration = DateTime.UtcNow - startTime;
                await _monitoringService.RecordTimerAsync($"inventory.{operation}", duration);
                await _monitoringService.IncrementCounterAsync("inventory.created");

                await _loggingService.LogBusinessAsync(operation, "Inventory", createdInventory.Id.ToString(),
                    $"成功創建庫存項目，產品ID: {inventory.ProductId}");

                return createdInventory;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                await _monitoringService.RecordExceptionAsync(ex,
                    new Dictionary<string, object> { ["Operation"] = operation, ["Duration"] = duration.TotalMilliseconds });

                await _loggingService.LogErrorAsync($"創建庫存項目時發生錯誤: {ex.Message}", ex,
                    new Dictionary<string, object> { ["ProductId"] = inventory?.ProductId });
                throw;
            }
        }

        public async Task<Inventory> UpdateInventoryAsync(int id, Inventory inventory)
        {
            const string operation = "UpdateInventory";
            var startTime = DateTime.UtcNow;

            try
            {
                await _loggingService.LogBusinessAsync(operation, "Inventory", id.ToString(), "開始更新庫存項目");

                var existingInventory = await _unitOfWork.GetRepository<Inventory>().GetByIdAsync(id);
                if (existingInventory == null)
                {
                    await _loggingService.LogWarningAsync($"嘗試更新不存在的庫存項目: {id}");
                    throw new ArgumentException($"庫存項目 ID {id} 不存在");
                }

                // 記錄更新前的狀態
                await _loggingService.LogBusinessAsync(operation, "Inventory", id.ToString(),
                    $"更新庫存項目，產品ID: {existingInventory.ProductId} -> {inventory.ProductId}, 數量: {existingInventory.Quantity} -> {inventory.Quantity}");

                // 更新所有必要欄位
                existingInventory.ProductId = inventory.ProductId;
                existingInventory.Quantity = inventory.Quantity;
                existingInventory.ProductSKU = inventory.ProductSKU;
                existingInventory.SafetyStock = inventory.SafetyStock;
                existingInventory.ReservedQuantity = inventory.ReservedQuantity;
                existingInventory.Type = inventory.Type;
                existingInventory.Status = inventory.Status;
                existingInventory.ProductionDate = inventory.ProductionDate;
                existingInventory.Notes = inventory.Notes;
                existingInventory.WarningLevel = inventory.WarningLevel;
                existingInventory.LastCountDate = inventory.LastCountDate;
                existingInventory.UpdatedAt = DateTime.Now;
                existingInventory.UpdatedBy = "System";

                // 驗證預留數量不超過總庫存
                if (existingInventory.ReservedQuantity > existingInventory.Quantity)
                {
                    await _loggingService.LogWarningAsync($"預留數量超過總庫存: InventoryId={id}, Reserved={existingInventory.ReservedQuantity}, Total={existingInventory.Quantity}");
                    throw new ArgumentException("預留數量不能超過總庫存數量");
                }

                await _unitOfWork.GetRepository<Inventory>().UpdateAsync(existingInventory);
                await _unitOfWork.SaveChangesAsync();

                // 使相關快取失效
                await _cacheDecorator.InvalidateCacheAsync($"inventory:id:{id}");
                await _cacheDecorator.InvalidateCacheAsync($"inventory:product:{inventory.ProductId}");
                await _cacheDecorator.InvalidateCacheByTagAsync("inventory");

                var duration = DateTime.UtcNow - startTime;
                await _monitoringService.RecordTimerAsync($"inventory.{operation}", duration);
                await _monitoringService.IncrementCounterAsync("inventory.updated");

                await _loggingService.LogBusinessAsync(operation, "Inventory", id.ToString(), "成功更新庫存項目");

                return existingInventory;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                await _monitoringService.RecordExceptionAsync(ex,
                    new Dictionary<string, object> { ["Operation"] = operation, ["InventoryId"] = id, ["Duration"] = duration.TotalMilliseconds });

                await _loggingService.LogErrorAsync($"更新庫存項目時發生錯誤: {ex.Message}", ex,
                    new Dictionary<string, object> { ["InventoryId"] = id });
                throw;
            }
        }

        // 其他方法保持類似的模式...
        public async Task<bool> UpdateInventoryQuantityAsync(int productId, int quantityAdjustment)
        {
            // 實現邏輯...
            return true;
        }

        public async Task<bool> DeleteInventoryAsync(int id)
        {
            // 實現邏輯...
            return true;
        }

        public async Task<(IEnumerable<Inventory> Inventory, int TotalCount)> GetInventoryPagedAsync(int pageNumber, int pageSize, int? productId = null, string? productSKU = null, InventoryStatus? status = null)
        {
            // 實現邏輯...
            return (Enumerable.Empty<Inventory>(), 0);
        }

        public async Task<InventoryStatistics> GetInventoryStatisticsAsync()
        {
            // 實現邏輯...
            return new InventoryStatistics();
        }

        public async Task<IEnumerable<InventoryMovement>> GetInventoryMovementsAsync(int? inventoryId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var movements = await _unitOfWork.GetRepository<InventoryMovement>().FindAsync(m => !m.IsDeleted);

                // 應用篩選條件
                if (inventoryId.HasValue)
                {
                    movements = movements.Where(m => m.InventoryId == inventoryId.Value);
                }

                if (startDate.HasValue)
                {
                    movements = movements.Where(m => m.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    movements = movements.Where(m => m.CreatedAt <= endDate.Value);
                }

                return movements.OrderByDescending(m => m.CreatedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得庫存移動記錄時發生錯誤: InventoryId={InventoryId}, StartDate={StartDate}, EndDate={EndDate}",
                    inventoryId, startDate, endDate);
                throw;
            }
        }

        public async Task<IEnumerable<CioSystem.Services.DTOs.ConsistencyReportItemDto>> GetInventoryConsistencyReportAsync()
        {
            // 實現邏輯...
            return Enumerable.Empty<CioSystem.Services.DTOs.ConsistencyReportItemDto>();
        }
    }
}