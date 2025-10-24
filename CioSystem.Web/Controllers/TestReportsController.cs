using Microsoft.AspNetCore.Mvc;
using CioSystem.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CioSystem.Web.Controllers
{
    /// <summary>
    /// 測試報表控制器 - 用於診斷問題
    /// </summary>
    public class TestReportsController : BaseController
    {
        private readonly ISalesService _salesService;
        private readonly IPurchasesService _purchasesService;
        private readonly IInventoryService _inventoryService;
        private readonly IProductService _productService;

        public TestReportsController(
            ILogger<TestReportsController> logger,
            ISalesService salesService,
            IPurchasesService purchasesService,
            IInventoryService inventoryService,
            IProductService productService) : base(logger)
        {
            _salesService = salesService;
            _purchasesService = purchasesService;
            _inventoryService = inventoryService;
            _productService = productService;
        }

        /// <summary>
        /// 測試報表數據載入
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                LogOperationStart("測試報表數據載入");

                // 測試各個服務
                var sales = await _salesService.GetAllSalesAsync();
                LogOperationComplete("銷售數據載入", $"共 {sales.Count()} 筆");

                var purchases = await _purchasesService.GetAllPurchasesAsync();
                LogOperationComplete("進貨數據載入", $"共 {purchases.Count()} 筆");

                var products = await _productService.GetAllProductsAsync();
                LogOperationComplete("產品數據載入", $"共 {products.Count()} 筆");

                var inventory = await _inventoryService.GetAllInventoryAsync();
                LogOperationComplete("庫存數據載入", $"共 {inventory.Count()} 筆");

                // 計算基本統計
                var totalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice);
                var totalCost = purchases.Sum(p => p.Quantity * p.UnitPrice);
                var grossProfit = totalRevenue - totalCost;

                var result = new
                {
                    SalesCount = sales.Count(),
                    PurchasesCount = purchases.Count(),
                    ProductsCount = products.Count(),
                    InventoryCount = inventory.Count(),
                    TotalRevenue = totalRevenue,
                    TotalCost = totalCost,
                    GrossProfit = grossProfit,
                    Success = true
                };

                LogOperationComplete("測試報表數據載入", "成功");
                return Json(result);
            }
            catch (Exception ex)
            {
                LogOperationComplete("測試報表數據載入", $"失敗: {ex.Message}");
                return Json(new { Success = false, Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }
    }
}