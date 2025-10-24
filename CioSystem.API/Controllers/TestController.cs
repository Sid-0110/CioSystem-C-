using Microsoft.AspNetCore.Mvc;
using CioSystem.Core;
using CioSystem.Models;
using CioSystem.Services;

namespace CioSystem.API.Controllers
{
    /// <summary>
    /// 測試控制器 - 用於驗證資料庫連接和基本功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TestController> _logger;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="logger">日誌記錄器</param>
        public TestController(IUnitOfWork unitOfWork, ILogger<TestController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// 測試資料庫連接
        /// </summary>
        /// <returns>測試結果</returns>
        [HttpGet("database")]
        public async Task<ActionResult<object>> TestDatabase()
        {
            try
            {
                // 測試取得所有產品
                var products = await _unitOfWork.GetRepository<Product>().GetAllAsync();
                
                // 測試取得所有庫存
                var inventory = await _unitOfWork.GetRepository<Inventory>().GetAllAsync();

                return Ok(new
                {
                    Status = "成功",
                    Message = "資料庫連接正常",
                    Timestamp = DateTime.Now,
                    Data = new
                    {
                        ProductsCount = products.Count(),
                        InventoryCount = inventory.Count(),
                        Products = products.Take(5), // 只返回前5筆產品資料
                        Inventory = inventory.Take(5) // 只返回前5筆庫存資料
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫連接測試失敗");
                return StatusCode(500, new
                {
                    Status = "錯誤",
                    Message = "資料庫連接失敗",
                    Error = ex.Message,
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 測試建立新產品
        /// </summary>
        /// <param name="productName">產品名稱</param>
        /// <returns>測試結果</returns>
        [HttpPost("create-product")]
        public async Task<ActionResult<object>> CreateTestProduct([FromQuery] string productName = "測試產品")
        {
            try
            {
                var product = new Product
                {
                    Name = productName,
                    Description = "這是一個測試產品",
                    Price = 99.99m,
                    Category = "測試類別",
                    SKU = $"TEST-{DateTime.Now:yyyyMMdd-HHmmss}",
                    Brand = "測試品牌",
                    Status = ProductStatus.Active,
                    MinStockLevel = 10,
                    CreatedBy = "TestController",
                    UpdatedBy = "TestController"
                };

                var createdProduct = await _unitOfWork.GetRepository<Product>().AddAsync(product);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    Status = "成功",
                    Message = "測試產品建立成功",
                    Timestamp = DateTime.Now,
                    Data = createdProduct
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立測試產品失敗");
                return StatusCode(500, new
                {
                    Status = "錯誤",
                    Message = "建立測試產品失敗",
                    Error = ex.Message,
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 測試建立新庫存
        /// </summary>
        /// <param name="productId">產品ID</param>
        /// <param name="quantity">數量</param>
        /// <returns>測試結果</returns>
        [HttpPost("create-inventory")]
        public async Task<ActionResult<object>> CreateTestInventory(
            [FromQuery] int productId = 1,
            [FromQuery] int quantity = 100,
            [FromQuery] string location = "測試倉庫-A區")
        {
            try
            {
                var inventory = new Inventory
                {
                    ProductId = productId,
                    Quantity = quantity,
                    Type = InventoryType.Stock,
                    Status = InventoryStatus.Normal,
                    CreatedBy = "TestController",
                    UpdatedBy = "TestController"
                };

                var createdInventory = await _unitOfWork.GetRepository<Inventory>().AddAsync(inventory);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    Status = "成功",
                    Message = "測試庫存建立成功",
                    Timestamp = DateTime.Now,
                    Data = createdInventory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立測試庫存失敗");
                return StatusCode(500, new
                {
                    Status = "錯誤",
                    Message = "建立測試庫存失敗",
                    Error = ex.Message,
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 取得系統資訊
        /// </summary>
        /// <returns>系統資訊</returns>
        [HttpGet("system-info")]
        public ActionResult<object> GetSystemInfo()
        {
            return Ok(new
            {
                Status = "成功",
                Message = "系統資訊",
                Timestamp = DateTime.Now,
                Data = new
                {
                    ApplicationName = "CioSystem v1 (C#) - 學習建構模式專案",
                    Version = "1.0.0",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    MachineName = Environment.MachineName,
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    DatabaseProvider = "SQLite",
                    EntityFrameworkVersion = "9.0.9",
                    DotNetVersion = Environment.Version.ToString()
                }
            });
        }
    }
}