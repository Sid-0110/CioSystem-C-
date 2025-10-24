using CioSystem.Core;
using CioSystem.Data;
using CioSystem.Models;
using CioSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using ValidationResult = CioSystem.Services.ValidationResult;

namespace CioSystem.API.Services
{
    /// <summary>
    /// 銷售服務實現
    /// </summary>
    public class SalesService : CioSystem.Services.ISalesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SalesService> _logger;
        private readonly IConfiguration _configuration;
        private readonly CioSystem.Services.IInventoryService _inventoryService;

        public SalesService(IUnitOfWork unitOfWork, ILogger<SalesService> logger, CioSystem.Services.IInventoryService inventoryService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IEnumerable<Sale>> GetAllSalesAsync()
        {
            try
            {
                Expression<Func<Sale, bool>> predicate = s => !s.IsDeleted;
                var sales = await _unitOfWork.GetRepository<Sale>().FindAsync(predicate);
                return sales;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得所有銷售記錄時發生錯誤");
                throw;
            }
        }

        public async Task<Sale?> GetSaleByIdAsync(int id)
        {
            try
            {
                Expression<Func<Sale, bool>> predicate = s => s.Id == id && !s.IsDeleted;
                var sales = await _unitOfWork.GetRepository<Sale>().FindAsync(predicate);
                return sales.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據ID取得銷售記錄時發生錯誤: Id={Id}", id);
                throw;
            }
        }

        public async Task<CioSystem.Services.ValidationResult> CreateSaleAsync(Sale sale)
        {
            try
            {
                if (sale == null)
                {
                    return new CioSystem.Services.ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "銷售記錄不能為空" }
                    };
                }

                // 設置創建時間和更新時間
                sale.CreatedAt = DateTime.UtcNow;
                sale.UpdatedAt = DateTime.UtcNow;
                sale.CreatedBy = "API";
                sale.UpdatedBy = "API";

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 重複提交防護：可配置時間窗（預設 60 秒）
                    var windowSeconds = _configuration.GetValue<int>("Sales:DuplicateWindowSeconds", 60);
                    var guardWindowStart = DateTime.UtcNow.AddSeconds(-windowSeconds);
                    var duplicateCount = await _unitOfWork
                        .GetRepository<Sale>()
                        .CountAsync(s => !s.IsDeleted
                            && s.ProductId == sale.ProductId
                            && s.Quantity == sale.Quantity
                            && s.UnitPrice == sale.UnitPrice
                            && s.CreatedAt >= guardWindowStart);
                    if (duplicateCount > 0)
                    {
                        _logger.LogWarning("[API] 偵測到可能的重複提交，已拒絕: ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}",
                            sale.ProductId, sale.Quantity, sale.UnitPrice);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new CioSystem.Services.ValidationResult { IsValid = false, Errors = new List<string> { "偵測到重複提交（時間窗內同筆銷售）" } };
                    }

                    await _unitOfWork.GetRepository<Sale>().AddAsync(sale);
                    await _unitOfWork.SaveChangesAsync();

                    // 銷售扣庫存
                    var ok = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId, -sale.Quantity);
                    if (!ok)
                    {
                        _logger.LogWarning("[API] 銷售創建成功但扣庫存失敗，回滾: ProductId={ProductId}, Quantity={Quantity}", sale.ProductId, sale.Quantity);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new CioSystem.Services.ValidationResult { IsValid = false, Errors = new List<string> { "扣庫存失敗" } };
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功創建銷售記錄並同步扣庫存: Id={Id}", sale.Id);
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
                _logger.LogError(ex, "創建銷售記錄時發生錯誤");
                return new CioSystem.Services.ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "創建銷售記錄時發生錯誤" }
                };
            }
        }

        public async Task<CioSystem.Services.ValidationResult> UpdateSaleAsync(Sale sale)
        {
            try
            {
                if (sale == null)
                {
                    return new CioSystem.Services.ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "銷售記錄不能為空" }
                    };
                }

                var existingSale = await GetSaleByIdAsync(sale.Id);
                if (existingSale == null)
                {
                    return new CioSystem.Services.ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "找不到指定的銷售記錄" }
                    };
                }

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var productChanged = existingSale.ProductId != sale.ProductId;
                    var quantityDifference = sale.Quantity - existingSale.Quantity; // 新 - 舊

                    // 更新屬性
                    existingSale.ProductId = sale.ProductId;
                    existingSale.Quantity = sale.Quantity;
                    existingSale.UnitPrice = sale.UnitPrice;
                    existingSale.UpdatedAt = DateTime.UtcNow;
                    existingSale.UpdatedBy = "API";

                    await _unitOfWork.GetRepository<Sale>().UpdateAsync(existingSale);
                    await _unitOfWork.SaveChangesAsync();

                    bool ok = true;
                    if (productChanged)
                    {
                        // 將舊產品數量加回，再從新產品扣新數量
                        var addBackOk = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId == existingSale.ProductId ? sale.ProductId : existingSale.ProductId, existingSale.Quantity);
                        var deductOk = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId, -sale.Quantity);
                        ok = addBackOk && deductOk;
                    }
                    else if (quantityDifference != 0)
                    {
                        ok = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId, -quantityDifference);
                    }

                    if (!ok)
                    {
                        _logger.LogWarning("[API] 更新銷售記錄成功但同步庫存失敗，回滾: Id={Id}", sale.Id);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new CioSystem.Services.ValidationResult { IsValid = false, Errors = new List<string> { "同步庫存失敗" } };
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功更新銷售記錄並同步庫存: Id={Id}", sale.Id);
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
                _logger.LogError(ex, "更新銷售記錄時發生錯誤: Id={Id}", sale.Id);
                return new CioSystem.Services.ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "更新銷售記錄時發生錯誤" }
                };
            }
        }

        public async Task<bool> DeleteSaleAsync(int id)
        {
            try
            {
                var sale = await GetSaleByIdAsync(id);
                if (sale == null)
                {
                    return false;
                }

                // 軟刪除
                sale.IsDeleted = true;
                sale.UpdatedAt = DateTime.UtcNow;
                sale.UpdatedBy = "API";

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    await _unitOfWork.GetRepository<Sale>().UpdateAsync(sale);
                    await _unitOfWork.SaveChangesAsync();

                    // 刪除銷售 -> 回補庫存
                    var ok = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId, sale.Quantity);
                    if (!ok)
                    {
                        _logger.LogWarning("[API] 刪除銷售成功但回補庫存失敗，回滾: Id={Id}", id);
                        await _unitOfWork.RollbackTransactionAsync();
                        return false;
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功刪除銷售記錄並回補庫存: Id={Id}", id);
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
                _logger.LogError(ex, "刪除銷售記錄時發生錯誤: Id={Id}", id);
                return false;
            }
        }

        public async Task<bool> SaleExistsAsync(int id)
        {
            try
            {
                Expression<Func<Sale, bool>> predicate = s => s.Id == id && !s.IsDeleted;
                var sales = await _unitOfWork.GetRepository<Sale>().FindAsync(predicate);
                return sales.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查銷售記錄是否存在時發生錯誤: Id={Id}", id);
                return false;
            }
        }

        public async Task<(IEnumerable<Sale> Sales, int TotalCount)> GetSalesPagedAsync(int page, int pageSize, int? productId = null, string? customerName = null)
        {
            try
            {
                var allSales = await _unitOfWork.GetRepository<Sale>().FindAsync(s => !s.IsDeleted);

                // 應用篩選條件
                if (productId.HasValue)
                {
                    allSales = allSales.Where(s => s.ProductId == productId.Value);
                }

                if (!string.IsNullOrEmpty(customerName))
                {
                    allSales = allSales.Where(s => s.CustomerName.Contains(customerName));
                }

                var totalCount = allSales.Count();
                var sales = allSales.OrderBy(s => s.Id).Skip((page - 1) * pageSize).Take(pageSize);

                return (sales, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得分頁銷售記錄時發生錯誤: Page={Page}, PageSize={PageSize}, ProductId={ProductId}, CustomerName={CustomerName}",
                    page, pageSize, productId, customerName);
                throw;
            }
        }

        public async Task<CioSystem.Services.Cache.SalesStatistics> GetSalesStatisticsAsync()
        {
            try
            {
                var sales = await _unitOfWork.GetRepository<Sale>().FindAsync(s => !s.IsDeleted);
                return new CioSystem.Services.Cache.SalesStatistics
                {
                    TotalSales = sales.Count(),
                    TotalQuantity = sales.Sum(s => s.Quantity),
                    TotalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice),
                    AverageOrderValue = sales.Any() ? sales.Average(s => s.Quantity * s.UnitPrice) : 0,
                    TopCustomer = sales.GroupBy(s => s.CustomerName)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得銷售統計資料時發生錯誤");
                throw;
            }
        }
    }
}