using CioSystem.Core;
using CioSystem.Data;
using CioSystem.Models;
using CioSystem.Services.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace CioSystem.Services
{
    /// <summary>
    /// 銷售服務實現
    /// </summary>
    public class SalesService : ISalesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SalesService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IInventoryService _inventoryService;

        public SalesService(IUnitOfWork unitOfWork, ILogger<SalesService> logger, IInventoryService inventoryService, IConfiguration configuration)
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
                // 使用簡單的查詢，避免複雜的 EF Core 問題
                var repository = _unitOfWork.GetRepository<Sale>();
                var allSales = await repository.GetAllAsync();
                // 在記憶體中過濾 IsDeleted
                var filteredSales = allSales.Where(s => !s.IsDeleted).ToList();
                _logger.LogInformation("成功取得銷售記錄: {Count} 筆", filteredSales.Count);
                return filteredSales;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得所有銷售記錄時發生錯誤");
                return new List<Sale>();
            }
        }

        public async Task<(IEnumerable<Sale> Sales, int TotalCount)> GetSalesPagedAsync(int page, int pageSize, int? productId = null, string? customerName = null)
        {
            try
            {
                // ✅ 修復：建立正確的查詢條件，避免記憶體分頁
                Expression<Func<Sale, bool>> predicate = s => !s.IsDeleted &&
                    (!productId.HasValue || s.ProductId == productId.Value) &&
                    (string.IsNullOrEmpty(customerName) || s.CustomerName.Contains(customerName));

                var repository = _unitOfWork.GetRepository<Sale>();

                // 使用資料庫層分頁
                var totalCount = await repository.CountAsync(predicate);
                var result = await repository.GetPagedAsync(page, pageSize, predicate, s => s.Id);

                // ✅ 修復：載入產品導航屬性
                var salesWithProducts = await LoadProductNavigationAsync(result.Items);

                _logger.LogInformation("銷售分頁查詢完成: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
                    page, pageSize, totalCount);

                return (salesWithProducts, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得分頁銷售記錄時發生錯誤: Page={Page}, PageSize={PageSize}, ProductId={ProductId}, CustomerName={CustomerName}",
                    page, pageSize, productId, customerName);
                throw;
            }
        }

        /// <summary>
        /// 載入產品導航屬性
        /// </summary>
        private async Task<IEnumerable<Sale>> LoadProductNavigationAsync(IEnumerable<Sale> sales)
        {
            try
            {
                var productIds = sales.Select(s => s.ProductId).Distinct().ToList();
                if (!productIds.Any())
                {
                    return sales;
                }

                var productRepository = _unitOfWork.GetRepository<Product>();
                var products = await productRepository.FindAsync(p => productIds.Contains(p.Id) && !p.IsDeleted);
                var productDict = products.ToDictionary(p => p.Id, p => p);

                // 為每個銷售記錄設定產品導航屬性
                foreach (var sale in sales)
                {
                    if (productDict.TryGetValue(sale.ProductId, out var product))
                    {
                        sale.Product = product;
                    }
                }

                return sales;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入產品導航屬性時發生錯誤");
                return sales; // 返回原始資料，避免頁面崩潰
            }
        }

        private Expression<Func<Sale, bool>> CombineExpressions(Expression<Func<Sale, bool>> left, Expression<Func<Sale, bool>> right)
        {
            var parameter = Expression.Parameter(typeof(Sale), "s");
            var leftBody = Expression.Invoke(left, parameter);
            var rightBody = Expression.Invoke(right, parameter);
            var combined = Expression.AndAlso(leftBody, rightBody);
            return Expression.Lambda<Func<Sale, bool>>(combined, parameter);
        }

        public async Task<Sale?> GetSaleByIdAsync(int id)
        {
            try
            {
                Expression<Func<Sale, bool>> predicate = s => s.Id == id && !s.IsDeleted;
                var sales = await _unitOfWork.GetRepository<Sale>().FindAsync(predicate);
                var sale = sales.FirstOrDefault();

                if (sale != null)
                {
                    // 載入產品導航屬性
                    var products = await LoadProductNavigationAsync(new[] { sale });
                    sale = products.FirstOrDefault();
                }

                return sale;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據ID取得銷售記錄時發生錯誤: Id={Id}", id);
                throw;
            }
        }

        public async Task<ValidationResult> CreateSaleAsync(Sale sale)
        {
            try
            {
                if (sale == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "銷售記錄不能為空" }
                    };
                }

                // 設置創建時間和更新時間
                sale.CreatedAt = DateTime.Now;
                sale.UpdatedAt = DateTime.Now;
                sale.CreatedBy = "System";
                sale.UpdatedBy = "System";

                // 事務：銷售記錄 + 扣庫存
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 重複提交防護：可配置時間窗（預設 60 秒）
                    var windowSeconds = _configuration.GetValue<int>("Sales:DuplicateWindowSeconds", 60);
                    var guardWindowStart = DateTime.Now.AddSeconds(-windowSeconds);
                    var duplicateCount = await _unitOfWork
                        .GetRepository<Sale>()
                        .CountAsync(s => !s.IsDeleted
                            && s.ProductId == sale.ProductId
                            && s.Quantity == sale.Quantity
                            && s.UnitPrice == sale.UnitPrice
                            && s.CreatedAt >= guardWindowStart);
                    if (duplicateCount > 0)
                    {
                        _logger.LogWarning("[Service] 偵測到可能的重複提交，已拒絕: ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}",
                            sale.ProductId, sale.Quantity, sale.UnitPrice);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ValidationResult { IsValid = false, Errors = new List<string> { "偵測到重複提交（時間窗內同筆銷售）" } };
                    }

                    await _unitOfWork.GetRepository<Sale>().AddAsync(sale);
                    await _unitOfWork.SaveChangesAsync();
                    // 後續可由 Web 層觸發 SignalR 推播

                    // 推送即時更新（可由 Web 專案的 DashboardHub 接收）
                    // 此層不直接推播，改由 Web 控制器完成推播

                    // 銷售扣庫存（並自動建立庫存移動記錄）
                    // 如果有員工自留，則扣除員工自留庫存；否則扣除一般庫存
                    // 統一交由 InventoryService 處理
                    var okInv = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId, -sale.Quantity);
                    if (!okInv)
                    {
                        _logger.LogWarning("銷售創建成功但扣庫存失敗，回滾: ProductId={ProductId}, Quantity={Quantity}", sale.ProductId, sale.Quantity);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ValidationResult { IsValid = false, Errors = new List<string> { "扣庫存失敗" } };
                    }
                    if (!string.IsNullOrEmpty(sale.EmployeeRetention))
                    {
                        // 保底：若為員工自留，同步遞減 ReservedQuantity
                        try
                        {
                            var invRepo = _unitOfWork.GetRepository<Inventory>();
                            var inv = (await invRepo.FindAsync(i => !i.IsDeleted && i.ProductId == sale.ProductId && i.Type == InventoryType.Stock)).FirstOrDefault();
                            if (inv != null)
                            {
                                inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - sale.Quantity);
                                inv.UpdatedAt = DateTime.Now;
                                inv.UpdatedBy = "System";
                                await invRepo.UpdateAsync(inv);
                                await _unitOfWork.SaveChangesAsync();
                            }
                        }
                        catch { }
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功創建銷售記錄並同步扣庫存: Id={Id}", sale.Id);
                    return new ValidationResult { IsValid = true };
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
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "創建銷售記錄時發生錯誤" }
                };
            }
        }

        public async Task<ValidationResult> UpdateSaleAsync(Sale sale)
        {
            try
            {
                if (sale == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "銷售記錄不能為空" }
                    };
                }

                var existingSale = await GetSaleByIdAsync(sale.Id);
                if (existingSale == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "找不到指定的銷售記錄" }
                    };
                }

                // 事務：更新銷售 + 調整庫存
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var productChanged = existingSale.ProductId != sale.ProductId;
                    var quantityDifference = sale.Quantity - existingSale.Quantity; // 新 - 舊

                    // 更新屬性
                    existingSale.ProductId = sale.ProductId;
                    existingSale.Quantity = sale.Quantity;
                    existingSale.UnitPrice = sale.UnitPrice;
                    existingSale.CustomerName = sale.CustomerName;
                    existingSale.UpdatedAt = DateTime.Now;
                    existingSale.UpdatedBy = "System";

                    await _unitOfWork.GetRepository<Sale>().UpdateAsync(existingSale);
                    await _unitOfWork.SaveChangesAsync();
                    // 後續可由 Web 層觸發 SignalR 推播
                    // 此層不直接推播，改由 Web 控制器完成推播

                    bool ok = true;
                    if (productChanged)
                    {
                        // 將舊產品數量加回庫存，再從新產品扣新數量
                        var restoreOk = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId == existingSale.ProductId ? sale.ProductId : sale.ProductId, 0); // 佔位，不影響
                        restoreOk = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId, 0); // 保持一致性

                        var addBackOk = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId == existingSale.ProductId ? sale.ProductId : existingSale.ProductId, existingSale.Quantity); // 加回舊
                        var deductOk = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId, -sale.Quantity); // 扣新
                        ok = addBackOk && deductOk;
                    }
                    else if (quantityDifference != 0)
                    {
                        // 新 - 舊 的差額，轉成庫存扣減：-差額
                        ok = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId, -quantityDifference);
                    }

                    if (!ok)
                    {
                        _logger.LogWarning("更新銷售記錄成功但同步庫存失敗，回滾: Id={Id}", sale.Id);
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ValidationResult { IsValid = false, Errors = new List<string> { "同步庫存失敗" } };
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("成功更新銷售記錄並同步庫存: Id={Id}", sale.Id);
                    return new ValidationResult { IsValid = true };
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
                return new ValidationResult
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
                sale.UpdatedAt = DateTime.Now;
                sale.UpdatedBy = "System";

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    await _unitOfWork.GetRepository<Sale>().UpdateAsync(sale);
                    await _unitOfWork.SaveChangesAsync();

                    // 邏輯刪除銷售 -> 將數量加回庫存
                    var ok = await _inventoryService.UpdateInventoryQuantityAsync(sale.ProductId, sale.Quantity);
                    if (!ok)
                    {
                        _logger.LogWarning("刪除銷售成功但回補庫存失敗，回滾: Id={Id}", id);
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

        /// <summary>
        /// 更新員工自留庫存：統一調整主庫存並同步更新預留數量
        /// 銷售情境下 quantityAdjustment 通常為負值：
        /// - 主庫存 Quantity += 調整量
        /// - 預留數量 ReservedQuantity += 調整量（若為負則遞減，且不低於 0）
        /// </summary>
        private async Task<bool> UpdateEmployeeRetentionInventoryAsync(int productId, int quantityAdjustment, string employeeName)
        {
            try
            {
                var invRepo = _unitOfWork.GetRepository<Inventory>();
                var list = await invRepo.FindAsync(i => !i.IsDeleted && i.ProductId == productId && i.Type == InventoryType.Stock);
                var mainInventory = list.FirstOrDefault();
                if (mainInventory == null)
                {
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
                }

                // 銷售時：同時扣除庫存數量和預留數量
                if (quantityAdjustment < 0) // 銷售出貨
                {
                    // 先扣除預留數量
                    var reservedDeduction = Math.Min(Math.Abs(quantityAdjustment), mainInventory.ReservedQuantity);
                    mainInventory.ReservedQuantity -= reservedDeduction;
                    
                    // 再扣除庫存數量（剩餘的部分）
                    var remainingDeduction = Math.Abs(quantityAdjustment) - reservedDeduction;
                    mainInventory.Quantity -= remainingDeduction;
                }
                else if (quantityAdjustment > 0) // 退貨
                {
                    // 退貨時增加庫存數量
                    mainInventory.Quantity += quantityAdjustment;
                }
                mainInventory.UpdatedAt = DateTime.Now;
                mainInventory.UpdatedBy = "System";
                await invRepo.UpdateAsync(mainInventory);

                _logger.LogInformation("更新員工自留主庫存: Employee={Employee}, Product={ProductId}, Adj={Adj}, Qty={Qty}, Reserved={Reserved}",
                    employeeName, productId, quantityAdjustment, mainInventory.Quantity, mainInventory.ReservedQuantity);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新員工自留主庫存時發生錯誤: Employee={Employee}, Product={ProductId}, Adj={Adj}", employeeName, productId, quantityAdjustment);
                throw;
            }
        }

        public async Task<SalesStatistics> GetSalesStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("開始執行銷售統計查詢");
                var sales = await _unitOfWork.GetRepository<Sale>().FindAsync(s => !s.IsDeleted);
                _logger.LogInformation("銷售資料查詢完成，共 {Count} 筆記錄", sales.Count());
                
                var totalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice);
                _logger.LogInformation("計算總銷售金額: {TotalRevenue}", totalRevenue);
                
                var statistics = new SalesStatistics
                {
                    TotalSales = sales.Count(),
                    TotalQuantity = sales.Sum(s => s.Quantity),
                    TotalRevenue = totalRevenue,
                    AverageOrderValue = sales.Any() ? sales.Average(s => s.UnitPrice) : 0,
                    TopCustomer = sales.GroupBy(s => s.CustomerName)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? string.Empty
                };
                
                _logger.LogInformation("銷售統計查詢完成: TotalSales={TotalSales}, TotalQuantity={TotalQuantity}, TotalRevenue={TotalRevenue}, AverageOrderValue={AverageOrderValue}", 
                    statistics.TotalSales, statistics.TotalQuantity, statistics.TotalRevenue, statistics.AverageOrderValue);
                
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得銷售統計資料時發生錯誤");
                throw;
            }
        }
    }
}