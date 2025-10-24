using CioSystem.Core;
using CioSystem.Data;
using CioSystem.Models;
using CioSystem.Services;
using CioSystem.Services.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using ValidationResult = CioSystem.Services.ValidationResult;

namespace CioSystem.API.Services
{
    /// <summary>
    /// 進貨服務實現
    /// </summary>
    public class PurchasesService : CioSystem.Services.IPurchasesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PurchasesService> _logger;
        private readonly CioSystem.Services.IInventoryService _inventoryService;

        public PurchasesService(IUnitOfWork unitOfWork, ILogger<PurchasesService> logger, CioSystem.Services.IInventoryService inventoryService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        }

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

        public async Task<CioSystem.Services.ValidationResult> CreatePurchaseAsync(Purchase purchase)
        {
            try
            {
                if (purchase == null)
                {
                    return new CioSystem.Services.ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "進貨記錄不能為空" }
                    };
                }

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 重複提交防護：60 秒內相同 產品/數量/單價
                    var guardWindowStart = DateTime.UtcNow.AddSeconds(-60);
                    var duplicateCount = await _unitOfWork
                        .GetRepository<Purchase>()
                        .CountAsync(p => !p.IsDeleted
                            && p.ProductId == purchase.ProductId
                            && p.Quantity == purchase.Quantity
                            && p.UnitPrice == purchase.UnitPrice
                            && p.CreatedAt >= guardWindowStart);
                    if (duplicateCount > 0)
                    {
                        _logger.LogWarning("[API] 偵測到可能的重複提交，已拒絕: ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}",
                            purchase.ProductId, purchase.Quantity, purchase.UnitPrice);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new CioSystem.Services.ValidationResult
                        {
                            IsValid = false,
                            Errors = new List<string> { "偵測到重複提交（60秒內同筆進貨）" }
                        };
                    }

                    // 設置創建時間和更新時間
                    purchase.CreatedAt = DateTime.UtcNow;
                    purchase.UpdatedAt = DateTime.UtcNow;
                    purchase.CreatedBy = "API";
                    purchase.UpdatedBy = "API";

                    await _unitOfWork.GetRepository<Purchase>().AddAsync(purchase);
                    await _unitOfWork.SaveChangesAsync();

                    // 同步更新庫存（失敗則回滾事務）
                    var inventoryUpdated = await _inventoryService.UpdateInventoryQuantityAsync(purchase.ProductId, purchase.Quantity);
                    if (!inventoryUpdated)
                    {
                        _logger.LogError("庫存更新失敗，回滾進貨建立: ProductId={ProductId}, Quantity={Quantity}",
                            purchase.ProductId, purchase.Quantity);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new CioSystem.Services.ValidationResult { IsValid = false, Errors = new List<string> { "庫存更新失敗" } };
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功創建進貨記錄並同步庫存: Id={Id}, ProductId={ProductId}, Quantity={Quantity}",
                        purchase.Id, purchase.ProductId, purchase.Quantity);
                    return new CioSystem.Services.ValidationResult { IsValid = true };
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建進貨記錄時發生錯誤");
                return new CioSystem.Services.ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "創建進貨記錄時發生錯誤" }
                };
            }
        }

        public async Task<CioSystem.Services.ValidationResult> UpdatePurchaseAsync(Purchase purchase)
        {
            try
            {
                if (purchase == null)
                {
                    return new CioSystem.Services.ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "進貨記錄不能為空" }
                    };
                }

                var existingPurchase = await GetPurchaseByIdAsync(purchase.Id);
                if (existingPurchase == null)
                {
                    return new CioSystem.Services.ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "找不到指定的進貨記錄" }
                    };
                }

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 計算數量差異與是否更換產品
                    var quantityDifference = purchase.Quantity - existingPurchase.Quantity;
                    var productChanged = existingPurchase.ProductId != purchase.ProductId;

                    // 更新屬性
                    existingPurchase.ProductId = purchase.ProductId;
                    existingPurchase.Quantity = purchase.Quantity;
                    existingPurchase.UnitPrice = purchase.UnitPrice;
                    existingPurchase.UpdatedAt = DateTime.UtcNow;
                    existingPurchase.UpdatedBy = "API";

                    await _unitOfWork.GetRepository<Purchase>().UpdateAsync(existingPurchase);
                    await _unitOfWork.SaveChangesAsync();

                    // 同步更新庫存（失敗則回滾）
                    bool ok = true;
                    if (productChanged)
                    {
                        var deductOk = await _inventoryService.UpdateInventoryQuantityAsync(existingPurchase.ProductId, -(existingPurchase.Quantity));
                        var addOk = await _inventoryService.UpdateInventoryQuantityAsync(purchase.ProductId, purchase.Quantity);
                        ok = deductOk && addOk;
                    }
                    else if (quantityDifference != 0)
                    {
                        ok = await _inventoryService.UpdateInventoryQuantityAsync(purchase.ProductId, quantityDifference);
                    }

                    if (!ok)
                    {
                        _logger.LogError("庫存更新失敗，回滾進貨更新: ExistingProductId={ExistingProductId}, NewProductId={NewProductId}, QuantityDifference={QuantityDifference}",
                            existingPurchase.ProductId, purchase.ProductId, quantityDifference);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new CioSystem.Services.ValidationResult { IsValid = false, Errors = new List<string> { "庫存更新失敗" } };
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功更新進貨記錄並同步庫存: Id={Id}, ProductId={ProductId}, QuantityDifference={QuantityDifference}",
                        purchase.Id, purchase.ProductId, quantityDifference);
                    return new CioSystem.Services.ValidationResult { IsValid = true };
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新進貨記錄時發生錯誤: Id={Id}", purchase.Id);
                return new CioSystem.Services.ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "更新進貨記錄時發生錯誤" }
                };
            }
        }

        public async Task<bool> DeletePurchaseAsync(int id)
        {
            try
            {
                var purchase = await GetPurchaseByIdAsync(id);
                if (purchase == null)
                {
                    return false;
                }

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 軟刪除
                    purchase.IsDeleted = true;
                    purchase.UpdatedAt = DateTime.UtcNow;
                    purchase.UpdatedBy = "API";

                    await _unitOfWork.GetRepository<Purchase>().UpdateAsync(purchase);
                    await _unitOfWork.SaveChangesAsync();

                    // 同步減少庫存
                    var inventoryUpdated = await _inventoryService.UpdateInventoryQuantityAsync(purchase.ProductId, -purchase.Quantity);
                    if (!inventoryUpdated)
                    {
                        _logger.LogWarning("庫存更新失敗，但進貨記錄已刪除: ProductId={ProductId}, Quantity={Quantity}",
                            purchase.ProductId, purchase.Quantity);
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功刪除進貨記錄並同步庫存: Id={Id}, ProductId={ProductId}, Quantity={Quantity}",
                        id, purchase.ProductId, purchase.Quantity);
                    return true;
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除進貨記錄時發生錯誤: Id={Id}", id);
                return false;
            }
        }

        public async Task<bool> PurchaseExistsAsync(int id)
        {
            try
            {
                Expression<Func<Purchase, bool>> predicate = p => p.Id == id && !p.IsDeleted;
                var purchases = await _unitOfWork.GetRepository<Purchase>().FindAsync(predicate);
                return purchases.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查進貨記錄是否存在時發生錯誤: Id={Id}", id);
                return false;
            }
        }

        public async Task<(IEnumerable<Purchase> Purchases, int TotalCount)> GetPurchasesPagedAsync(int pageNumber, int pageSize, int? productId = null)
        {
            try
            {
                Expression<Func<Purchase, bool>> predicate = p => !p.IsDeleted &&
                    (!productId.HasValue || p.ProductId == productId.Value);

                var purchases = await _unitOfWork.GetRepository<Purchase>().FindAsync(predicate);

                // 按ID從小到大排序
                var orderedPurchases = purchases.OrderBy(p => p.Id);

                var totalCount = orderedPurchases.Count();
                var pagedPurchases = orderedPurchases
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("成功取得分頁進貨記錄: 頁碼={PageNumber}, 每頁大小={PageSize}, 總數量={TotalCount}",
                    pageNumber, pageSize, totalCount);

                return (pagedPurchases, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得分頁進貨記錄時發生錯誤: 頁碼={PageNumber}, 每頁大小={PageSize}", pageNumber, pageSize);
                throw;
            }
        }

        /// <summary>
        /// 重新排序進貨記錄ID為連續序號
        /// </summary>
        /// <param name="includeDeleted">是否包含已刪除的記錄</param>
        /// <returns>重新排序結果</returns>
        public async Task<CioSystem.Services.ValidationResult> ReorderPurchaseIdsAsync(bool includeDeleted = false)
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
                    return new CioSystem.Services.ValidationResult
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
                        purchase.UpdatedAt = DateTime.UtcNow;
                        purchase.UpdatedBy = "System";

                        await repository.UpdateAsync(purchase);
                        reorderedCount++;
                    }

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation("成功重新排序進貨記錄ID，共處理 {Count} 筆記錄", reorderedCount);

                    return new CioSystem.Services.ValidationResult
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
                return new CioSystem.Services.ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "重新排序進貨記錄ID時發生錯誤: " + ex.Message }
                };
            }
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

        public async Task<CioSystem.Services.Cache.PurchasesStatistics> GetPurchasesStatisticsAsync()
        {
            try
            {
                var purchases = await _unitOfWork.GetRepository<Purchase>().FindAsync(p => !p.IsDeleted);
                return new CioSystem.Services.Cache.PurchasesStatistics
                {
                    TotalPurchasesCount = purchases.Count(),
                    TotalQuantity = purchases.Sum(p => p.Quantity),
                    TotalCost = purchases.Sum(p => p.Quantity * p.UnitPrice),
                    AveragePurchaseValue = purchases.Any() ? purchases.Average(p => p.Quantity * p.UnitPrice) : 0,
                    TopSupplier = string.Empty // 暫時設為空，需要根據實際業務邏輯實現
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