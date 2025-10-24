using Microsoft.AspNetCore.Mvc;
using CioSystem.Services;
using CioSystem.Models;
using System.Text;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CioSystem.Web.Controllers
{
    public class ExportImportController : Controller
    {
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private readonly ISalesService _salesService;
        private readonly IPurchasesService _purchasesService;
        private readonly ILogger<ExportImportController> _logger;

        public ExportImportController(
            IProductService productService,
            IInventoryService inventoryService,
            ISalesService salesService,
            IPurchasesService purchasesService,
            ILogger<ExportImportController> logger)
        {
            _productService = productService;
            _inventoryService = inventoryService;
            _salesService = salesService;
            _purchasesService = purchasesService;
            _logger = logger;
        }

        /// <summary>
        /// 匯出所有資料到 Excel
        /// </summary>
        public async Task<IActionResult> ExportAll()
        {
            try
            {
                _logger.LogInformation("開始匯出所有資料到 Excel");

                // 取得所有資料
                var products = await _productService.GetAllProductsAsync();
                var inventory = await _inventoryService.GetAllInventoryAsync();
                var sales = await _salesService.GetAllSalesAsync();
                var purchases = await _purchasesService.GetAllPurchasesAsync();

                // 建立 Excel 工作簿
                using var workbook = new XLWorkbook();

                // 產品資料工作表
                var productsSheet = workbook.Worksheets.Add("產品資料");
                productsSheet.Cell(1, 1).Value = "ID";
                productsSheet.Cell(1, 2).Value = "產品名稱";
                productsSheet.Cell(1, 3).Value = "產品編號";
                productsSheet.Cell(1, 4).Value = "品牌";
                productsSheet.Cell(1, 5).Value = "類別";
                productsSheet.Cell(1, 6).Value = "顏色";
                productsSheet.Cell(1, 7).Value = "價格";
                productsSheet.Cell(1, 8).Value = "成本價";
                productsSheet.Cell(1, 9).Value = "狀態";
                productsSheet.Cell(1, 10).Value = "最小庫存";
                productsSheet.Cell(1, 11).Value = "最大庫存";
                productsSheet.Cell(1, 12).Value = "描述";
                productsSheet.Cell(1, 13).Value = "建立時間";
                productsSheet.Cell(1, 14).Value = "更新時間";

                var row = 2;
                foreach (var product in products)
                {
                    productsSheet.Cell(row, 1).Value = product.Id;
                    productsSheet.Cell(row, 2).Value = product.Name;
                    productsSheet.Cell(row, 3).Value = product.SKU;
                    productsSheet.Cell(row, 4).Value = product.Brand;
                    productsSheet.Cell(row, 5).Value = product.Category;
                    productsSheet.Cell(row, 6).Value = product.Color;
                    productsSheet.Cell(row, 7).Value = product.Price;
                    productsSheet.Cell(row, 8).Value = product.CostPrice;
                    productsSheet.Cell(row, 9).Value = product.Status.ToString();
                    productsSheet.Cell(row, 10).Value = product.MinStockLevel;
                    productsSheet.Cell(row, 11).Value = product.MaxStockLevel;
                    productsSheet.Cell(row, 12).Value = product.Description;
                    productsSheet.Cell(row, 13).Value = product.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    productsSheet.Cell(row, 14).Value = product.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                // 庫存資料工作表
                var inventorySheet = workbook.Worksheets.Add("庫存資料");
                inventorySheet.Cell(1, 1).Value = "ID";
                inventorySheet.Cell(1, 2).Value = "產品ID";
                inventorySheet.Cell(1, 3).Value = "產品名稱";
                inventorySheet.Cell(1, 4).Value = "產品編號";
                inventorySheet.Cell(1, 5).Value = "數量";
                inventorySheet.Cell(1, 6).Value = "安全庫存";
                inventorySheet.Cell(1, 7).Value = "預留數量";
                inventorySheet.Cell(1, 8).Value = "狀態";
                inventorySheet.Cell(1, 9).Value = "類型";
                inventorySheet.Cell(1, 10).Value = "生產日期";
                inventorySheet.Cell(1, 11).Value = "最後盤點日期";
                inventorySheet.Cell(1, 12).Value = "備註";
                inventorySheet.Cell(1, 13).Value = "建立時間";
                inventorySheet.Cell(1, 14).Value = "更新時間";

                row = 2;
                foreach (var item in inventory)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    inventorySheet.Cell(row, 1).Value = item.Id;
                    inventorySheet.Cell(row, 2).Value = item.ProductId;
                    inventorySheet.Cell(row, 3).Value = product?.Name ?? "未知產品";
                    inventorySheet.Cell(row, 4).Value = product?.SKU ?? "N/A";
                    inventorySheet.Cell(row, 5).Value = item.Quantity;
                    inventorySheet.Cell(row, 6).Value = item.SafetyStock;
                    inventorySheet.Cell(row, 7).Value = item.ReservedQuantity;
                    inventorySheet.Cell(row, 8).Value = item.Status.ToString();
                    inventorySheet.Cell(row, 9).Value = item.Type.ToString();
                    inventorySheet.Cell(row, 10).Value = item.ProductionDate.HasValue ? item.ProductionDate.Value.ToString("yyyy-MM-dd") : "";
                    inventorySheet.Cell(row, 11).Value = item.LastCountDate.HasValue ? item.LastCountDate.Value.ToString("yyyy-MM-dd") : "";
                    inventorySheet.Cell(row, 12).Value = item.Notes;
                    inventorySheet.Cell(row, 13).Value = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    inventorySheet.Cell(row, 14).Value = item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                // 銷售資料工作表
                var salesSheet = workbook.Worksheets.Add("銷售資料");
                salesSheet.Cell(1, 1).Value = "ID";
                salesSheet.Cell(1, 2).Value = "產品ID";
                salesSheet.Cell(1, 3).Value = "產品名稱";
                salesSheet.Cell(1, 4).Value = "產品編號";
                salesSheet.Cell(1, 5).Value = "數量";
                salesSheet.Cell(1, 6).Value = "單價";
                salesSheet.Cell(1, 7).Value = "總金額";
                salesSheet.Cell(1, 8).Value = "客戶名稱";
                salesSheet.Cell(1, 9).Value = "建立時間";
                salesSheet.Cell(1, 10).Value = "更新時間";

                row = 2;
                foreach (var sale in sales)
                {
                    var product = products.FirstOrDefault(p => p.Id == sale.ProductId);
                    salesSheet.Cell(row, 1).Value = sale.Id;
                    salesSheet.Cell(row, 2).Value = sale.ProductId;
                    salesSheet.Cell(row, 3).Value = product?.Name ?? "未知產品";
                    salesSheet.Cell(row, 4).Value = product?.SKU ?? "N/A";
                    salesSheet.Cell(row, 5).Value = sale.Quantity;
                    salesSheet.Cell(row, 6).Value = sale.UnitPrice;
                    salesSheet.Cell(row, 7).Value = sale.Quantity * sale.UnitPrice;
                    salesSheet.Cell(row, 8).Value = sale.CustomerName;
                    salesSheet.Cell(row, 9).Value = sale.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    salesSheet.Cell(row, 10).Value = sale.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                // 進貨資料工作表
                var purchasesSheet = workbook.Worksheets.Add("進貨資料");
                purchasesSheet.Cell(1, 1).Value = "ID";
                purchasesSheet.Cell(1, 2).Value = "產品ID";
                purchasesSheet.Cell(1, 3).Value = "產品名稱";
                purchasesSheet.Cell(1, 4).Value = "產品編號";
                purchasesSheet.Cell(1, 5).Value = "數量";
                purchasesSheet.Cell(1, 6).Value = "單價";
                purchasesSheet.Cell(1, 7).Value = "總金額";
                purchasesSheet.Cell(1, 8).Value = "供應商";
                purchasesSheet.Cell(1, 9).Value = "建立時間";
                purchasesSheet.Cell(1, 10).Value = "更新時間";

                row = 2;
                foreach (var purchase in purchases)
                {
                    var product = products.FirstOrDefault(p => p.Id == purchase.ProductId);
                    purchasesSheet.Cell(row, 1).Value = purchase.Id;
                    purchasesSheet.Cell(row, 2).Value = purchase.ProductId;
                    purchasesSheet.Cell(row, 3).Value = product?.Name ?? "未知產品";
                    purchasesSheet.Cell(row, 4).Value = product?.SKU ?? "N/A";
                    purchasesSheet.Cell(row, 5).Value = purchase.Quantity;
                    purchasesSheet.Cell(row, 6).Value = purchase.UnitPrice;
                    purchasesSheet.Cell(row, 7).Value = purchase.Quantity * purchase.UnitPrice;
                    purchasesSheet.Cell(row, 8).Value = purchase.Supplier;
                    purchasesSheet.Cell(row, 9).Value = purchase.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    purchasesSheet.Cell(row, 10).Value = purchase.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                // 設定標題列樣式
                foreach (var worksheet in workbook.Worksheets)
                {
                    var headerRange = worksheet.Range(1, 1, 1, worksheet.LastColumnUsed().ColumnNumber());
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // 自動調整欄寬
                    worksheet.Columns().AdjustToContents();
                }

                // 產生檔案名稱
                var fileName = $"CioSystem_完整資料匯出_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // 轉換為記憶體流
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                _logger.LogInformation("成功匯出所有資料，檔案名稱: {FileName}", fileName);

                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯出資料時發生錯誤");
                TempData["ErrorMessage"] = "匯出資料時發生錯誤，請稍後再試。";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// 匯出範本檔案
        /// </summary>
        public IActionResult ExportTemplate()
        {
            try
            {
                _logger.LogInformation("開始匯出範本檔案");

                using var workbook = new XLWorkbook();

                // 產品資料範本
                var productsSheet = workbook.Worksheets.Add("產品資料範本");
                productsSheet.Cell(1, 1).Value = "產品名稱";
                productsSheet.Cell(1, 2).Value = "產品編號";
                productsSheet.Cell(1, 3).Value = "品牌";
                productsSheet.Cell(1, 4).Value = "類別";
                productsSheet.Cell(1, 5).Value = "顏色";
                productsSheet.Cell(1, 6).Value = "價格";
                productsSheet.Cell(1, 7).Value = "成本價";
                productsSheet.Cell(1, 8).Value = "最小庫存";
                productsSheet.Cell(1, 9).Value = "最大庫存";
                productsSheet.Cell(1, 10).Value = "描述";

                // 範本資料範例
                productsSheet.Cell(2, 1).Value = "範例產品";
                productsSheet.Cell(2, 2).Value = "EXAMPLE-001";
                productsSheet.Cell(2, 3).Value = "範例品牌";
                productsSheet.Cell(2, 4).Value = "服飾";
                productsSheet.Cell(2, 5).Value = "黑色";
                productsSheet.Cell(2, 6).Value = 100;
                productsSheet.Cell(2, 7).Value = 80;
                productsSheet.Cell(2, 8).Value = 10;
                productsSheet.Cell(2, 9).Value = 100;
                productsSheet.Cell(2, 10).Value = "這是範例產品描述";

                // 庫存資料範本
                var inventorySheet = workbook.Worksheets.Add("庫存資料範本");
                inventorySheet.Cell(1, 1).Value = "產品編號";
                inventorySheet.Cell(1, 2).Value = "數量";
                inventorySheet.Cell(1, 3).Value = "安全庫存";
                inventorySheet.Cell(1, 4).Value = "預留數量";
                inventorySheet.Cell(1, 5).Value = "生產日期";
                inventorySheet.Cell(1, 6).Value = "備註";

                inventorySheet.Cell(2, 1).Value = "EXAMPLE-001";
                inventorySheet.Cell(2, 2).Value = 50;
                inventorySheet.Cell(2, 3).Value = 10;
                inventorySheet.Cell(2, 4).Value = 0;
                inventorySheet.Cell(2, 5).Value = "2024-01-01";
                inventorySheet.Cell(2, 6).Value = "範例庫存備註";

                // 銷售資料範本
                var salesSheet = workbook.Worksheets.Add("銷售資料範本");
                salesSheet.Cell(1, 1).Value = "產品編號";
                salesSheet.Cell(1, 2).Value = "數量";
                salesSheet.Cell(1, 3).Value = "單價";
                salesSheet.Cell(1, 4).Value = "客戶名稱";

                salesSheet.Cell(2, 1).Value = "EXAMPLE-001";
                salesSheet.Cell(2, 2).Value = 2;
                salesSheet.Cell(2, 3).Value = 100;
                salesSheet.Cell(2, 4).Value = "範例客戶";

                // 進貨資料範本
                var purchasesSheet = workbook.Worksheets.Add("進貨資料範本");
                purchasesSheet.Cell(1, 1).Value = "產品編號";
                purchasesSheet.Cell(1, 2).Value = "數量";
                purchasesSheet.Cell(1, 3).Value = "單價";
                purchasesSheet.Cell(1, 4).Value = "供應商";

                purchasesSheet.Cell(2, 1).Value = "EXAMPLE-001";
                purchasesSheet.Cell(2, 2).Value = 10;
                purchasesSheet.Cell(2, 3).Value = 80;
                purchasesSheet.Cell(2, 4).Value = "範例供應商";

                // 設定樣式
                foreach (var worksheet in workbook.Worksheets)
                {
                    var headerRange = worksheet.Range(1, 1, 1, worksheet.LastColumnUsed().ColumnNumber());
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Columns().AdjustToContents();
                }

                var fileName = $"CioSystem_匯入範本_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                _logger.LogInformation("成功匯出範本檔案，檔案名稱: {FileName}", fileName);

                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯出範本檔案時發生錯誤");
                TempData["ErrorMessage"] = "匯出範本檔案時發生錯誤，請稍後再試。";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// 顯示匯入頁面
        /// </summary>
        public IActionResult Import()
        {
            return View();
        }

        /// <summary>
        /// 處理匯入檔案
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "請選擇要匯入的檔案。";
                    return View();
                }

                if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "請選擇 Excel 檔案 (.xlsx)。";
                    return View();
                }

                _logger.LogInformation("開始匯入檔案: {FileName}", file.FileName);

                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);

                var importResults = new List<string>();

                // 匯入產品資料
                if (workbook.Worksheets.Any(w => w.Name == "產品資料範本"))
                {
                    var productsSheet = workbook.Worksheets.First(w => w.Name == "產品資料範本");
                    var productsCount = await ImportProducts(productsSheet);
                    importResults.Add($"產品資料：成功匯入 {productsCount} 筆（重複 SKU 會跳過）");
                }

                // 匯入庫存資料
                if (workbook.Worksheets.Any(w => w.Name == "庫存資料範本"))
                {
                    var inventorySheet = workbook.Worksheets.First(w => w.Name == "庫存資料範本");
                    var inventoryCount = await ImportInventory(inventorySheet);
                    importResults.Add($"庫存資料：成功處理 {inventoryCount} 筆（同產品會累加數量）");
                }

                // 匯入銷售資料
                if (workbook.Worksheets.Any(w => w.Name == "銷售資料範本"))
                {
                    var salesSheet = workbook.Worksheets.First(w => w.Name == "銷售資料範本");
                    var salesCount = await ImportSales(salesSheet);
                    importResults.Add($"銷售資料：成功匯入 {salesCount} 筆");
                }

                // 匯入進貨資料
                if (workbook.Worksheets.Any(w => w.Name == "進貨資料範本"))
                {
                    var purchasesSheet = workbook.Worksheets.First(w => w.Name == "進貨資料範本");
                    var purchasesCount = await ImportPurchases(purchasesSheet);
                    importResults.Add($"進貨資料：成功匯入 {purchasesCount} 筆");
                }

                TempData["SuccessMessage"] = string.Join("<br/>", importResults);
                _logger.LogInformation("匯入完成: {Results}", string.Join(", ", importResults));

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯入檔案時發生錯誤");
                TempData["ErrorMessage"] = "匯入檔案時發生錯誤，請檢查檔案格式是否正確。";
                return View();
            }
        }

        private async Task<int> ImportProducts(IXLWorksheet worksheet)
        {
            var count = 0;
            var products = await _productService.GetAllProductsAsync();
            var existingSKUs = products.Select(p => p.SKU).ToHashSet();

            for (int row = 2; row <= worksheet.LastRowUsed().RowNumber(); row++)
            {
                try
                {
                    var sku = worksheet.Cell(row, 2).Value.ToString();
                    if (string.IsNullOrEmpty(sku))
                        continue;

                    // 如果產品已存在，跳過（不更新現有產品）
                    if (existingSKUs.Contains(sku))
                    {
                        _logger.LogInformation("產品 SKU {SKU} 已存在，跳過匯入", sku);
                        continue;
                    }

                    var product = new Product
                    {
                        Name = worksheet.Cell(row, 1).Value.ToString(),
                        SKU = sku,
                        Brand = worksheet.Cell(row, 3).Value.ToString(),
                        Category = worksheet.Cell(row, 4).Value.ToString(),
                        Color = worksheet.Cell(row, 5).Value.ToString(),
                        Price = Convert.ToDecimal(worksheet.Cell(row, 6).Value),
                        CostPrice = Convert.ToDecimal(worksheet.Cell(row, 7).Value),
                        MinStockLevel = Convert.ToInt32(worksheet.Cell(row, 8).Value),
                        MaxStockLevel = Convert.ToInt32(worksheet.Cell(row, 9).Value),
                        Description = worksheet.Cell(row, 10).Value.ToString(),
                        Status = ProductStatus.Active,
                        CreatedAt = DateTime.Now,
                        CreatedBy = "System Import"
                    };

                    await _productService.CreateProductAsync(product);
                    existingSKUs.Add(sku);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "匯入產品資料第 {Row} 行時發生錯誤", row);
                }
            }

            return count;
        }

        private async Task<int> ImportInventory(IXLWorksheet worksheet)
        {
            var count = 0;
            var products = await _productService.GetAllProductsAsync();
            var productMap = products.ToDictionary(p => p.SKU, p => p.Id);
            var allInventory = await _inventoryService.GetAllInventoryAsync();
            var existingInventory = allInventory.ToDictionary(i => i.ProductId, i => i);

            for (int row = 2; row <= worksheet.LastRowUsed().RowNumber(); row++)
            {
                try
                {
                    var sku = worksheet.Cell(row, 1).Value.ToString();
                    if (!productMap.ContainsKey(sku))
                    {
                        _logger.LogWarning("產品 SKU {SKU} 不存在，跳過庫存匯入", sku);
                        continue;
                    }

                    var productId = productMap[sku];
                    var quantity = Convert.ToInt32(worksheet.Cell(row, 2).Value);
                    var safetyStock = Convert.ToInt32(worksheet.Cell(row, 3).Value);
                    var reservedQuantity = Convert.ToInt32(worksheet.Cell(row, 4).Value);
                    var productionDate = DateTime.TryParse(worksheet.Cell(row, 5).Value.ToString(), out var prodDate) ? prodDate : (DateTime?)null;
                    var notes = worksheet.Cell(row, 6).Value.ToString();

                    // 如果該產品已有庫存記錄，則累加數量
                    if (existingInventory.ContainsKey(productId))
                    {
                        var existingInv = existingInventory[productId];
                        existingInv.Quantity += quantity;
                        existingInv.SafetyStock = Math.Max(existingInv.SafetyStock, safetyStock); // 取較大的安全庫存
                        existingInv.ReservedQuantity += reservedQuantity;
                        existingInv.Notes = string.IsNullOrEmpty(notes) ? existingInv.Notes : $"{existingInv.Notes}; {notes}";
                        existingInv.UpdatedAt = DateTime.Now;
                        existingInv.UpdatedBy = "System Import";

                        await _inventoryService.UpdateInventoryAsync(existingInv.Id, existingInv);
                        _logger.LogInformation("累加庫存：產品 {SKU} 增加 {Quantity} 件", sku, quantity);
                    }
                    else
                    {
                        // 創建新的庫存記錄
                        var inventory = new Inventory
                        {
                            ProductId = productId,
                            ProductSKU = sku,
                            Quantity = quantity,
                            SafetyStock = safetyStock,
                            ReservedQuantity = reservedQuantity,
                            ProductionDate = productionDate,
                            Notes = notes,
                            Status = InventoryStatus.Normal,
                            Type = InventoryType.Stock,
                            CreatedAt = DateTime.Now,
                            CreatedBy = "System Import"
                        };

                        await _inventoryService.CreateInventoryAsync(inventory);
                        existingInventory[productId] = inventory; // 更新本地快取
                        _logger.LogInformation("創建新庫存：產品 {SKU} 數量 {Quantity} 件", sku, quantity);
                    }

                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "匯入庫存資料第 {Row} 行時發生錯誤", row);
                }
            }

            return count;
        }

        private async Task<int> ImportSales(IXLWorksheet worksheet)
        {
            var count = 0;
            var products = await _productService.GetAllProductsAsync();
            var productMap = products.ToDictionary(p => p.SKU, p => p.Id);

            for (int row = 2; row <= worksheet.LastRowUsed().RowNumber(); row++)
            {
                try
                {
                    var sku = worksheet.Cell(row, 1).Value.ToString();
                    if (!productMap.ContainsKey(sku))
                        continue;

                    var sale = new Sale
                    {
                        ProductId = productMap[sku],
                        Quantity = Convert.ToInt32(worksheet.Cell(row, 2).Value),
                        UnitPrice = Convert.ToDecimal(worksheet.Cell(row, 3).Value),
                        CustomerName = worksheet.Cell(row, 4).Value.ToString(),
                        CreatedAt = DateTime.Now,
                        CreatedBy = "System Import"
                    };

                    await _salesService.CreateSaleAsync(sale);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "匯入銷售資料第 {Row} 行時發生錯誤", row);
                }
            }

            return count;
        }

        private async Task<int> ImportPurchases(IXLWorksheet worksheet)
        {
            var count = 0;
            var products = await _productService.GetAllProductsAsync();
            var productMap = products.ToDictionary(p => p.SKU, p => p.Id);

            for (int row = 2; row <= worksheet.LastRowUsed().RowNumber(); row++)
            {
                try
                {
                    var sku = worksheet.Cell(row, 1).Value.ToString();
                    if (!productMap.ContainsKey(sku))
                        continue;

                    var purchase = new Purchase
                    {
                        ProductId = productMap[sku],
                        Quantity = Convert.ToInt32(worksheet.Cell(row, 2).Value),
                        UnitPrice = Convert.ToDecimal(worksheet.Cell(row, 3).Value),
                        Supplier = worksheet.Cell(row, 4).Value.ToString(),
                        CreatedAt = DateTime.Now,
                        CreatedBy = "System Import"
                    };

                    await _purchasesService.CreatePurchaseAsync(purchase);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "匯入進貨資料第 {Row} 行時發生錯誤", row);
                }
            }

            return count;
        }
    }
}