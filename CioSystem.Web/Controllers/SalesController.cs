using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CioSystem.Services;
using CioSystem.Models;
using CioSystem.Web.Hubs;

namespace CioSystem.Web.Controllers
{
    public class SalesController : BaseController
    {
        private readonly ISalesService _salesService;
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private new readonly ILogger<SalesController> _logger;
        private readonly IHubContext<DashboardHub> _hubContext;

        public SalesController(ISalesService salesService, IProductService productService, IInventoryService inventoryService, ILogger<SalesController> logger, IHubContext<DashboardHub> hubContext) : base(logger)
        {
            _salesService = salesService;
            _productService = productService;
            _inventoryService = inventoryService;
            _logger = logger;
            _hubContext = hubContext;
        }

        // GET: Sales
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, int? productId = null, string? customerName = null)
        {
            try
            {
                _logger.LogInformation("顯示銷售列表頁面: Page={Page}, PageSize={PageSize}, ProductId={ProductId}, CustomerName={CustomerName}",
                    page, pageSize, productId, customerName);

                // 並行執行分頁查詢和統計查詢
                var salesTask = _salesService.GetSalesPagedAsync(page, pageSize, productId, customerName);
                var statisticsTask = _salesService.GetSalesStatisticsAsync();

                await Task.WhenAll(salesTask, statisticsTask);
                var (sales, totalCount) = await salesTask;
                var statistics = await statisticsTask;

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.ProductId = productId;
                ViewBag.CustomerName = customerName;

                // 設置統計資料
                ViewBag.TotalSales = statistics.TotalSales;
                ViewBag.TotalQuantity = statistics.TotalQuantity;
                ViewBag.TotalRevenue = statistics.TotalRevenue;
                ViewBag.AverageOrderValue = statistics.AverageOrderValue;
                ViewBag.TopCustomer = statistics.TopCustomer;
                
                _logger.LogInformation("設置 ViewBag 統計資料: AverageOrderValue={AverageOrderValue}", statistics.AverageOrderValue);

                return View(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得銷售列表時發生錯誤");
                TempData["ErrorMessage"] = "取得銷售列表時發生錯誤。";
                return View(Enumerable.Empty<Sale>());
            }
        }

        // GET: Sales/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    TempData["ErrorMessage"] = "找不到指定的銷售記錄。";
                    return RedirectToAction(nameof(Index));
                }

                return View(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得銷售記錄詳情時發生錯誤: Id={Id}", id);
                TempData["ErrorMessage"] = "取得銷售記錄詳情時發生錯誤。";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Sales/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                ViewBag.Products = products;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入銷售建立頁面時發生錯誤");
                TempData["ErrorMessage"] = "載入銷售建立頁面時發生錯誤。";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sale sale)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _salesService.CreateSaleAsync(sale);
                    if (result.IsValid)
                    {
                        // 嘗試發送 SignalR 更新，但不影響主要功能
                        try
                        {
                            await _hubContext.Clients.All.SendAsync("MetricsUpdated", new
                            {
                                totalRevenue = 0,
                                totalCost = 0,
                                grossProfit = 0,
                                lowStockCount = 0,
                                inventoryValue = 0
                            });
                        }
                        catch (Exception signalREx)
                        {
                            _logger.LogWarning(signalREx, "SignalR 更新失敗，但不影響主要功能");
                        }

                        TempData["SuccessMessage"] = "銷售記錄已成功創建";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                    }
                }

                var products = await _productService.GetAllProductsAsync();
                ViewBag.Products = products;
                return View(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立銷售記錄時發生錯誤");
                TempData["ErrorMessage"] = "建立銷售記錄時發生錯誤。";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Sales/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    TempData["ErrorMessage"] = "找不到指定的銷售記錄。";
                    return RedirectToAction(nameof(Index));
                }

                var products = await _productService.GetAllProductsAsync();
                ViewBag.Products = products;
                return View(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入銷售編輯頁面時發生錯誤: Id={Id}", id);
                TempData["ErrorMessage"] = "載入銷售編輯頁面時發生錯誤。";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Sales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Sale sale)
        {
            try
            {
                if (id != sale.Id)
                {
                    TempData["ErrorMessage"] = "銷售記錄ID不匹配。";
                    return RedirectToAction(nameof(Index));
                }

                if (ModelState.IsValid)
                {
                    var result = await _salesService.UpdateSaleAsync(sale);
                    if (result.IsValid)
                    {
                        // 嘗試發送 SignalR 更新，但不影響主要功能
                        try
                        {
                            await _hubContext.Clients.All.SendAsync("MetricsUpdated", new
                            {
                                totalRevenue = 0,
                                totalCost = 0,
                                grossProfit = 0,
                                lowStockCount = 0,
                                inventoryValue = 0
                            });
                        }
                        catch (Exception signalREx)
                        {
                            _logger.LogWarning(signalREx, "SignalR 更新失敗，但不影響主要功能");
                        }

                        TempData["SuccessMessage"] = "銷售記錄已成功更新";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                    }
                }

                var products = await _productService.GetAllProductsAsync();
                ViewBag.Products = products;
                return View(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新銷售記錄時發生錯誤: Id={Id}", id);
                TempData["ErrorMessage"] = "更新銷售記錄時發生錯誤。";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Sales/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var sale = await _salesService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    TempData["ErrorMessage"] = "找不到指定的銷售記錄。";
                    return RedirectToAction(nameof(Index));
                }

                return View(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入銷售刪除頁面時發生錯誤: Id={Id}", id);
                TempData["ErrorMessage"] = "載入銷售刪除頁面時發生錯誤。";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Sales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _salesService.DeleteSaleAsync(id);
                if (result)
                {
                    // 嘗試發送 SignalR 更新，但不影響主要功能
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("MetricsUpdated", new
                        {
                            totalRevenue = 0,
                            totalCost = 0,
                            grossProfit = 0,
                            lowStockCount = 0,
                            inventoryValue = 0
                        });
                    }
                    catch (Exception signalREx)
                    {
                        _logger.LogWarning(signalREx, "SignalR 更新失敗，但不影響主要功能");
                    }

                    TempData["SuccessMessage"] = "銷售記錄已成功刪除";
                }
                else
                {
                    TempData["ErrorMessage"] = "刪除銷售記錄時發生錯誤。";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除銷售記錄時發生錯誤: Id={Id}", id);
                TempData["ErrorMessage"] = "刪除銷售記錄時發生錯誤。";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}