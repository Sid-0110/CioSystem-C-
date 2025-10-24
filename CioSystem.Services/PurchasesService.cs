using CioSystem.Core;
using CioSystem.Data;
using CioSystem.Models;
using CioSystem.Services.Cache;
using CioSystem.Services;
using CioSystem.Services.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CioSystem.Services
{
    /// <summary>
    /// 進貨服務實現
    /// </summary>
    public class PurchasesService : IPurchasesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PurchasesService> _logger;
        private readonly IInventoryService _inventoryService;

        public PurchasesService(IUnitOfWork unitOfWork, ILogger<PurchasesService> logger, IInventoryService inventoryService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// 取得所有進貨記錄
        /// </summary>
        /// <returns>進貨記錄列表</returns>
        public async Task<IEnumerable<Purchase>> GetAllPurchasesAsync()
        {
            try
            {
                Expression<Func<Purchase, bool>> predicate = p => !p.IsDeleted;
                var purchases = await _unitOfWork.GetRepository<Purchase>().FindAsync(predicate);
                return purchases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得所有進貨記錄時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 根據ID取得進貨記錄
        /// </summary>
        /// <param name="id">進貨記錄ID</param>
        /// <returns>進貨記錄</returns>
        public async Task<Purchase?> GetPurchaseByIdAsync(int id)
        {
            try
            {
                Expression<Func<Purchase, bool>> predicate = p => p.Id == id && !p.IsDeleted;
                var purchases = await _unitOfWork.GetRepository<Purchase>().FindAsync(predicate);
                return purchases.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據ID取得進貨記錄時發生錯誤: Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 創建進貨記錄
        /// </summary>
        /// <param name="purchase">進貨記錄</param>
        /// <returns>創建結果</returns>
        public async Task<ValidationResult> CreatePurchaseAsync(Purchase purchase)
        {
            try
            {
                var validation = await ValidatePurchaseAsync(purchase);
                if (!validation.IsValid)
                {
                    return validation;
                }

                purchase.CreatedAt = DateTime.Now;
                purchase.UpdatedAt = DateTime.Now;
                purchase.CreatedBy = "System";
                purchase.UpdatedBy = "System";

                // 使用事務確保進貨記錄和庫存更新的一致性
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 重複提交防護：60 秒內相同 產品/數量/單價
                    var guardWindowStart = DateTime.Now.AddSeconds(-60);
                    var duplicateCount = await _unitOfWork
                        .GetRepository<Purchase>()
                        .CountAsync(p => !p.IsDeleted
                            && p.ProductId == purchase.ProductId
                            && p.Quantity == purchase.Quantity
                            && p.UnitPrice == purchase.UnitPrice
                            && p.CreatedAt >= guardWindowStart);
                    if (duplicateCount > 0)
                    {
                        _logger.LogWarning("偵測到可能的重複提交，已拒絕: ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}",
                            purchase.ProductId, purchase.Quantity, purchase.UnitPrice);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ValidationResult
                        {
                            IsValid = false,
                            Errors = new List<string> { "偵測到重複提交（60秒內同筆進貨）" }
                        };
                    }

                    // 創建進貨記錄
                    await _unitOfWork.GetRepository<Purchase>().AddAsync(purchase);
                    await _unitOfWork.SaveChangesAsync();

                    // 更新庫存數量：統一交由 InventoryService 處理（含 ReservedQuantity 與 Movement）
                    bool inventoryUpdated;
                    if (!string.IsNullOrEmpty(purchase.EmployeeRetention))
                    {
                        inventoryUpdated = await _inventoryService.UpdateInventoryQuantityAsync(purchase.ProductId, purchase.Quantity);
                        // 保底：直接更新 ReservedQuantity（避免 EF 追蹤差異導致不同步）
                        await _unitOfWork.BeginTransactionAsync();
                        try
                        {
                            var invRepo = _unitOfWork.GetRepository<Inventory>();
                            var inv = (await invRepo.FindAsync(i => !i.IsDeleted && i.ProductId == purchase.ProductId && i.Type == InventoryType.Stock)).FirstOrDefault();
                            if (inv != null)
                            {
                                inv.ReservedQuantity += purchase.Quantity;
                                inv.UpdatedAt = DateTime.Now;
                                inv.UpdatedBy = "System";
                                await invRepo.UpdateAsync(inv);
                                await _unitOfWork.SaveChangesAsync();
                            }
                            await _unitOfWork.CommitTransactionAsync();
                        }
                        catch
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                        }
                    }
                    else
                    {
                        inventoryUpdated = await _inventoryService.UpdateInventoryQuantityAsync(purchase.ProductId, purchase.Quantity);
                    }

                    if (!inventoryUpdated)
                    {
                        _logger.LogError("庫存更新失敗: ProductId={ProductId}, Quantity={Quantity}, EmployeeRetention={EmployeeRetention}",
                            purchase.ProductId, purchase.Quantity, purchase.EmployeeRetention);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ValidationResult
                        {
                            IsValid = false,
                            Errors = new List<string> { "庫存更新失敗，請檢查產品是否存在" }
                        };
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功創建進貨記錄並更新庫存: Id={Id}, ProductId={ProductId}, Quantity={Quantity}",
                        purchase.Id, purchase.ProductId, purchase.Quantity);

                    return new ValidationResult { IsValid = true };
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "創建進貨記錄時發生錯誤，已回滾事務");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建進貨記錄時發生錯誤");
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "創建進貨記錄時發生錯誤: " + ex.Message }
                };
            }
        }

        /// <summary>
        /// 更新進貨記錄
        /// </summary>
        /// <param name="purchase">進貨記錄</param>
        /// <returns>更新結果</returns>
        public async Task<ValidationResult> UpdatePurchaseAsync(Purchase purchase)
        {
            try
            {
                var validation = await ValidatePurchaseAsync(purchase);
                if (!validation.IsValid)
                {
                    return validation;
                }

                var existingPurchase = await GetPurchaseByIdAsync(purchase.Id);
                if (existingPurchase == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "找不到要更新的進貨記錄" }
                    };
                }

                var quantityChange = purchase.Quantity - existingPurchase.Quantity;
                var productChanged = existingPurchase.ProductId != purchase.ProductId;

                purchase.UpdatedAt = DateTime.Now;
                purchase.UpdatedBy = "System";

                // 使用事務確保進貨記錄和庫存更新的一致性
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 更新進貨記錄
                    await _unitOfWork.GetRepository<Purchase>().UpdateAsync(purchase);
                    await _unitOfWork.SaveChangesAsync();

                    // 更新庫存數量
                    bool ok = true;
                    if (productChanged)
                    {
                        // 先從舊產品扣除原數量，再對新產品增加新數量
                        var deductOk = await _inventoryService.UpdateInventoryQuantityAsync(existingPurchase.ProductId, -existingPurchase.Quantity);
                        var addOk = await _inventoryService.UpdateInventoryQuantityAsync(purchase.ProductId, purchase.Quantity);
                        ok = deductOk && addOk;
                    }
                    else if (quantityChange != 0)
                    {
                        ok = await _inventoryService.UpdateInventoryQuantityAsync(purchase.ProductId, quantityChange);
                    }

                    if (!ok)
                    {
                        _logger.LogError("庫存更新失敗: ExistingProductId={ExistingProductId}, NewProductId={NewProductId}, QuantityChange={QuantityChange}",
                            existingPurchase.ProductId, purchase.ProductId, quantityChange);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ValidationResult
                        {
                            IsValid = false,
                            Errors = new List<string> { "庫存更新失敗，請檢查產品與庫存狀態" }
                        };
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功更新進貨記錄並同步庫存: Id={Id}, ProductId={ProductId}, Quantity={Quantity}, QuantityChange={QuantityChange}",
                        purchase.Id, purchase.ProductId, purchase.Quantity, quantityChange);

                    return new ValidationResult { IsValid = true };
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "更新進貨記錄時發生錯誤，已回滾事務");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新進貨記錄時發生錯誤");
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "更新進貨記錄時發生錯誤: " + ex.Message }
                };
            }
        }

        /// <summary>
        /// 刪除進貨記錄
        /// </summary>
        /// <param name="id">進貨記錄ID</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeletePurchaseAsync(int id)
        {
            try
            {
                var purchase = await GetPurchaseByIdAsync(id);
                if (purchase == null)
                {
                    return false;
                }

                purchase.IsDeleted = true;
                purchase.UpdatedAt = DateTime.Now;
                purchase.UpdatedBy = "System";

                // 使用事務確保進貨記錄和庫存更新的一致性
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 刪除進貨記錄
                    await _unitOfWork.GetRepository<Purchase>().UpdateAsync(purchase);
                    await _unitOfWork.SaveChangesAsync();

                    // 減少庫存數量
                    var inventoryUpdated = await _inventoryService.UpdateInventoryQuantityAsync(purchase.ProductId, -purchase.Quantity);
                    if (!inventoryUpdated)
                    {
                        _logger.LogError("庫存更新失敗: ProductId={ProductId}, Quantity={Quantity}",
                            purchase.ProductId, purchase.Quantity);
                        await _unitOfWork.RollbackTransactionAsync();
                        return false;
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功刪除進貨記錄並同步庫存: Id={Id}, ProductId={ProductId}, Quantity={Quantity}",
                        id, purchase.ProductId, purchase.Quantity);

                    return true;
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "刪除進貨記錄時發生錯誤，已回滾事務: Id={Id}", id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除進貨記錄時發生錯誤: Id={Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 檢查進貨記錄是否存在
        /// </summary>
        /// <param name="id">進貨記錄ID</param>
        /// <returns>是否存在</returns>
        public async Task<bool> PurchaseExistsAsync(int id)
        {
            try
            {
                Expression<Func<Purchase, bool>> predicate = p => p.Id == id && !p.IsDeleted;
                var repository = _unitOfWork.GetRepository<Purchase>();
                return await repository.CountAsync(predicate) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查進貨記錄是否存在時發生錯誤: Id={Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 取得分頁進貨記錄
        /// </summary>
        /// <param name="pageNumber">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="productId">產品ID篩選（可選）</param>
        /// <returns>分頁進貨記錄和總數量</returns>
        public async Task<(IEnumerable<Purchase> Purchases, int TotalCount)> GetPurchasesPagedAsync(int pageNumber, int pageSize, int? productId = null)
        {
            try
            {
                Expression<Func<Purchase, bool>> predicate = p => !p.IsDeleted;

                if (productId.HasValue)
                {
                    predicate = p => !p.IsDeleted && p.ProductId == productId.Value;
                }

                var repository = _unitOfWork.GetRepository<Purchase>();
                var totalCount = await repository.CountAsync(predicate);

                var result = await repository.GetPagedAsync(pageNumber, pageSize, predicate, p => p.Id);
                var purchases = result.Items;

                // ✅ 修復：載入產品導航屬性
                var purchasesWithProducts = await LoadProductNavigationAsync(purchases);

                return (purchasesWithProducts, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得分頁進貨記錄時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 載入產品導航屬性
        /// </summary>
        private async Task<IEnumerable<Purchase>> LoadProductNavigationAsync(IEnumerable<Purchase> purchases)
        {
            try
            {
                var productIds = purchases.Select(p => p.ProductId).Distinct().ToList();
                if (!productIds.Any())
                {
                    return purchases;
                }

                var productRepository = _unitOfWork.GetRepository<Product>();
                var products = await productRepository.FindAsync(pr => productIds.Contains(pr.Id) && !pr.IsDeleted);
                var productDict = products.ToDictionary(p => p.Id, p => p);

                // 為每個進貨記錄設定產品導航屬性
                foreach (var purchase in purchases)
                {
                    if (productDict.TryGetValue(purchase.ProductId, out var product))
                    {
                        purchase.Product = product;
                    }
                }

                return purchases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入產品導航屬性時發生錯誤");
                return purchases; // 返回原始資料，避免頁面崩潰
            }
        }

        /// <summary>
        /// 驗證進貨記錄
        /// </summary>
        /// <param name="purchase">進貨記錄</param>
        /// <returns>驗證結果</returns>
        private async Task<ValidationResult> ValidatePurchaseAsync(Purchase purchase)
        {
            var errors = new List<string>();

            if (purchase == null)
            {
                errors.Add("進貨記錄不能為空");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            if (purchase.ProductId <= 0)
            {
                errors.Add("產品ID必須大於0");
            }

            if (purchase.Quantity <= 0)
            {
                errors.Add("進貨數量必須大於0");
            }

            if (purchase.UnitPrice < 0)
            {
                errors.Add("單價不能為負數");
            }

            // 檢查產品是否存在
            if (purchase.ProductId > 0)
            {
                var productRepository = _unitOfWork.GetRepository<Product>();
                var productExists = await productRepository.CountAsync(p => p.Id == purchase.ProductId && !p.IsDeleted) > 0;
                if (!productExists)
                {
                    errors.Add("指定的產品不存在");
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// 取得所有進貨記錄（帶連續序號）
        /// </summary>
        /// <returns>帶序號的進貨記錄列表</returns>
        public async Task<IEnumerable<PurchaseWithSequenceDto>> GetAllPurchasesWithSequenceAsync()
        {
            try
            {
                var purchases = await GetAllPurchasesAsync();
                var purchaseList = purchases.OrderBy(p => p.CreatedAt).ToList();

                return purchaseList.Select((purchase, index) => new PurchaseWithSequenceDto
                {
                    Purchase = purchase,
                    Sequence = index + 1,
                    TotalCount = purchaseList.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得帶序號的進貨記錄時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 取得分頁進貨記錄（帶連續序號）
        /// </summary>
        /// <param name="pageNumber">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="productId">產品ID篩選（可選）</param>
        /// <returns>帶序號的分頁進貨記錄</returns>
        public async Task<IEnumerable<PurchaseWithSequenceDto>> GetPurchasesPagedWithSequenceAsync(int pageNumber, int pageSize, int? productId = null)
        {
            try
            {
                // 先取得所有記錄以計算正確的序號
                var allPurchases = await GetAllPurchasesWithSequenceAsync();

                // 如果指定了產品ID，進行篩選
                if (productId.HasValue)
                {
                    allPurchases = allPurchases.Where(p => p.Purchase.ProductId == productId.Value);
                }

                var allPurchasesList = allPurchases.ToList();
                var totalCount = allPurchasesList.Count;

                // 重新計算序號（基於篩選後的結果）
                var purchasesWithSequence = allPurchasesList.Select((p, index) => new PurchaseWithSequenceDto
                {
                    Purchase = p.Purchase,
                    Sequence = index + 1,
                    TotalCount = totalCount
                });

                // 分頁
                var pagedPurchases = purchasesWithSequence
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize);

                return pagedPurchases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得帶序號的分頁進貨記錄時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 重新排序進貨記錄ID為連續序號
        /// </summary>
        /// <param name="includeDeleted">是否包含已刪除的記錄</param>
        /// <returns>重新排序結果</returns>
        public async Task<ValidationResult> ReorderPurchaseIdsAsync(bool includeDeleted = false)
        {
            try
            {
                _logger.LogInformation("開始重新排序進貨記錄ID，包含已刪除記錄: {IncludeDeleted}", includeDeleted);

                // 根據參數決定是否包含已刪除的記錄
                IEnumerable<Purchase> purchases;
                if (includeDeleted)
                {
                    // 取得所有記錄（包含已刪除）
                    Expression<Func<Purchase, bool>> predicate = p => true;
                    purchases = await _unitOfWork.GetRepository<Purchase>().FindAsync(predicate);
                }
                else
                {
                    // 只取得未刪除的記錄
                    purchases = await GetAllPurchasesAsync();
                }

                var purchaseList = purchases.OrderBy(p => p.CreatedAt).ToList();

                if (!purchaseList.Any())
                {
                    return new ValidationResult
                    {
                        IsValid = true,
                        Errors = new List<string> { "沒有需要重新排序的進貨記錄" }
                    };
                }

                var repository = _unitOfWork.GetRepository<Purchase>();
                var reorderedCount = 0;

                // 開始事務
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // 重新分配ID
                    for (int i = 0; i < purchaseList.Count; i++)
                    {
                        var purchase = purchaseList[i];
                        var newId = i + 1;

                        // 如果ID已經正確，跳過
                        if (purchase.Id == newId)
                            continue;

                        // 檢查新ID是否已被使用
                        var existingPurchase = await GetPurchaseByIdAsync(newId);
                        if (existingPurchase != null && existingPurchase.Id != purchase.Id)
                        {
                            // 如果新ID已被其他記錄使用，需要先處理衝突
                            // 使用臨時ID來避免衝突
                            var tempId = purchaseList.Count + 1000 + i;

                            // 更新原記錄為臨時ID
                            purchase.Id = tempId;
                            await repository.UpdateAsync(purchase);
                            await _unitOfWork.SaveChangesAsync();
                        }

                        // 更新為新的連續ID
                        purchase.Id = newId;
                        purchase.UpdatedAt = DateTime.Now;
                        purchase.UpdatedBy = "System";

                        await repository.UpdateAsync(purchase);
                        reorderedCount++;
                    }

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation("成功重新排序進貨記錄ID，共處理 {Count} 筆記錄", reorderedCount);

                    return new ValidationResult
                    {
                        IsValid = true,
                        Errors = new List<string> { $"成功重新排序 {reorderedCount} 筆進貨記錄ID" }
                    };
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "重新排序進貨記錄ID時發生錯誤，已回滾事務");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新排序進貨記錄ID時發生錯誤");
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "重新排序進貨記錄ID時發生錯誤: " + ex.Message }
                };
            }
        }

        /// <summary>
        /// 更新員工自留庫存：需求調整為直接寫入主庫存紀錄
        /// 規則：
        /// - 庫存數量(Quantity) += 調整量
        /// - 預留數量(ReservedQuantity) += 調整量
        /// - 不建立或使用獨立的 EmployeeRetention 類型庫存，以避免被列表過濾
        /// </summary>
        private async Task<bool> UpdateEmployeeRetentionInventoryAsync(int productId, int quantityAdjustment, string employeeName)
        {
            try
            {
                // 查找主庫存記錄（一般庫存）
                var invRepo = _unitOfWork.GetRepository<Inventory>();
                var list = await invRepo.FindAsync(i => !i.IsDeleted && i.ProductId == productId && i.Type == InventoryType.Stock);
                var mainInventory = list.FirstOrDefault();

                var isNew = false;
                if (mainInventory == null)
                {
                    // 若尚無主庫存，建立一筆主庫存再寫入
                    mainInventory = new Inventory
                    {
                        ProductId = productId,
                        Quantity = 0,
                        ReservedQuantity = 0,
                        Type = InventoryType.Stock,
                        Status = InventoryStatus.Normal,
                        Notes = employeeName,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        CreatedBy = "System",
                        UpdatedBy = "System"
                    };
                    await invRepo.AddAsync(mainInventory);
                    // 立即保存取得永久主鍵，避免後續任何追蹤狀態誤判為暫存
                    await _unitOfWork.SaveChangesAsync();
                    isNew = true;
                }

                // 進貨時：增加庫存數量，同時增加預留數量（但不重複計算）
                mainInventory.Quantity += quantityAdjustment;
                if (quantityAdjustment > 0)
                {
                    // 進貨時同時增加預留數量
                    mainInventory.ReservedQuantity += quantityAdjustment;
                }
                mainInventory.UpdatedAt = DateTime.Now;
                mainInventory.UpdatedBy = "System";

                if (!isNew)
                {
                    // 重新以主鍵載入追蹤實體後更新，避免暫存鍵值造成的 Modified 例外
                    var tracked = await _unitOfWork.GetRepository<Inventory>().GetByIdAsync(mainInventory.Id);
                    if (tracked == null)
                    {
                        _logger.LogWarning("找不到主庫存記錄: ProductId={ProductId}, InventoryId={InventoryId}", productId, mainInventory.Id);
                        return false;
                    }
                    tracked.Quantity = mainInventory.Quantity;
                    tracked.ReservedQuantity = mainInventory.ReservedQuantity;
                    tracked.UpdatedAt = mainInventory.UpdatedAt;
                    tracked.UpdatedBy = mainInventory.UpdatedBy;
                    await invRepo.UpdateAsync(tracked);
                }
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("已寫入員工自留至主庫存: 員工 {Employee}, 產品 {ProductId}, 調整 {Adjustment}, 新庫存 {Qty}, 預留 {Reserved}",
                    employeeName, productId, quantityAdjustment, mainInventory.Quantity, mainInventory.ReservedQuantity);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新員工自留庫存時發生錯誤: 員工 {Employee}, 產品 {ProductId}, 調整 {Adjustment}",
                    employeeName, productId, quantityAdjustment);
                throw;
            }
        }

        public async Task<PurchasesStatistics> GetPurchasesStatisticsAsync()
        {
            try
            {
                var purchases = await _unitOfWork.GetRepository<Purchase>().FindAsync(p => !p.IsDeleted);
                return new PurchasesStatistics
                {
                    TotalPurchasesCount = purchases.Count(),
                    TotalQuantity = purchases.Sum(p => p.Quantity),
                    TotalCost = purchases.Sum(p => p.Quantity * p.UnitPrice),
                    AveragePurchaseValue = purchases.Any() ? purchases.Average(p => p.Quantity * p.UnitPrice) : 0,
                    // 其他進貨統計...
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得進貨統計資料時發生錯誤");
                throw;
            }
        }
    }
}