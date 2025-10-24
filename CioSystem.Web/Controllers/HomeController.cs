using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CioSystem.Web.Models;
using CioSystem.Services;

namespace CioSystem.Web.Controllers;

[Authorize]
public class HomeController : BaseController
{
    private readonly IProductService _productService;
    private readonly IInventoryService _inventoryService;

    public HomeController(ILogger<HomeController> logger, IProductService productService, IInventoryService inventoryService)
        : base(logger)
    {
        _productService = productService;
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // 載入統計資料
            var productStats = await _productService.GetProductStatisticsAsync();
            var inventoryStats = await _inventoryService.GetInventoryStatisticsAsync();

            ViewBag.ProductStats = productStats;
            ViewBag.InventoryStats = inventoryStats;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入首頁統計資料時發生錯誤");
            return View();
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
