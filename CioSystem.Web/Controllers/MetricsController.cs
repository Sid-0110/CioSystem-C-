using CioSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace CioSystem.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly CioSystem.Web.Services.IMetricsService _metrics;

        public MetricsController(CioSystem.Web.Services.IMetricsService metrics)
        {
            _metrics = metrics;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var s = await _metrics.GetSummaryAsync(from, to);
            return Ok(new { totalRevenue = s.TotalRevenue, totalCost = s.TotalCost, grossProfit = s.GrossProfit, lowStockCount = s.LowStockCount, inventoryValue = s.InventoryValue });
        }

        [HttpGet("sales-trend")]
        public async Task<IActionResult> GetSalesTrend([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string granularity = "day")
        {
            var points = await _metrics.GetSalesTrendAsync(from, to, granularity);
            return Ok(points.Select(p => new { date = p.Date, quantity = p.Quantity, amount = p.Amount }));
        }

        [HttpGet("inventory-status")]
        public async Task<IActionResult> GetInventoryStatus()
        {
            var list = await _metrics.GetInventoryStatusAsync();
            return Ok(list);
        }

        [HttpGet("inventory-top-value")]
        public async Task<IActionResult> GetInventoryTopValue([FromQuery] int top = 10)
        {
            var topList = await _metrics.GetInventoryTopValueAsync(top);
            return Ok(topList);
        }

        [HttpGet("product-sales")]
        public async Task<IActionResult> GetProductSales([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] int top = 50)
        {
            var list = await _metrics.GetProductSalesAsync(from, to, top);
            return Ok(list.Select(p => new {
                productId = p.ProductId,
                productName = p.ProductName,
                productSKU = p.ProductSKU,
                totalQuantity = p.TotalQuantity,
                totalRevenue = p.TotalRevenue,
                averagePrice = p.AveragePrice,
                salesCount = p.SalesCount
            }));
        }
    }
}

