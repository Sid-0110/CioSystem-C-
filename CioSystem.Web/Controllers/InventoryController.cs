using CioSystem.Models;
using CioSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using CioSystem.Web.Hubs;

namespace CioSystem.Web.Controllers
{
    /// <summary>
    /// 庫存與產品的組合 ViewModel
    /// </summary>
    public class InventoryWithProduct
    {
        public Inventory Inventory { get; set; } = null!;
        public Product? Product { get; set; }
    }

    /// <summary>
    /// 庫存管理控制器
    /// 提供庫存相關的 Web 介面
    /// </summary>
    public class InventoryController : BaseController
    {
        private readonly IInventoryService _inventoryService;
        private readonly IProductService _productService;
        private new readonly ILogger<InventoryController> _logger;
        // SignalR 已禁用
        // private readonly IHubContext<DashboardHub> _hubContext;

        public InventoryController(
            IInventoryService inventoryService,
            IProductService productService,
            ILogger<InventoryController> logger) : base(logger)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // SignalR 已禁用
            // _hubContext = hubContext;
        }

        /// <summary>
        /// 庫存列表頁面
        /// </summary>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="productId">產品 ID</param>
        /// <param name="productSKU">產品編號</param>
        /// <param name="status">狀態</param>
        /// <returns>庫存列表視圖</returns>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, int? productId = null, string? productSKU = null, InventoryStatus? status = null)
        {
            try
            {
                _logger.LogInformation("顯示庫存列表頁面: Page={Page}, PageSize={PageSize}, ProductId={ProductId}, ProductSKU={ProductSKU}, Status={Status}",
                    page, pageSize, productId, productSKU, status);

                // ✅ 優化：使用快取的統計資料，避免重複查詢
                var statisticsTask = _inventoryService.GetInventoryStatisticsAsync();

                // ✅ 優化：直接使用分頁查詢，避免記憶體分頁
                var inventoryTask = _inventoryService.GetInventoryPagedAsync(page, pageSize, productId, productSKU, status);

                // 並行執行查詢
                await Task.WhenAll(statisticsTask, inventoryTask);
                var statistics = await statisticsTask;
                var inventory = await inventoryTask;

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.ProductId = productId;
                ViewBag.ProductSKU = productSKU;
                ViewBag.Status = status;
                var totalCount = inventory.TotalCount;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // ✅ 優化：使用快取的產品清單
                var products = await _productService.GetAllProductsAsync();
                products ??= new List<Product>();
                ViewBag.Products = products;

                // ✅ 優化：使用預計算的統計資料
                ViewBag.TotalInventoryItems = statistics.TotalItems;
                ViewBag.NormalInventory = statistics.AvailableItems;
                ViewBag.LowStockInventory = statistics.LowStockItems;
                ViewBag.OutOfStockInventory = statistics.UnavailableItems;
                ViewBag.ExcessInventory = 0; // 暫時設為0，需要根據實際業務邏輯實現

                // 創建一個包含產品信息的庫存列表
                var inventoryWithProducts = new List<InventoryWithProduct>();
                var pagedInventory = inventory.Inventory ?? Enumerable.Empty<Inventory>();
                foreach (var inv in pagedInventory.OrderBy(x => x.Id))
                {
                    var product = products.FirstOrDefault(p => p.Id == inv.ProductId);
                    inventoryWithProducts.Add(new InventoryWithProduct
                    {
                        Inventory = inv,
                        Product = product
                    });
                }

                return View(inventoryWithProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得庫存列表時發生錯誤");
                TempData["ErrorMessage"] = "載入庫存列表時發生錯誤，請稍後再試。";
                return View(new List<InventoryWithProduct>());
            }
        }

        /// <summary>
        /// 新增庫存頁面 (GET)
        /// </summary>
        /// <returns>新增庫存視圖</returns>
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
                _logger.LogError(ex, "載入新增庫存頁面時發生錯誤");
                TempData["ErrorMessage"] = "載入頁面時發生錯誤，請稍後再試。";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 新增庫存 (POST)
        /// </summary>
        /// <param name="inventory">庫存資料</param>
        /// <returns>重定向到庫存列表或顯示錯誤</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Quantity,ProductSKU,SafetyStock,ReservedQuantity,Status,Type,ProductionDate,Notes")] Inventory inventory)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    var products = await _productService.GetAllProductsAsync();
                    // View 端期待 List<Product>，因此統一提供 List 而非 SelectList
                    ViewBag.Products = products;
                }
                catch
                {
                    // 忽略載入產品清單的錯誤
                }
                return View(inventory);
            }

            try
            {
                // 自動設置產品編號
                if (inventory.ProductId > 0 && string.IsNullOrEmpty(inventory.ProductSKU))
                {
                    var product = await _productService.GetProductByIdAsync(inventory.ProductId);
                    if (product != null)
                    {
                        inventory.ProductSKU = product.SKU;
                    }
                }

                // 自動計算庫存狀態
                inventory.Status = inventory.CalculateStatus();

                _logger.LogInformation("建立新庫存: ProductId={ProductId}, Quantity={Quantity}, SafetyStock={SafetyStock}",
                    inventory.ProductId, inventory.Quantity, inventory.SafetyStock);

                var createdInventory = await _inventoryService.CreateInventoryAsync(inventory);

                // 推送即時統計更新
                try
                {
                    var sales = await _productService.GetAllProductsAsync(); // 佔位，不取用
                    var products2 = await _productService.GetAllProductsAsync();
                    var inventory2 = await _inventoryService.GetAllInventoryAsync();
                    var allSales = await _inventoryService.GetAllInventoryAsync(); // 佔位，不取用
                    var totalRevenue = 0m; // 在庫存異動中不計入
                    var totalCost = 0m;
                    var grossProfit = totalRevenue - totalCost;
                    var lowStockCount = inventory2.Count(i => i.SafetyStock > 0 && i.Quantity <= i.SafetyStock);
                    var inventoryValue = inventory2.Sum(i => i.Quantity * (products2.FirstOrDefault(p => p.Id == i.ProductId)?.Price ?? 0));
                    // SignalR 已禁用
                    // await _hubContext.Clients.All.SendAsync("MetricsUpdated", new {"MetricsUpdated", new
                    //{
                        //totalRevenue,
                        //totalCost,
                        //grossProfit,
                        //lowStockCount,
                        //inventoryValue
                    //});
                }
                catch { }

                TempData["SuccessMessage"] = $"庫存建立成功！";
                return RedirectToAction(nameof(Index), new { page = 1, pageSize = 10 });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "建立庫存時發生參數錯誤");
                ModelState.AddModelError("", ex.Message);

                try
                {
                    var products = await _productService.GetAllProductsAsync();
                    ViewBag.Products = new SelectList(products, "Id", "Name", inventory.ProductId);
                }
                catch
                {
                    // 忽略載入產品清單的錯誤
                }
                return View(inventory);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "建立庫存時發生無效操作");
                ModelState.AddModelError("", ex.Message);

                try
                {
                    var products = await _productService.GetAllProductsAsync();
                    ViewBag.Products = new SelectList(products, "Id", "Name", inventory.ProductId);
                }
                catch
                {
                    // 忽略載入產品清單的錯誤
                }
                return View(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立庫存時發生錯誤");
                ModelState.AddModelError("", "建立庫存時發生錯誤，請稍後再試。");

                try
                {
                    var products = await _productService.GetAllProductsAsync();
                    ViewBag.Products = new SelectList(products, "Id", "Name", inventory.ProductId);
                }
                catch
                {
                    // 忽略載入產品清單的錯誤
                }
                return View(inventory);
            }
        }

        /// <summary>
        /// 庫存詳情頁面
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <returns>庫存詳情視圖</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(id.Value);
                if (inventory == null)
                {
                    return NotFound();
                }

                // 載入產品資訊供視圖顯示名稱等
                var product = await _productService.GetProductByIdAsync(inventory.ProductId);
                if (product != null)
                {
                    inventory.Product = product;
                }

                return View(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入庫存詳情時發生錯誤: InventoryId={InventoryId}", id);
                TempData["ErrorMessage"] = "載入庫存詳情時發生錯誤，請稍後再試。";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 庫存編輯頁面
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <returns>庫存編輯視圖</returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(id.Value);
                if (inventory == null)
                {
                    return NotFound();
                }

                // 載入產品清單供下拉選單使用
                var products = await _productService.GetAllProductsAsync();
                ViewBag.Products = new SelectList(products, "Id", "Name", inventory.ProductId);

                // 載入目前產品資訊，供右側欄顯示名稱
                var product = products.FirstOrDefault(p => p.Id == inventory.ProductId);
                if (product != null)
                {
                    inventory.Product = product;
                }

                return View(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入庫存編輯頁面時發生錯誤: InventoryId={InventoryId}", id);
                TempData["ErrorMessage"] = "載入庫存編輯頁面時發生錯誤，請稍後再試。";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 庫存編輯處理
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <param name="inventory">庫存資料</param>
        /// <returns>重定向到庫存列表或編輯頁面</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductId,Quantity,ProductSKU,SafetyStock,ReservedQuantity,Status,Type,Notes,EmployeeRetention")] Inventory inventory)
        {
            if (id != inventory.Id)
            {
                _logger.LogWarning("庫存編輯 ID 不匹配: 請求ID={RequestId}, 庫存ID={InventoryId}", id, inventory.Id);
                return NotFound();
            }

            // 導覽屬性不由表單提交，避免 ModelState 要求 Product 導致驗證失敗
            ModelState.Remove("Product");

            // 詳細記錄 ModelState 錯誤
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("庫存編輯表單驗證失敗: InventoryId={InventoryId}, 錯誤數量={ErrorCount}",
                    id, ModelState.ErrorCount);

                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        _logger.LogWarning("驗證錯誤 - {Key}: {Errors}",
                            error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                try
                {
                    var products = await _productService.GetAllProductsAsync();
                    ViewBag.Products = new SelectList(products, "Id", "Name", inventory.ProductId);

                    // 載入目前產品資訊，供右側欄顯示名稱
                    var product = products.FirstOrDefault(p => p.Id == inventory.ProductId);
                    if (product != null)
                    {
                        inventory.Product = product;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "載入產品清單時發生錯誤");
                    ModelState.AddModelError("", "載入產品清單時發生錯誤，請稍後再試。");
                }

                return View(inventory);
            }

            try
            {
                // 驗證預留數量不能超過總庫存
                if (inventory.ReservedQuantity > inventory.Quantity)
                {
                    ModelState.AddModelError("ReservedQuantity", "預留數量不能超過總庫存數量");
                    var products = await _productService.GetAllProductsAsync();
                    ViewBag.Products = new SelectList(products, "Id", "Name", inventory.ProductId);
                    var product = products.FirstOrDefault(p => p.Id == inventory.ProductId);
                    if (product != null)
                    {
                        inventory.Product = product;
                    }
                    return View(inventory);
                }

                // 自動設置產品編號
                if (inventory.ProductId > 0 && string.IsNullOrEmpty(inventory.ProductSKU))
                {
                    var product = await _productService.GetProductByIdAsync(inventory.ProductId);
                    if (product != null)
                    {
                        inventory.ProductSKU = product.SKU;
                        _logger.LogInformation("自動設置產品編號: ProductId={ProductId}, SKU={SKU}",
                            inventory.ProductId, inventory.ProductSKU);
                    }
                }

                // 自動計算庫存狀態
                var calculatedStatus = inventory.CalculateStatus();
                inventory.Status = calculatedStatus;

                _logger.LogInformation("開始更新庫存: InventoryId={InventoryId}, ProductId={ProductId}, Quantity={Quantity}, Status={Status}",
                    id, inventory.ProductId, inventory.Quantity, inventory.Status);

                await _inventoryService.UpdateInventoryAsync(id, inventory);

                _logger.LogInformation("庫存更新成功: InventoryId={InventoryId}", id);
                TempData["SuccessMessage"] = "庫存更新成功！";

                _logger.LogInformation("準備重定向到庫存列表頁面");
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "更新庫存時發生參數錯誤: InventoryId={InventoryId}", id);
                ModelState.AddModelError("", ex.Message);

                try
                {
                    var products = await _productService.GetAllProductsAsync();
                    ViewBag.Products = new SelectList(products, "Id", "Name", inventory.ProductId);
                    var product = products.FirstOrDefault(p => p.Id == inventory.ProductId);
                    if (product != null)
                    {
                        inventory.Product = product;
                    }
                }
                catch
                {
                    // 忽略載入產品清單的錯誤
                }
                return View(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新庫存時發生錯誤: InventoryId={InventoryId}", id);
                TempData["ErrorMessage"] = "更新庫存時發生錯誤，請稍後再試。";

                try
                {
                    var products = await _productService.GetAllProductsAsync();
                    ViewBag.Products = new SelectList(products, "Id", "Name", inventory.ProductId);
                    var product = products.FirstOrDefault(p => p.Id == inventory.ProductId);
                    if (product != null)
                    {
                        inventory.Product = product;
                    }
                }
                catch
                {
                    // 忽略載入產品清單的錯誤
                }
                return View(inventory);
            }
        }

        /// <summary>
        /// 庫存刪除確認頁面
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <returns>庫存刪除確認視圖</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(id.Value);
                if (inventory == null)
                {
                    return NotFound();
                }

                return View(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入庫存刪除頁面時發生錯誤: InventoryId={InventoryId}", id);
                TempData["ErrorMessage"] = "載入庫存刪除頁面時發生錯誤，請稍後再試。";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 庫存刪除處理
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <returns>重定向到庫存列表</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _inventoryService.DeleteInventoryAsync(id);
                TempData["SuccessMessage"] = "庫存刪除成功！";
                return RedirectToAction(nameof(Index), new { page = 1, pageSize = 10 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除庫存時發生錯誤: InventoryId={InventoryId}", id);
                TempData["ErrorMessage"] = "刪除庫存時發生錯誤，請稍後再試。";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 批量更新安全庫存
        /// </summary>
        /// <param name="safetyStock">安全庫存數量</param>
        /// <returns>重定向到庫存列表</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchUpdateSafetyStock(int safetyStock)
        {
            try
            {
                _logger.LogInformation("開始批量更新安全庫存: SafetyStock={SafetyStock}", safetyStock);

                // 獲取所有庫存記錄
                var inventories = await _inventoryService.GetAllInventoryAsync();
                var updatedCount = 0;

                foreach (var inventory in inventories)
                {
                    if (inventory.SafetyStock != safetyStock)
                    {
                        inventory.SafetyStock = safetyStock;
                        await _inventoryService.UpdateInventoryAsync(inventory.Id, inventory);
                        updatedCount++;
                    }
                }

                _logger.LogInformation("批量更新安全庫存完成: UpdatedCount={UpdatedCount}", updatedCount);
                TempData["SuccessMessage"] = $"成功更新 {updatedCount} 筆庫存記錄的安全庫存為 {safetyStock}。";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新安全庫存時發生錯誤: SafetyStock={SafetyStock}", safetyStock);
                TempData["ErrorMessage"] = "批量更新安全庫存時發生錯誤，請稍後再試。";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 取得庫存移動記錄 (AJAX)
        /// </summary>
        /// <param name="productId">產品ID（可選）</param>
        /// <param name="startDate">開始日期（可選）</param>
        /// <param name="endDate">結束日期（可選）</param>
        /// <returns>庫存移動記錄 JSON</returns>
        [HttpGet]
        public async Task<IActionResult> GetMovements(int? productId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var movements = await _inventoryService.GetInventoryMovementsAsync(null, startDate, endDate);

                // 如果指定了產品ID，需要先找到對應的庫存ID
                if (productId.HasValue)
                {
                    var inventory = await _inventoryService.GetInventoryByProductIdAsync(productId.Value);
                    if (inventory != null)
                    {
                        movements = movements.Where(m => m.InventoryId == inventory.Id);
                    }
                    else
                    {
                        movements = new List<InventoryMovement>();
                    }
                }

                // 取得產品資訊
                var products = await _productService.GetAllProductsAsync();
                var productMap = products.ToDictionary(p => p.Id, p => new { Name = p.Name, SKU = p.SKU });

                // 取得庫存資訊
                var allInventory = await _inventoryService.GetAllInventoryAsync();
                var inventoryMap = allInventory.ToDictionary(i => i.Id, i => i.ProductId);

                var result = movements.Select(m => new
                {
                    id = m.Id,
                    inventoryId = m.InventoryId,
                    movementType = (int)m.Type,
                    quantityChange = m.Type == MovementType.Outbound ? -m.Quantity : m.Quantity,
                    previousQuantity = m.PreviousQuantity,
                    newQuantity = m.NewQuantity,
                    reason = m.Reason,
                    createdAt = m.CreatedAt,
                    createdBy = m.CreatedBy,
                    productName = inventoryMap.ContainsKey(m.InventoryId) && productMap.ContainsKey(inventoryMap[m.InventoryId])
                        ? productMap[inventoryMap[m.InventoryId]].Name
                        : "未知產品",
                    productSKU = inventoryMap.ContainsKey(m.InventoryId) && productMap.ContainsKey(inventoryMap[m.InventoryId])
                        ? productMap[inventoryMap[m.InventoryId]].SKU
                        : "N/A"
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得庫存移動記錄時發生錯誤: ProductId={ProductId}, StartDate={StartDate}, EndDate={EndDate}",
                    productId, startDate, endDate);
                return Json(new { error = "載入庫存移動記錄時發生錯誤" });
            }
        }
    }
}