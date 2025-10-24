using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CioSystem.Data;
using CioSystem.Models;
using CioSystem.Services;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using CioSystem.Web.Hubs;

namespace CioSystem.Web.Controllers
{
    public class PurchasesController : BaseController
    {
        private readonly IPurchasesService _purchasesService;
        private readonly ISalesService _salesService;
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private new readonly ILogger<PurchasesController> _logger;
        // SignalR 已禁用
        // private readonly IHubContext<DashboardHub> _hubContext;

        public PurchasesController(IPurchasesService purchasesService, ISalesService salesService, IProductService productService, IInventoryService inventoryService, ILogger<PurchasesController> logger) : base(logger)
        {
            _purchasesService = purchasesService;
            _salesService = salesService;
            _productService = productService;
            _inventoryService = inventoryService;
            _logger = logger;
            // SignalR 已禁用
            // _hubContext = hubContext;
        }

        // GET: Purchases
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, int? productId = null)
        {
            try
            {
                // 參數驗證
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                _logger.LogInformation("顯示進貨列表頁面 - 頁碼={Page}, 每頁大小={PageSize}, 產品ID={ProductId}", page, pageSize, productId);

                // ✅ 優化：使用快取的統計資料，避免重複查詢
                var statisticsTask = _purchasesService.GetPurchasesStatisticsAsync();

                // ✅ 優化：直接使用分頁查詢
                var purchasesTask = _purchasesService.GetPurchasesPagedAsync(page, pageSize, productId);

                // 並行執行查詢
                await Task.WhenAll(statisticsTask, purchasesTask);
                var statistics = await statisticsTask;
                var (purchases, totalCount) = await purchasesTask;

                _logger.LogInformation("取得分頁進貨記錄: {Count} 筆，總計 {TotalCount} 筆", purchases?.Count() ?? 0, totalCount);

                // ✅ 優化：使用預計算的統計資料
                ViewBag.TotalPurchases = statistics.TotalPurchasesCount;
                ViewBag.TotalQuantity = statistics.TotalQuantity;
                ViewBag.TotalAmount = statistics.TotalCost.ToString("C");
                ViewBag.AveragePrice = statistics.AveragePurchaseValue.ToString("C");

                var products = await _productService.GetAllProductsAsync();
                ViewBag.ProductMap = products.ToDictionary(p => p.Id, p => p.Name);
                ViewBag.Products = products.Where(p => p.Status == ProductStatus.Active).ToList();
                ViewBag.SelectedProductId = productId;

                // 計算分頁資訊
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPrevious = page > 1,
                    HasNext = page < totalPages,
                    PreviousPage = page > 1 ? page - 1 : (int?)null,
                    NextPage = page < totalPages ? page + 1 : (int?)null
                };

                // 額外診斷日誌
                _logger.LogInformation("進貨分頁資料：本頁筆數={PageCount}，總筆數={TotalCount}", purchases?.Count() ?? 0, totalCount);
                if (purchases == null || !purchases.Any())
                {
                    _logger.LogWarning("進貨分頁結果為空，可能原因：篩選條件或頁碼超界。Page={Page}, PageSize={PageSize}, ProductId={ProductId}",
                        page, pageSize, productId);
                }

                return View("Index", purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得進貨列表時發生錯誤: {Message}. Page={Page}, PageSize={PageSize}, ProductId={ProductId}", ex.Message, page, pageSize, productId);
                _logger.LogError(ex, "堆疊追蹤: {StackTrace}", ex.StackTrace);
                TempData["ErrorMessage"] = "載入進貨列表時發生錯誤，請稍後再試。";
                return View("Index", new List<Purchase>());
            }
        }

        // GET: Purchases/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var purchase = await _purchasesService.GetPurchaseByIdAsync(id.Value);
                if (purchase == null)
                {
                    return NotFound();
                }

                var product = await _productService.GetProductByIdAsync(purchase.ProductId);
                ViewBag.ProductName = product?.Name;

                return View("Details", purchase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得進貨記錄詳情時發生錯誤: Id={Id}", id);
                TempData["ErrorMessage"] = "載入進貨記錄詳情時發生錯誤，請稍後再試。";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Purchases/Create
        public async Task<IActionResult> Create()
        {
            var products = await _productService.GetAllProductsAsync();
            ViewBag.Products = products.Where(p => p.Status == ProductStatus.Active).ToList();
            return View("Create");
        }

        // POST: Purchases/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Quantity,UnitPrice,Supplier,EmployeeRetention")] Purchase purchase)
        {
            if (ModelState.IsValid)
            {
                // 設置進貨記錄的基本信息
                purchase.CreatedAt = DateTime.UtcNow;
                purchase.UpdatedAt = DateTime.UtcNow;
                purchase.CreatedBy = "System"; // 這裡應該從用戶認證中獲取
                purchase.UpdatedBy = "System";

                var result = await _purchasesService.CreatePurchaseAsync(purchase);
                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    var allProducts = await _productService.GetAllProductsAsync();
                    ViewBag.Products = allProducts.Where(p => p.Status == ProductStatus.Active).ToList();
                    return View("Create", purchase);
                }

                // 庫存已由服務層自動同步更新
                try
                {
                    var allSales = await _salesService?.GetAllSalesAsync()!; // 若無注入可忽略
                    var products2 = await _productService.GetAllProductsAsync();
                    var inventory2 = await _inventoryService.GetAllInventoryAsync();
                    var totalRevenue = allSales?.Sum(s => s.UnitPrice * s.Quantity) ?? 0m;
                    var totalCost = (await _purchasesService.GetAllPurchasesAsync()).Sum(p => p.UnitPrice * p.Quantity);
                    var grossProfit = totalRevenue - totalCost;
                    var lowStockCount = inventory2.Count(i => i.SafetyStock > 0 && i.Quantity <= i.SafetyStock);
                    var inventoryValue = inventory2.Sum(i => i.Quantity * (products2.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0));
                    // SignalR 已禁用
                    // await _hubContext.Clients.All.SendAsync("MetricsUpdated", new {
                        //totalRevenue,
                        //totalCost,
                        //grossProfit,
                        //lowStockCount,
                        //inventoryValue
                    //});
                }
                catch { }

                TempData["SuccessMessage"] = "進貨記錄已成功創建";
                return RedirectToAction(nameof(Index), new { page = 1, pageSize = 10 });
            }

            // 將模型錯誤彙總顯示於訊息區，方便快速定位
            var allErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            if (allErrors.Any())
            {
                TempData["ErrorMessage"] = string.Join("; ", allErrors);
            }

            var products = await _productService.GetAllProductsAsync();
            ViewBag.Products = products.Where(p => p.Status == ProductStatus.Active).ToList();
            return View("Create", purchase);
        }

        // GET: Purchases/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchase = await _purchasesService.GetPurchaseByIdAsync(id.Value);

            if (purchase == null)
            {
                return NotFound();
            }

            var products = await _productService.GetAllProductsAsync();
            ViewBag.Products = products.Where(p => p.Status == ProductStatus.Active).ToList();
            return View("Edit", purchase);
        }

        // POST: Purchases/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductId,Quantity,UnitPrice,Supplier,EmployeeRetention,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy,IsDeleted")] Purchase purchase)
        {
            if (id != purchase.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 獲取原始進貨記錄
                    var originalPurchase = await _purchasesService.GetPurchaseByIdAsync(id);

                    if (originalPurchase == null)
                    {
                        return NotFound();
                    }

                    // 計算數量差異
                    var quantityDifference = purchase.Quantity - originalPurchase.Quantity;

                    // 更新進貨記錄
                    originalPurchase.ProductId = purchase.ProductId;
                    originalPurchase.Quantity = purchase.Quantity;
                    originalPurchase.UnitPrice = purchase.UnitPrice;
                    originalPurchase.UpdatedAt = DateTime.UtcNow;
                    originalPurchase.UpdatedBy = "System";

                    var result = await _purchasesService.UpdatePurchaseAsync(originalPurchase);
                    if (!result.IsValid)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        var allProducts = await _productService.GetAllProductsAsync();
                        ViewBag.Products = allProducts.Where(p => p.Status == ProductStatus.Active).ToList();
                        return View("Edit", purchase);
                    }

                    // 庫存已由服務層自動同步更新
                    try
                    {
                        var summaryRes = await HttpContext.RequestServices
                            .GetRequiredService<CioSystem.Web.Services.IMetricsService>()
                            .GetSummaryAsync();
                        // SignalR 已禁用
                        // await _hubContext.Clients.All.SendAsync("MetricsUpdated", new {
                        //totalRevenue = summaryRes.TotalRevenue,
                        //totalCost = summaryRes.TotalCost,
                        //grossProfit = summaryRes.GrossProfit,
                        //lowStockCount = summaryRes.LowStockCount,
                        //inventoryValue = summaryRes.InventoryValue
                        //});
                    }
                    catch { }

                    TempData["SuccessMessage"] = "進貨記錄已成功更新";
                    return RedirectToAction(nameof(Index), new { page = 1, pageSize = 10 });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await PurchaseExistsAsync(purchase.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var products = await _productService.GetAllProductsAsync();
            ViewBag.Products = products.Where(p => p.Status == ProductStatus.Active).ToList();
            return View("Edit", purchase);
        }

        // GET: Purchases/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var purchase = await _purchasesService.GetPurchaseByIdAsync(id.Value);
                if (purchase == null)
                {
                    return NotFound();
                }

                return View("Delete", purchase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得進貨記錄刪除資料時發生錯誤: Id={Id}", id);
                TempData["ErrorMessage"] = "載入進貨記錄刪除資料時發生錯誤，請稍後再試。";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Purchases/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var purchase = await _purchasesService.GetPurchaseByIdAsync(id);

            if (purchase != null)
            {
                // 軟刪除
                purchase.IsDeleted = true;
                purchase.UpdatedAt = DateTime.UtcNow;
                purchase.UpdatedBy = "System";

                // 庫存已由服務層自動同步更新

                var success = await _purchasesService.DeletePurchaseAsync(id);
                if (!success)
                {
                    TempData["ErrorMessage"] = "刪除進貨記錄時發生錯誤，請稍後再試。";
                    return RedirectToAction(nameof(Index), new { page = 1, pageSize = 10 });
                }
                try
                {
                    var summaryRes = await HttpContext.RequestServices
                        .GetRequiredService<CioSystem.Web.Services.IMetricsService>()
                        .GetSummaryAsync();
                    // SignalR 已禁用
                    // await _hubContext.Clients.All.SendAsync("MetricsUpdated", new {
                    //totalRevenue = summaryRes.TotalRevenue,
                    //totalCost = summaryRes.TotalCost,
                    //grossProfit = summaryRes.GrossProfit,
                    //lowStockCount = summaryRes.LowStockCount,
                    //inventoryValue = summaryRes.InventoryValue
                    //});
                }
                catch { }
                TempData["SuccessMessage"] = "進貨記錄已成功刪除";
            }

            return RedirectToAction(nameof(Index), new { page = 1, pageSize = 10 });
        }

        private async Task<bool> PurchaseExistsAsync(int id)
        {
            return await _purchasesService.PurchaseExistsAsync(id);
        }

        /// <summary>
        /// 根據產品ID獲取品牌資訊（AJAX）
        /// </summary>
        /// <param name="productId">產品ID</param>
        /// <returns>品牌資訊</returns>
        [HttpGet]
        public async Task<IActionResult> GetProductBrand(int productId)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "產品不存在" });
                }

                return Json(new
                {
                    success = true,
                    brand = product.Brand ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取產品品牌時發生錯誤: ProductId={ProductId}", productId);
                return Json(new { success = false, message = "獲取品牌資訊時發生錯誤" });
            }
        }
    }
}