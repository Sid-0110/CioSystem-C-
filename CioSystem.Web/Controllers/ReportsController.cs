using Microsoft.AspNetCore.Mvc;
using CioSystem.Services;
using CioSystem.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using CioSystem.Web.Hubs;

namespace CioSystem.Web.Controllers
{
    /// <summary>
    /// 報表分析控制器
    /// </summary>
    public class ReportsController : BaseController
    {
        private new readonly ILogger<ReportsController> _logger;
        private readonly ISalesService _salesService;
        private readonly IPurchasesService _purchasesService;
        private readonly IInventoryService _inventoryService;
        private readonly IProductService _productService;
        // SignalR 已禁用
        // private readonly IHubContext<DashboardHub> _hubContext;

        public ReportsController(
            ILogger<ReportsController> logger,
            ISalesService salesService,
            IPurchasesService purchasesService,
            IInventoryService inventoryService,
            IProductService productService) : base(logger)
        {
            _logger = logger;
            _salesService = salesService;
            _purchasesService = purchasesService;
            _inventoryService = inventoryService;
            _productService = productService;
            // SignalR 已禁用
            // _hubContext = hubContext;
        }

        /// <summary>
        /// 報表分析首頁
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("顯示報表分析首頁");

                // 取得基本統計數據
                _logger.LogInformation("取得銷售數據");
                var sales = await _salesService.GetAllSalesAsync();
                _logger.LogInformation("取得銷售數據完成，共 {Count} 筆", sales.Count());

                _logger.LogInformation("取得進貨數據");
                var purchases = await _purchasesService.GetAllPurchasesAsync();
                _logger.LogInformation("取得進貨數據完成，共 {Count} 筆", purchases.Count());

                _logger.LogInformation("取得產品數據");
                var products = await _productService.GetAllProductsAsync();
                _logger.LogInformation("取得產品數據完成，共 {Count} 筆", products.Count());

                _logger.LogInformation("取得庫存數據");
                var inventory = await _inventoryService.GetAllInventoryAsync();
                _logger.LogInformation("取得庫存數據完成，共 {Count} 筆", inventory.Count());

                // 計算財務概況
                var totalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice);
                var totalCost = purchases.Sum(p => p.Quantity * p.UnitPrice);
                var grossProfit = totalRevenue - totalCost;
                var profitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

                // 計算庫存價值
                var inventoryValue = inventory.Sum(i => i.Quantity * (products.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0));

                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.TotalCost = totalCost;
                ViewBag.GrossProfit = grossProfit;
                ViewBag.ProfitMargin = profitMargin;
                ViewBag.InventoryValue = inventoryValue;
                ViewBag.TotalSales = sales.Count();
                ViewBag.TotalPurchases = purchases.Count();
                ViewBag.TotalProducts = products.Count();

                _logger.LogInformation("報表分析首頁載入完成：Sales={SalesCount}, Purchases={PurchasesCount}, Products={ProductsCount}, InventoryItems={InventoryCount}",
                    sales.Count(), purchases.Count(), products.Count(), inventory.Count());
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入報表分析時發生錯誤（可能為資料來源或服務層問題）");
                TempData["ErrorMessage"] = "載入報表分析時發生錯誤，請稍後再試。";
                return View();
            }
        }

        /// <summary>
        /// 財務概況報表
        /// </summary>
        public async Task<IActionResult> FinancialOverview()
        {
            try
            {
                _logger.LogInformation("顯示財務概況報表");

                // 取得所有數據
                var sales = await _salesService.GetAllSalesAsync();
                var purchases = await _purchasesService.GetAllPurchasesAsync();
                var products = await _productService.GetAllProductsAsync();
                var inventory = await _inventoryService.GetAllInventoryAsync();

                // 計算財務指標
                var totalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice);
                var totalCost = purchases.Sum(p => p.Quantity * p.UnitPrice);
                var grossProfit = totalRevenue - totalCost;
                var profitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

                // 計算庫存價值
                var inventoryValue = inventory.Sum(i => i.Quantity * (products.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0));

                // 計算月度和年度趨勢
                var monthlyRevenue = sales.GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
                    .Select(g => new {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(s => s.Quantity * s.UnitPrice),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();

                var monthlyCost = purchases.GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                    .Select(g => new {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Cost = g.Sum(p => p.Quantity * p.UnitPrice),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();

                // 計算產品銷售排行
                var topSellingProducts = sales.GroupBy(s => s.ProductId)
                    .Select(g => new {
                        ProductId = g.Key,
                        ProductName = products.FirstOrDefault(p => p.Id == g.Key)?.Name ?? "未知產品",
                        ProductSKU = products.FirstOrDefault(p => p.Id == g.Key)?.SKU ?? "N/A",
                        TotalQuantity = g.Sum(s => s.Quantity),
                        TotalRevenue = g.Sum(s => s.Quantity * s.UnitPrice),
                        SalesCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .Take(10)
                    .Cast<object>()
                    .ToList();

                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.TotalCost = totalCost;
                ViewBag.GrossProfit = grossProfit;
                ViewBag.ProfitMargin = profitMargin;
                ViewBag.InventoryValue = inventoryValue;
                ViewBag.MonthlyRevenue = monthlyRevenue;
                ViewBag.MonthlyCost = monthlyCost;
                ViewBag.TopSellingProducts = topSellingProducts;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "顯示財務概況報表時發生錯誤");
                TempData["ErrorMessage"] = "載入財務概況時發生錯誤，請稍後再試。";
                return View();
            }
        }

        /// <summary>
        /// 銷售報表
        /// </summary>
        public async Task<IActionResult> SalesReport()
        {
            try
            {
                _logger.LogInformation("顯示銷售報表");

                var sales = await _salesService.GetAllSalesAsync();
                var products = await _productService.GetAllProductsAsync();

                // 銷售趨勢分析
                var salesTrend = sales.GroupBy(s => s.CreatedAt.Date)
                    .Select(g => new {
                        Date = g.Key,
                        Revenue = g.Sum(s => s.Quantity * s.UnitPrice),
                        Quantity = g.Sum(s => s.Quantity),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // 產品銷售分析
                var productSales = sales.GroupBy(s => s.ProductId)
                    .Select(g => new {
                        ProductId = g.Key,
                        ProductName = products.FirstOrDefault(p => p.Id == g.Key)?.Name ?? "未知產品",
                        ProductSKU = products.FirstOrDefault(p => p.Id == g.Key)?.SKU ?? "N/A",
                        TotalQuantity = g.Sum(s => s.Quantity),
                        TotalRevenue = g.Sum(s => s.Quantity * s.UnitPrice),
                        AveragePrice = g.Average(s => s.UnitPrice),
                        SalesCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .Cast<object>()
                    .ToList();

                // 客戶分析
                var customerAnalysis = sales.Where(s => !string.IsNullOrEmpty(s.CustomerName))
                    .GroupBy(s => s.CustomerName)
                    .Select(g => new {
                        CustomerName = g.Key,
                        TotalPurchases = g.Sum(s => s.Quantity * s.UnitPrice),
                        PurchaseCount = g.Count(),
                        LastPurchase = g.Max(s => s.CreatedAt)
                    })
                    .OrderByDescending(x => x.TotalPurchases)
                    .Cast<object>()
                    .ToList();

                ViewBag.SalesTrend = salesTrend;
                ViewBag.ProductSales = productSales;
                ViewBag.CustomerAnalysis = customerAnalysis;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "顯示銷售報表時發生錯誤");
                TempData["ErrorMessage"] = "載入銷售報表時發生錯誤，請稍後再試。";
                return View();
            }
        }

        /// <summary>
        /// 庫存報表
        /// </summary>
        public async Task<IActionResult> InventoryReport()
        {
            try
            {
                _logger.LogInformation("顯示庫存報表");

                var inventory = await _inventoryService.GetAllInventoryAsync();
                var products = await _productService.GetAllProductsAsync();

                // 空值保護
                inventory ??= new List<Inventory>();
                products ??= new List<Product>();

                // 庫存狀態分析
                var inventoryStatus = inventory.GroupBy(i => i.Status)
                    .Select(g => new {
                        Status = g.Key.ToString(),
                        Count = g.Count(),
                        TotalQuantity = g.Sum(i => i.Quantity),
                        TotalValue = g.Sum(i => i.Quantity * (products.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0))
                    })
                    .Cast<object>()
                    .ToList();

                // 庫存價值分析
                var inventoryValue = inventory.Select(i => new {
                    ProductId = i.ProductId,
                    ProductName = products.FirstOrDefault(p => p.Id == i.ProductId)?.Name ?? "未知產品",
                    ProductSKU = products.FirstOrDefault(p => p.Id == i.ProductId)?.SKU ?? "N/A",
                    Quantity = i.Quantity,
                    UnitPrice = products.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0,
                    TotalValue = i.Quantity * (products.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0),
                    Status = i.Status.ToString()
                })
                .OrderByDescending(x => x.TotalValue)
                .Cast<object>()
                .ToList();

                // 低庫存警告
                var lowStockItems = inventory.Where(i => i.Status == InventoryStatus.LowStock || i.Status == InventoryStatus.OutOfStock)
                    .Select(i => new {
                        ProductId = i.ProductId,
                        ProductName = products.FirstOrDefault(p => p.Id == i.ProductId)?.Name ?? "未知產品",
                        ProductSKU = products.FirstOrDefault(p => p.Id == i.ProductId)?.SKU ?? "N/A",
                        Quantity = i.Quantity,
                        SafetyStock = i.SafetyStock,
                        Status = i.Status.ToString()
                    })
                    .Cast<object>()
                    .ToList();

                ViewBag.InventoryStatus = inventoryStatus;
                ViewBag.InventoryValue = inventoryValue;
                ViewBag.LowStockItems = lowStockItems;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "顯示庫存報表時發生錯誤");
                TempData["ErrorMessage"] = "載入庫存報表時發生錯誤，請稍後再試。";
                return View();
            }
        }

        /// <summary>
        /// 進貨報表
        /// </summary>
        public async Task<IActionResult> PurchasesReport()
        {
            try
            {
                _logger.LogInformation("顯示進貨報表");

                var purchases = await _purchasesService.GetAllPurchasesAsync();
                var products = await _productService.GetAllProductsAsync();

                // 進貨趨勢分析
                var purchaseTrend = purchases.GroupBy(p => p.CreatedAt.Date)
                    .Select(g => new {
                        Date = g.Key,
                        Cost = g.Sum(p => p.Quantity * p.UnitPrice),
                        Quantity = g.Sum(p => p.Quantity),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // 產品進貨分析
                var productPurchases = purchases.GroupBy(p => p.ProductId)
                    .Select(g => new {
                        ProductId = g.Key,
                        ProductName = products.FirstOrDefault(pr => pr.Id == g.Key)?.Name ?? "未知產品",
                        ProductSKU = products.FirstOrDefault(pr => pr.Id == g.Key)?.SKU ?? "N/A",
                        TotalQuantity = g.Sum(p => p.Quantity),
                        TotalCost = g.Sum(p => p.Quantity * p.UnitPrice),
                        AveragePrice = g.Average(p => p.UnitPrice),
                        PurchaseCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalCost)
                    .Cast<object>()
                    .ToList();

                // 供應商分析
                var supplierAnalysis = purchases.Where(p => !string.IsNullOrEmpty(p.Supplier))
                    .GroupBy(p => p.Supplier)
                    .Select(g => new {
                        Supplier = g.Key,
                        TotalCost = g.Sum(p => p.Quantity * p.UnitPrice),
                        PurchaseCount = g.Count(),
                        LastPurchase = g.Max(p => p.CreatedAt)
                    })
                    .OrderByDescending(x => x.TotalCost)
                    .Cast<object>()
                    .ToList();

                ViewBag.PurchaseTrend = purchaseTrend;
                ViewBag.ProductPurchases = productPurchases;
                ViewBag.SupplierAnalysis = supplierAnalysis;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "顯示進貨報表時發生錯誤");
                TempData["ErrorMessage"] = "載入進貨報表時發生錯誤，請稍後再試。";
                return View();
            }
        }
    }
}