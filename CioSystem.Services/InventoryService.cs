using CioSystem.Core;
using CioSystem.Data;
using CioSystem.Models;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using CioSystem.Services;
using CioSystem.Services.DTOs;

namespace CioSystem.Services
{
    /// <summary>
    /// 庫存服務實現
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(IUnitOfWork unitOfWork, ILogger<InventoryService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<Inventory>> GetAllInventoryAsync()
        {
            try
            {
                Expression<Func<Inventory, bool>> predicate = i => !i.IsDeleted;
                var inventory = await _unitOfWork.GetRepository<Inventory>().FindAsync(predicate);
                return inventory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得所有庫存項目時發生錯誤");
                throw;
            }
        }

        public async Task<Inventory?> GetInventoryByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.GetRepository<Inventory>().GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據 ID 取得庫存項目時發生錯誤: {InventoryId}", id);
                throw;
            }
        }

        public async Task<Inventory?> GetInventoryByProductIdAsync(int productId)
        {
            try
            {
                Expression<Func<Inventory, bool>> predicate = i => !i.IsDeleted && i.ProductId == productId;
                var inventory = await _unitOfWork.GetRepository<Inventory>().FindAsync(predicate);
                return inventory?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據產品 ID 取得庫存項目時發生錯誤: {ProductId}", productId);
                throw;
            }
        }

        public async Task<Inventory> CreateInventoryAsync(Inventory inventory)
        {
            try
            {
                inventory.CreatedAt = DateTime.Now;
                inventory.UpdatedAt = DateTime.Now;
                inventory.CreatedBy = "System";
                inventory.UpdatedBy = "System";

                var createdInventory = await _unitOfWork.GetRepository<Inventory>().AddAsync(inventory);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("成功創建庫存項目: {ProductId} (ID: {InventoryId})", inventory.ProductId, createdInventory.Id);
                return createdInventory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建庫存項目時發生錯誤: {ProductId}", inventory?.ProductId);
                throw;
            }
        }

        public async Task<Inventory> UpdateInventoryAsync(int id, Inventory inventory)
        {
            try
            {
                var existingInventory = await _unitOfWork.GetRepository<Inventory>().GetByIdAsync(id);
                if (existingInventory == null)
                {
                    _logger.LogWarning("嘗試更新不存在的庫存項目: {InventoryId}", id);
                    throw new ArgumentException($"庫存項目 ID {id} 不存在");
                }

                // 記錄更新前的狀態
                _logger.LogInformation("更新庫存項目: {InventoryId}, 產品ID: {ProductId} -> {NewProductId}, 數量: {Quantity} -> {NewQuantity}",
                    id, existingInventory.ProductId, inventory.ProductId, existingInventory.Quantity, inventory.Quantity);

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
                    _logger.LogWarning("預留數量超過總庫存: InventoryId={InventoryId}, Reserved={Reserved}, Total={Total}",
                        id, existingInventory.ReservedQuantity, existingInventory.Quantity);
                    throw new ArgumentException("預留數量不能超過總庫存數量");
                }

                await _unitOfWork.GetRepository<Inventory>().UpdateAsync(existingInventory);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("成功更新庫存項目: {InventoryId}", id);
                return existingInventory;
            }
            catch (ArgumentException)
            {
                // 重新拋出參數錯誤，不記錄為系統錯誤
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新庫存項目時發生錯誤: {InventoryId}", id);
                throw;
            }
        }

        public async Task<bool> UpdateInventoryQuantityAsync(int productId, int quantityAdjustment)
        {
            try
            {
                // 阻擋零變動
                if (quantityAdjustment == 0)
                {
                    _logger.LogInformation("忽略零變動更新: ProductId={ProductId}", productId);
                    return true;
                }
                var inventory = await GetInventoryByProductIdAsync(productId);
                if (inventory == null)
                {
                    // 若為正向調整（進貨），且庫存不存在，則自動建立一筆庫存
                    if (quantityAdjustment > 0)
                    {
                        _logger.LogInformation("產品 {ProductId} 無庫存記錄，將自動建立並設置初始數量 {Quantity}", productId, quantityAdjustment);
                        var created = await CreateInventoryAsync(new Inventory
                        {
                            ProductId = productId,
                            Quantity = 0,
                            ProductSKU = null,
                            SafetyStock = 0,
                            ReservedQuantity = 0,
                            Type = InventoryType.Stock,
                            Status = InventoryStatus.Normal,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            CreatedBy = "System",
                            UpdatedBy = "System"
                        });

                        inventory = created;
                    }
                    else
                    {
                        _logger.LogWarning("產品 {ProductId} 的庫存項目不存在且為負向調整，無法執行扣減", productId);
                        return false;
                    }
                }

                var oldQuantity = inventory.Quantity;
                var newQuantity = inventory.Quantity + quantityAdjustment;
                if (newQuantity < 0)
                {
                    _logger.LogWarning("庫存數量不能為負數: 產品 {ProductId}, 當前數量 {CurrentQuantity}, 調整 {Adjustment}",
                        productId, inventory.Quantity, quantityAdjustment);
                    return false;
                }

                // 更新庫存數量
                inventory.Quantity = newQuantity;
                inventory.UpdatedAt = DateTime.Now;
                inventory.UpdatedBy = "System";

                await _unitOfWork.GetRepository<Inventory>().UpdateAsync(inventory);
                await _unitOfWork.SaveChangesAsync();

                // 創建庫存移動記錄（冪等檢查：5 秒內相同事件不重複）
                var movementType = quantityAdjustment > 0 ? MovementType.Inbound : MovementType.Outbound;
                if (!await ExistsRecentMovementAsync(inventory.Id, movementType, Math.Abs(quantityAdjustment), "進貨/出貨調整", TimeSpan.FromSeconds(5)))
                {
                    await CreateInventoryMovementAsync(inventory.Id, quantityAdjustment, oldQuantity, newQuantity, movementType, "進貨/出貨調整");
                }

                _logger.LogInformation("成功更新庫存數量: 產品 {ProductId}, 調整 {Adjustment}, 新數量 {NewQuantity}",
                    productId, quantityAdjustment, newQuantity);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新庫存數量時發生錯誤: 產品 {ProductId}, 調整 {Adjustment}", productId, quantityAdjustment);
                throw;
            }
        }

        /// <summary>
        /// 創建庫存移動記錄
        /// </summary>
        private async Task CreateInventoryMovementAsync(int inventoryId, int quantity, int previousQuantity,
            int newQuantity, MovementType movementType, string reason)
        {
            try
            {
                var movement = new InventoryMovement
                {
                    InventoryId = inventoryId,
                    Type = movementType,
                    Quantity = Math.Abs(quantity),
                    PreviousQuantity = previousQuantity,
                    NewQuantity = newQuantity,
                    Reason = reason,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                };

                await _unitOfWork.GetRepository<InventoryMovement>().AddAsync(movement);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("成功創建庫存移動記錄: InventoryId={InventoryId}, Type={Type}, Quantity={Quantity}",
                    inventoryId, movementType, Math.Abs(quantity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建庫存移動記錄時發生錯誤: InventoryId={InventoryId}", inventoryId);
                // 不拋出異常，因為庫存更新已經成功
            }
        }

        // 冪等檢查：最近 timeWindow 內是否已有相同移動記錄
        private async Task<bool> ExistsRecentMovementAsync(int inventoryId, MovementType type, int quantity, string reason, TimeSpan timeWindow)
        {
            var since = DateTime.Now.Subtract(timeWindow);
            var repo = _unitOfWork.GetRepository<InventoryMovement>();
            var exists = await repo.CountAsync(m => !m.IsDeleted
                && m.InventoryId == inventoryId
                && m.Type == type
                && m.Quantity == quantity
                && m.Reason == reason
                && m.CreatedAt >= since) > 0;
            return exists;
        }

        public async Task<bool> DeleteInventoryAsync(int id)
        {
            try
            {
                var inventory = await _unitOfWork.GetRepository<Inventory>().GetByIdAsync(id);
                if (inventory == null)
                {
                    _logger.LogWarning("嘗試刪除不存在的庫存項目: {InventoryId}", id);
                    return false;
                }

                inventory.IsDeleted = true;
                inventory.UpdatedAt = DateTime.Now;
                inventory.UpdatedBy = "System";

                await _unitOfWork.GetRepository<Inventory>().UpdateAsync(inventory);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("成功刪除庫存項目: {InventoryId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除庫存項目時發生錯誤: {InventoryId}", id);
                throw;
            }
        }

        public async Task<(IEnumerable<Inventory> Inventory, int TotalCount)> GetInventoryPagedAsync(int pageNumber, int pageSize, int? productId = null, string? productSKU = null, InventoryStatus? status = null)
        {
            try
            {
                Expression<Func<Inventory, bool>> predicate = i => !i.IsDeleted &&
                    (!productId.HasValue || i.ProductId == productId.Value) &&
                    (string.IsNullOrEmpty(productSKU) || (i.ProductSKU != null && i.ProductSKU.Contains(productSKU))) &&
                    (!status.HasValue || i.Status == status.Value);

                var repository = _unitOfWork.GetRepository<Inventory>();

                // ✅ 修復：使用資料庫層分頁，避免記憶體分頁
                var totalCount = await repository.CountAsync(predicate);
                var result = await repository.GetPagedAsync(pageNumber, pageSize, predicate, i => i.Id);

                _logger.LogInformation("庫存分頁查詢完成: Page={Page}, PageSize:{PageSize}, TotalCount={TotalCount}",
                    pageNumber, pageSize, totalCount);

                return (result.Items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得分頁庫存項目時發生錯誤");
                throw;
            }
        }

        public async Task<InventoryStatistics> GetInventoryStatisticsAsync()
        {
            try
            {
                Expression<Func<Inventory, bool>> predicate = i => !i.IsDeleted;
                var inventory = await _unitOfWork.GetRepository<Inventory>().FindAsync(predicate);

                // 取得產品資訊以計算總價值
                var products = await _unitOfWork.GetRepository<Product>().FindAsync(p => !p.IsDeleted);
                var productMap = products.ToDictionary(p => p.Id, p => p.Price);

                var totalValue = inventory.Sum(i => i.Quantity * (productMap.ContainsKey(i.ProductId) ? productMap[i.ProductId] : 0));
                var averageQuantity = inventory.Any() ? (decimal)inventory.Average(i => i.Quantity) : 0;

                var statistics = new InventoryStatistics
                {
                    TotalItems = inventory.Count(),
                    TotalQuantity = inventory.Sum(i => i.Quantity),
                    TotalValue = totalValue,
                    AverageQuantity = averageQuantity,
                    AvailableItems = inventory.Count(i => i.Status == InventoryStatus.Normal || i.Status == InventoryStatus.Excess),
                    UnavailableItems = inventory.Count(i => i.Status == InventoryStatus.OutOfStock || i.Status == InventoryStatus.LowStock),
                    LowStockItems = inventory.Count(i => i.Status == InventoryStatus.LowStock),
                    ExpiredItems = 0, // 暫時設為0，需要根據實際業務邏輯實現
                    ExpiringSoonItems = 0 // 暫時設為0，需要根據實際業務邏輯實現
                };

                _logger.LogInformation("成功取得庫存統計資訊: TotalItems={TotalItems}, TotalQuantity={TotalQuantity}, TotalValue={TotalValue}",
                    statistics.TotalItems, statistics.TotalQuantity, statistics.TotalValue);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得庫存統計資訊時發生錯誤");
                throw;
            }
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

        public async Task<IEnumerable<ConsistencyReportItemDto>> GetInventoryConsistencyReportAsync()
        {
            try
            {
                // 讀取所有必要資料
                var inventories = await _unitOfWork.GetRepository<Inventory>().FindAsync(i => !i.IsDeleted);
                var purchases = await _unitOfWork.GetRepository<Purchase>().FindAsync(p => !p.IsDeleted);
                var sales = await _unitOfWork.GetRepository<Sale>().FindAsync(s => !s.IsDeleted);
                var products = await _unitOfWork.GetRepository<Product>().FindAsync(p => !p.IsDeleted);

                var productNameMap = products.ToDictionary(p => p.Id, p => new { p.Name, p.SKU });
                var currentQtyMap = inventories.GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
                var purchaseMap = purchases.GroupBy(p => p.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
                var salesMap = sales.GroupBy(s => s.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                var productIds = new HashSet<int>(currentQtyMap.Keys);
                foreach (var id in purchaseMap.Keys) productIds.Add(id);
                foreach (var id in salesMap.Keys) productIds.Add(id);

                var result = new List<ConsistencyReportItemDto>();
                foreach (var pid in productIds.OrderBy(x => x))
                {
                    var inTotal = purchaseMap.ContainsKey(pid) ? purchaseMap[pid] : 0;
                    var outTotal = salesMap.ContainsKey(pid) ? salesMap[pid] : 0;
                    var expected = inTotal - outTotal;
                    var current = currentQtyMap.ContainsKey(pid) ? currentQtyMap[pid] : 0;

                    productNameMap.TryGetValue(pid, out var pinfo);

                    result.Add(new ConsistencyReportItemDto
                    {
                        ProductId = pid,
                        ProductSKU = pinfo?.SKU,
                        ProductName = pinfo?.Name,
                        PurchasesTotal = inTotal,
                        SalesTotal = outTotal,
                        ExpectedQuantity = expected,
                        CurrentQuantity = current,
                        Difference = current - expected
                    });
                }

                _logger.LogInformation("成功生成庫存一致性對帳報表，共 {Count} 項產品", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成庫存一致性對帳報表時發生錯誤");
                throw;
            }
        }
    }
}