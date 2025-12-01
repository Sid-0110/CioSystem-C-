using Microsoft.AspNetCore.Mvc;
using CioSystem.Services;
using CioSystem.Models;
using CioSystem.Core.Interfaces;
using System.Text;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CioSystem.Services.Logging;

namespace CioSystem.Web.Controllers
{
    public class ExportImportController : Controller
    {
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        private readonly ISalesService _salesService;
        private readonly IPurchasesService _purchasesService;
        private readonly IDatabaseManagementService _databaseManagementService;
        private readonly ILogger<ExportImportController> _logger;
        private readonly ISystemLogService _systemLogService;

        public ExportImportController(
            IProductService productService,
            IInventoryService inventoryService,
            ISalesService salesService,
            IPurchasesService purchasesService,
            IDatabaseManagementService databaseManagementService,
            ILogger<ExportImportController> logger,
            ISystemLogService systemLogService)
        {
            _productService = productService;
            _inventoryService = inventoryService;
            _salesService = salesService;
            _purchasesService = purchasesService;
            _databaseManagementService = databaseManagementService;
            _logger = logger;
            _systemLogService = systemLogService;
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
                productsSheet.Cell(1, 7).Value = "尺寸";
                productsSheet.Cell(1, 8).Value = "價格";
                productsSheet.Cell(1, 9).Value = "成本價";
                productsSheet.Cell(1, 10).Value = "狀態";
                productsSheet.Cell(1, 11).Value = "最小庫存";
                productsSheet.Cell(1, 12).Value = "最大庫存";
                productsSheet.Cell(1, 13).Value = "描述";
                productsSheet.Cell(1, 14).Value = "建立時間";
                productsSheet.Cell(1, 15).Value = "更新時間";

                var row = 2;
                foreach (var product in products)
                {
                    productsSheet.Cell(row, 1).Value = product.Id;
                    productsSheet.Cell(row, 2).Value = product.Name;
                    productsSheet.Cell(row, 3).Value = product.SKU;
                    productsSheet.Cell(row, 4).Value = product.Brand;
                    productsSheet.Cell(row, 5).Value = product.Category;
                    productsSheet.Cell(row, 6).Value = product.Color;
                    productsSheet.Cell(row, 7).Value = product.Dimensions;
                    productsSheet.Cell(row, 8).Value = product.Price;
                    productsSheet.Cell(row, 9).Value = product.CostPrice;
                    productsSheet.Cell(row, 10).Value = product.Status.ToString();
                    productsSheet.Cell(row, 11).Value = product.MinStockLevel;
                    productsSheet.Cell(row, 12).Value = product.MaxStockLevel;
                    productsSheet.Cell(row, 13).Value = product.Description;
                    productsSheet.Cell(row, 14).Value = product.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    productsSheet.Cell(row, 15).Value = product.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
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
                inventorySheet.Cell(1, 13).Value = "員工自留";
                inventorySheet.Cell(1, 14).Value = "建立時間";
                inventorySheet.Cell(1, 15).Value = "更新時間";

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
                    inventorySheet.Cell(row, 12).Value = item.Notes ?? "";
                    inventorySheet.Cell(row, 13).Value = item.EmployeeRetention ?? "";
                    inventorySheet.Cell(row, 14).Value = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    inventorySheet.Cell(row, 15).Value = item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
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
                purchasesSheet.Cell(1, 9).Value = "員工自留";
                purchasesSheet.Cell(1, 10).Value = "進貨日期";
                purchasesSheet.Cell(1, 11).Value = "建立時間";
                purchasesSheet.Cell(1, 12).Value = "更新時間";

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
                    purchasesSheet.Cell(row, 7).Value = purchase.TotalAmount;
                    purchasesSheet.Cell(row, 8).Value = purchase.Supplier ?? "";
                    purchasesSheet.Cell(row, 9).Value = purchase.EmployeeRetention ?? "";
                    purchasesSheet.Cell(row, 10).Value = purchase.PurchaseDate.ToString("yyyy-MM-dd");
                    purchasesSheet.Cell(row, 11).Value = purchase.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    purchasesSheet.Cell(row, 12).Value = purchase.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
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
                productsSheet.Cell(1, 6).Value = "尺寸";
                productsSheet.Cell(1, 7).Value = "價格";
                productsSheet.Cell(1, 8).Value = "成本價";
                productsSheet.Cell(1, 9).Value = "最小庫存";
                productsSheet.Cell(1, 10).Value = "最大庫存";
                productsSheet.Cell(1, 11).Value = "描述";

                // 範本資料範例
                productsSheet.Cell(2, 1).Value = "範例產品";
                productsSheet.Cell(2, 2).Value = "EXAMPLE-001";
                productsSheet.Cell(2, 3).Value = "範例品牌";
                productsSheet.Cell(2, 4).Value = "服飾";
                productsSheet.Cell(2, 5).Value = "黑色";
                productsSheet.Cell(2, 6).Value = "M";
                productsSheet.Cell(2, 7).Value = 100;
                productsSheet.Cell(2, 8).Value = 80;
                productsSheet.Cell(2, 9).Value = 10;
                productsSheet.Cell(2, 10).Value = 100;
                productsSheet.Cell(2, 11).Value = "這是範例產品描述";

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
                purchasesSheet.Cell(1, 5).Value = "員工自留";
                purchasesSheet.Cell(1, 6).Value = "進貨日期";

                purchasesSheet.Cell(2, 1).Value = "EXAMPLE-001";
                purchasesSheet.Cell(2, 2).Value = 10;
                purchasesSheet.Cell(2, 3).Value = 80;
                purchasesSheet.Cell(2, 4).Value = "範例供應商";
                purchasesSheet.Cell(2, 5).Value = "";
                purchasesSheet.Cell(2, 6).Value = DateTime.Now.ToString("yyyy-MM-dd");

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

                // 記錄所有工作表名稱
                var worksheetNames = workbook.Worksheets.Select(w => w.Name).ToList();
                _logger.LogInformation("檔案包含的工作表: {Worksheets}", string.Join(", ", worksheetNames));

                var importResults = new List<string>();

                // 匯入產品資料（支援「產品資料範本」和「產品資料」兩種格式）
                if (workbook.Worksheets.Any(w => w.Name == "產品資料範本" || w.Name == "產品資料"))
                {
                    var worksheetName = workbook.Worksheets.Any(w => w.Name == "產品資料範本") ? "產品資料範本" : "產品資料";
                    var productsSheet = workbook.Worksheets.First(w => w.Name == worksheetName);
                    var lastRow = productsSheet.LastRowUsed()?.RowNumber() ?? 0;
                    _logger.LogInformation("產品資料工作表 '{WorksheetName}' 最後使用行數: {LastRow}", worksheetName, lastRow);
                    var productsCount = await ImportProducts(productsSheet, worksheetName == "產品資料");
                    importResults.Add($"產品資料：成功匯入 {productsCount} 筆（重複 SKU 會跳過）");
                    
                    // 產品匯入後，重新讀取產品列表以確保包含新匯入的產品
                    _logger.LogInformation("產品匯入完成，重新讀取產品列表以確保後續匯入能匹配到新產品");
                }
                else
                {
                    _logger.LogWarning("未找到產品資料工作表（產品資料範本 或 產品資料）");
                    importResults.Add("產品資料：未找到對應的工作表");
                }

                // 匯入庫存資料（支援「庫存資料範本」和「庫存資料」兩種格式）
                if (workbook.Worksheets.Any(w => w.Name == "庫存資料範本" || w.Name == "庫存資料"))
                {
                    var worksheetName = workbook.Worksheets.Any(w => w.Name == "庫存資料範本") ? "庫存資料範本" : "庫存資料";
                    var inventorySheet = workbook.Worksheets.First(w => w.Name == worksheetName);
                    var lastRow = inventorySheet.LastRowUsed()?.RowNumber() ?? 0;
                    _logger.LogInformation("庫存資料工作表 '{WorksheetName}' 最後使用行數: {LastRow}", worksheetName, lastRow);
                    var inventoryCount = await ImportInventory(inventorySheet, worksheetName == "庫存資料");
                    importResults.Add($"庫存資料：成功匯入 {inventoryCount} 筆（重複 SKU 會跳過）");
                }
                else
                {
                    _logger.LogWarning("未找到庫存資料工作表（庫存資料範本 或 庫存資料）");
                    importResults.Add("庫存資料：未找到對應的工作表");
                }

                // 匯入銷售資料（支援「銷售資料範本」和「銷售資料」兩種格式）
                if (workbook.Worksheets.Any(w => w.Name == "銷售資料範本" || w.Name == "銷售資料"))
                {
                    var worksheetName = workbook.Worksheets.Any(w => w.Name == "銷售資料範本") ? "銷售資料範本" : "銷售資料";
                    var salesSheet = workbook.Worksheets.First(w => w.Name == worksheetName);
                    var lastRow = salesSheet.LastRowUsed()?.RowNumber() ?? 0;
                    _logger.LogInformation("銷售資料工作表 '{WorksheetName}' 最後使用行數: {LastRow}", worksheetName, lastRow);
                    var (salesCount, salesSkipped, salesReasons) = await ImportSales(salesSheet, worksheetName == "銷售資料");
                    
                    // 確保銷售資料匯入完成後，所有事務都已提交
                    _logger.LogInformation("銷售資料匯入完成，等待事務提交...");
                    await Task.Delay(200); // 延遲，確保事務已提交並清除變更追蹤
                    
                    if (salesCount > 0)
                    {
                    importResults.Add($"銷售資料：成功匯入 {salesCount} 筆");
                    }
                    else if (salesSkipped > 0 && salesReasons.Count > 0)
                    {
                        var reasons = string.Join("、", salesReasons.Select(kvp => $"{kvp.Key}({kvp.Value}筆)"));
                        importResults.Add($"銷售資料：成功匯入 0 筆，跳過 {salesSkipped} 筆（原因：{reasons}）");
                    }
                    else
                    {
                        importResults.Add($"銷售資料：成功匯入 0 筆");
                    }
                }
                else
                {
                    _logger.LogWarning("未找到銷售資料工作表（銷售資料範本 或 銷售資料）");
                    importResults.Add("銷售資料：未找到對應的工作表");
                }

                // 在匯入進貨資料前，確保所有之前的事務都已提交
                _logger.LogInformation("準備匯入進貨資料，等待所有事務提交...");
                await Task.Delay(200); // 延遲，確保所有事務都已提交並清除變更追蹤

                // 匯入進貨資料（支援「進貨資料範本」和「進貨資料」兩種格式）
                if (workbook.Worksheets.Any(w => w.Name == "進貨資料範本" || w.Name == "進貨資料"))
                {
                    var worksheetName = workbook.Worksheets.Any(w => w.Name == "進貨資料範本") ? "進貨資料範本" : "進貨資料";
                    var purchasesSheet = workbook.Worksheets.First(w => w.Name == worksheetName);
                    var lastRow = purchasesSheet.LastRowUsed()?.RowNumber() ?? 0;
                    _logger.LogInformation("進貨資料工作表 '{WorksheetName}' 最後使用行數: {LastRow}", worksheetName, lastRow);
                    var (purchasesCount, purchasesSkipped, purchasesReasons) = await ImportPurchases(purchasesSheet, worksheetName == "進貨資料");
                    if (purchasesCount > 0)
                    {
                    importResults.Add($"進貨資料：成功匯入 {purchasesCount} 筆");
                    }
                    else if (purchasesSkipped > 0 && purchasesReasons.Count > 0)
                    {
                        var reasons = string.Join("、", purchasesReasons.Select(kvp => $"{kvp.Key}({kvp.Value}筆)"));
                        importResults.Add($"進貨資料：成功匯入 0 筆，跳過 {purchasesSkipped} 筆（原因：{reasons}）");
                    }
                    else
                    {
                        importResults.Add($"進貨資料：成功匯入 0 筆");
                    }
                }
                else
                {
                    _logger.LogWarning("未找到進貨資料工作表（進貨資料範本 或 進貨資料）");
                    importResults.Add("進貨資料：未找到對應的工作表");
                }

                // 檢查是否有任何數據被匯入
                var totalImported = importResults
                    .Where(r => r.Contains("成功"))
                    .Select(r => 
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(r, @"成功.*?(\d+).*?筆");
                        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
                    })
                    .Sum();

                // 生成詳細的診斷信息
                var diagnosticInfo = new List<string>();
                foreach (var worksheet in workbook.Worksheets)
                {
                    var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                    var rowsUsed = worksheet.RowsUsed().Count();
                    var rangeUsed = worksheet.RangeUsed();
                    var maxRow = rangeUsed?.LastRow().RowNumber() ?? 0;
                    var actualLastRow = Math.Max(Math.Max(lastRow, maxRow), rowsUsed);
                    
                    diagnosticInfo.Add($"工作表 '{worksheet.Name}': 最後行={lastRow}, 使用行={rowsUsed}, 範圍行={maxRow}, 實際行={actualLastRow}");
                    
                    // 讀取前幾行數據作為示例
                    if (actualLastRow > 1)
                    {
                        var sampleData = new List<string>();
                        for (int r = 1; r <= Math.Min(3, actualLastRow); r++)
                        {
                            var rowData = new List<string>();
                            var maxCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 15;
                            for (int c = 1; c <= Math.Min(maxCol, 15); c++)
                            {
                                var cell = worksheet.Cell(r, c);
                                var value = cell.GetString() ?? cell.Value.ToString();
                                rowData.Add(value);
                            }
                            sampleData.Add($"第{r}行: " + string.Join(" | ", rowData));
                        }
                        diagnosticInfo.AddRange(sampleData);
                    }
                }
                
                _logger.LogInformation("Excel 文件診斷信息:\n{DiagnosticInfo}", string.Join("\n", diagnosticInfo));

                if (totalImported == 0 && importResults.All(r => !r.Contains("未找到")))
                {
                    // 檢查資料庫中是否有現有數據
                    var existingProducts = await _productService.GetAllProductsAsync();
                    var existingProductsCount = existingProducts.Count();
                    var existingSKUs = existingProducts.Select(p => p.SKU?.Trim() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    
                    // 從 Excel 中提取所有 SKU（用於對比）
                    var excelSKUs = new List<string>();
                    var excelSKUDetails = new Dictionary<string, string>(); // SKU -> 工作表名稱
                    foreach (var worksheet in workbook.Worksheets)
                    {
                        if (worksheet.Name == "庫存資料" || worksheet.Name == "銷售資料" || worksheet.Name == "進貨資料")
                        {
                            var skuCol = 4; // 完整匯出格式中 SKU 在第4欄
                            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                            for (int r = 2; r <= Math.Min(lastRow, 30); r++) // 只檢查前30行
                            {
                                try
                                {
                                    var skuCell = worksheet.Cell(r, skuCol);
                                    var sku = skuCell.GetString()?.Trim();
                                    if (!string.IsNullOrWhiteSpace(sku))
                                    {
                                        if (!excelSKUs.Contains(sku, StringComparer.OrdinalIgnoreCase))
                                        {
                                            excelSKUs.Add(sku);
                                        }
                                        if (!excelSKUDetails.ContainsKey(sku))
                                        {
                                            excelSKUDetails[sku] = worksheet.Name;
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    
                    var detailMessage = "匯入完成，但沒有數據被匯入。<br/><br/>" +
                                       "<strong>可能的原因：</strong><br/>" +
                                       $"1. 資料庫中已有 {existingProductsCount} 個產品，所有 SKU 都已存在（產品資料會跳過）<br/>" +
                                       "2. 庫存/銷售/進貨資料中的產品 SKU 與資料庫中的不匹配<br/>" +
                                       "3. 工作表只有標題行，沒有數據<br/>" +
                                       "4. 數據格式不正確<br/><br/>" +
                                       "<strong>建議：</strong><br/>" +
                                       "• 如果是要還原數據，請先使用「清空數據」功能清空現有數據<br/>" +
                                       "• 檢查 Excel 中的產品 SKU 是否與資料庫中的一致<br/><br/>" +
                                       "<strong>診斷信息：</strong><br/>" +
                                       string.Join("<br/>", diagnosticInfo.Select(d => d.Replace("\n", "<br/>"))) +
                                       "<br/><br/><strong>SKU 對比：</strong><br/>" +
                                       $"資料庫中的 SKU（前20個）: {string.Join(", ", existingSKUs.Take(20))}<br/>" +
                                       $"Excel 中的 SKU（前20個）: {string.Join(", ", excelSKUs.Take(20))}<br/>" +
                                       $"Excel 中找不到對應產品的 SKU: {string.Join(", ", excelSKUs.Where(s => !existingSKUs.Contains(s, StringComparer.OrdinalIgnoreCase)).Take(10))}<br/>" +
                                       $"<strong>匹配狀態：</strong><br/>" +
                                       $"• 資料庫中有 {existingSKUs.Count} 個產品<br/>" +
                                       $"• Excel 中有 {excelSKUs.Count} 個不同的 SKU<br/>" +
                                       $"• 能匹配的 SKU: {excelSKUs.Count(s => existingSKUs.Contains(s, StringComparer.OrdinalIgnoreCase))} 個<br/>" +
                                       $"• 無法匹配的 SKU: {excelSKUs.Count(s => !existingSKUs.Contains(s, StringComparer.OrdinalIgnoreCase))} 個";
                    TempData["WarningMessage"] = detailMessage;
                }
                else
                {
                TempData["SuccessMessage"] = string.Join("<br/>", importResults);
                }
                
                _logger.LogInformation("匯入完成: {Results}, 總共匯入: {Total} 筆", string.Join(", ", importResults), totalImported);

                // 寫入系統操作日誌
                await _systemLogService.LogAsync(
                    "Info",
                    $"執行資料匯入：檔名={file.FileName}, 結果={string.Join("; ", importResults)}, TotalImported={totalImported}",
                    User?.Identity?.Name ?? "System");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯入檔案時發生錯誤");
                TempData["ErrorMessage"] = "匯入檔案時發生錯誤，請檢查檔案格式是否正確。";
                return View();
            }
        }

        private async Task<int> ImportProducts(IXLWorksheet worksheet, bool isFullExport = false)
        {
            var count = 0;
            var skippedCount = 0;
            var products = await _productService.GetAllProductsAsync();
            var existingSKUs = products.Select(p => p.SKU).ToHashSet();
            _logger.LogInformation("資料庫中現有產品數量: {Count}, 現有 SKU: {SKUs}", existingSKUs.Count, string.Join(", ", existingSKUs.Take(10)));

            // 判斷欄位位置（完整匯出格式 vs 範本格式）
            int idCol = isFullExport ? 1 : -1;   // 完整匯出時，第 1 欄為產品 ID
            int nameCol = isFullExport ? 2 : 1;
            int skuCol = isFullExport ? 3 : 2;
            int brandCol = isFullExport ? 4 : 3;
            int categoryCol = isFullExport ? 5 : 4;
            int colorCol = isFullExport ? 6 : 5;
            int dimensionsCol = isFullExport ? 7 : 6; // 尺寸欄位
            int priceCol = isFullExport ? 8 : 7;
            int costPriceCol = isFullExport ? 9 : 8;
            int minStockCol = isFullExport ? 11 : 9;
            int maxStockCol = isFullExport ? 12 : 10;
            int descCol = isFullExport ? 13 : 11;
            int statusCol = isFullExport ? 10 : -1; // 狀態欄位只在完整匯出時存在
            int createdAtCol = isFullExport ? 14 : -1;
            int updatedAtCol = isFullExport ? 15 : -1;

            // 使用多種方法檢查數據行數
            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var rowsUsed = worksheet.RowsUsed().Count();
            var rangeUsed = worksheet.RangeUsed();
            var maxRow = rangeUsed?.LastRow().RowNumber() ?? 0;
            
            _logger.LogInformation("開始匯入產品資料 - LastRowUsed: {LastRow}, RowsUsed: {RowsUsed}, RangeUsed.MaxRow: {MaxRow}, SKU欄位: {SkuCol}", 
                lastRowUsed, rowsUsed, maxRow, skuCol);
            
            // 使用最大的行數值
            var actualLastRow = Math.Max(Math.Max(lastRowUsed, maxRow), rowsUsed);
            
            // 如果只有標題行，直接返回
            if (actualLastRow <= 1)
            {
                _logger.LogWarning("產品資料工作表只有標題行，沒有數據 (actualLastRow: {ActualLastRow})", actualLastRow);
                return 0;
            }
            
            _logger.LogInformation("將處理第 2 行到第 {LastRow} 行的數據", actualLastRow);
            
            // 如果實際行數小於等於1，直接返回
            if (actualLastRow <= 1)
            {
                _logger.LogWarning("產品資料工作表只有標題行，沒有數據行 (actualLastRow: {ActualLastRow})", actualLastRow);
                return 0;
            }
            
            for (int row = 2; row <= actualLastRow; row++)
            {
                try
                {
                    var skuCell = worksheet.Cell(row, skuCol);
                    var sku = skuCell.GetString()?.Trim();
                    var skuValue = skuCell.Value.ToString();
                    var cellIsEmpty = skuCell.Value.IsBlank;
                    
                    _logger.LogInformation("第 {Row} 行，SKU欄位({SkuCol})，GetString(): '{SKU}', Value: '{Value}', IsBlank: {IsBlank}", 
                        row, skuCol, sku ?? "(null)", skuValue ?? "(null)", cellIsEmpty);
                    
                    // 檢查單元格是否為空
                    if (cellIsEmpty || string.IsNullOrWhiteSpace(sku))
                    {
                        _logger.LogInformation("第 {Row} 行 SKU 為空，跳過", row);
                        continue;
                    }

                    // 如果產品已存在，跳過（不更新現有產品）
                    if (existingSKUs.Contains(sku))
                    {
                        _logger.LogInformation("產品 SKU '{SKU}' 已存在，跳過匯入。資料庫中的 SKU 列表: {SKUList}", 
                            sku, string.Join(", ", existingSKUs.Take(10)));
                        skippedCount++;
                        continue;
                    }
                    
                    _logger.LogInformation("準備匯入產品 SKU: '{SKU}'", sku);

                    var nameCell = worksheet.Cell(row, nameCol);
                    var name = nameCell.GetString() ?? "";
                    var brandCell = worksheet.Cell(row, brandCol);
                    var brand = brandCell.GetString() ?? "";
                    var categoryCell = worksheet.Cell(row, categoryCol);
                    var category = categoryCell.GetString() ?? "";
                    var colorCell = worksheet.Cell(row, colorCol);
                    var color = colorCell.GetString();
                    var dimensionsCell = worksheet.Cell(row, dimensionsCol);
                    var dimensions = dimensionsCell.GetString();
                    
                    // 安全地轉換數值
                    decimal price = 0;
                    decimal costPrice = 0;
                    int minStock = 0;
                    int? maxStock = null;
                    
                    // 讀取價格
                    var priceCell = worksheet.Cell(row, priceCol);
                    var priceValue = priceCell.Value;
                    var priceStr = priceCell.GetString();
                    var priceIsBlank = priceCell.Value.IsBlank;
                    _logger.LogInformation("第 {Row} 行，價格欄位({PriceCol})，GetString(): '{PriceStr}', Value: '{Value}', IsBlank: {IsBlank}", 
                        row, priceCol, priceStr, priceValue, priceIsBlank);
                    
                    bool priceValid = false;
                    try
                    {
                        if (!priceIsBlank)
                        {
                            decimal parsedPrice = 0;
                            bool conversionSuccess = false;
                            
                            // 方法1: 嘗試直接轉換為 decimal（適用於數字格式）
                            try
                            {
                                parsedPrice = Convert.ToDecimal(priceValue);
                                conversionSuccess = true;
                                _logger.LogInformation("第 {Row} 行價格直接轉換成功: {Price}", row, parsedPrice);
                            }
                            catch
                            {
                                // 如果直接轉換失敗，嘗試從字符串解析
                            }
                            
                            // 方法2: 如果直接轉換失敗，嘗試從字符串解析（適用於文本格式）
                            if (!conversionSuccess && !string.IsNullOrWhiteSpace(priceStr))
                            {
                                // 移除可能的貨幣符號和千分位符號
                                var cleanPriceStr = priceStr.Replace("$", "").Replace(",", "").Replace("NT$", "").Replace("NT", "").Replace(" ", "").Trim();
                                if (decimal.TryParse(cleanPriceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsedPrice))
                                {
                                    conversionSuccess = true;
                                    _logger.LogInformation("第 {Row} 行價格從字符串解析成功: '{CleanStr}' -> {Price}", row, cleanPriceStr, parsedPrice);
                                }
                                else
                                {
                                    // 嘗試使用當前文化設置解析
                                    if (decimal.TryParse(cleanPriceStr, out parsedPrice))
                                    {
                                        conversionSuccess = true;
                                        _logger.LogInformation("第 {Row} 行價格使用當前文化設置解析成功: '{CleanStr}' -> {Price}", row, cleanPriceStr, parsedPrice);
                                    }
                                }
                            }
                            
                            if (conversionSuccess && parsedPrice > 0)
                            {
                                price = parsedPrice;
                                priceValid = true;
                                _logger.LogInformation("第 {Row} 行價格最終值: {Price}", row, price);
                            }
                            else if (conversionSuccess)
                            {
                                _logger.LogWarning("第 {Row} 行價格為 0 或負數: {Price}，將使用默認值 0.01", row, parsedPrice);
                            }
                            else
                            {
                                _logger.LogWarning("第 {Row} 行價格無法轉換: Value='{Value}', String='{Str}'，將使用默認值 0.01", row, priceValue, priceStr);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("第 {Row} 行價格欄位為空，將使用默認值 0.01", row);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "第 {Row} 行價格轉換發生異常: {Value}，將使用默認值 0.01", row, priceValue);
                    }
                    
                    // 讀取成本價
                    var costPriceCell = worksheet.Cell(row, costPriceCol);
                    var costPriceValue = costPriceCell.Value;
                    var costPriceStr = costPriceCell.GetString();
                    var costPriceIsBlank = costPriceCell.Value.IsBlank;
                    _logger.LogInformation("第 {Row} 行，成本價欄位({CostPriceCol})，GetString(): '{CostPriceStr}', Value: '{Value}', IsBlank: {IsBlank}", 
                        row, costPriceCol, costPriceStr, costPriceValue, costPriceIsBlank);
                    
                    bool costPriceValid = false;
                    try
                    {
                        if (!costPriceIsBlank)
                        {
                            decimal parsedCostPrice = 0;
                            bool conversionSuccess = false;
                            
                            // 方法1: 嘗試直接轉換為 decimal（適用於數字格式）
                            try
                            {
                                parsedCostPrice = Convert.ToDecimal(costPriceValue);
                                conversionSuccess = true;
                                _logger.LogInformation("第 {Row} 行成本價直接轉換成功: {CostPrice}", row, parsedCostPrice);
                            }
                            catch
                            {
                                // 如果直接轉換失敗，嘗試從字符串解析
                            }
                            
                            // 方法2: 如果直接轉換失敗，嘗試從字符串解析（適用於文本格式）
                            if (!conversionSuccess && !string.IsNullOrWhiteSpace(costPriceStr))
                            {
                                // 移除可能的貨幣符號和千分位符號
                                var cleanCostPriceStr = costPriceStr.Replace("$", "").Replace(",", "").Replace("NT$", "").Replace("NT", "").Replace(" ", "").Trim();
                                if (decimal.TryParse(cleanCostPriceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsedCostPrice))
                                {
                                    conversionSuccess = true;
                                    _logger.LogInformation("第 {Row} 行成本價從字符串解析成功: '{CleanStr}' -> {CostPrice}", row, cleanCostPriceStr, parsedCostPrice);
                                }
                                else
                                {
                                    // 嘗試使用當前文化設置解析
                                    if (decimal.TryParse(cleanCostPriceStr, out parsedCostPrice))
                                    {
                                        conversionSuccess = true;
                                        _logger.LogInformation("第 {Row} 行成本價使用當前文化設置解析成功: '{CleanStr}' -> {CostPrice}", row, cleanCostPriceStr, parsedCostPrice);
                                    }
                                }
                            }
                            
                            if (conversionSuccess && parsedCostPrice > 0)
                            {
                                costPrice = parsedCostPrice;
                                costPriceValid = true;
                                _logger.LogInformation("第 {Row} 行成本價最終值: {CostPrice}", row, costPrice);
                            }
                            else if (conversionSuccess)
                            {
                                _logger.LogWarning("第 {Row} 行成本價為 0 或負數: {CostPrice}，將使用默認值 0.01", row, parsedCostPrice);
                            }
                            else
                            {
                                _logger.LogWarning("第 {Row} 行成本價無法轉換: Value='{Value}', String='{Str}'，將使用默認值 0.01", row, costPriceValue, costPriceStr);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("第 {Row} 行成本價欄位為空，將使用默認值 0.01", row);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "第 {Row} 行成本價轉換發生異常: {Value}，將使用默認值 0.01", row, costPriceValue);
                    }
                    
                    // 如果價格或成本價無效，使用默認值（0.01）而不是跳過
                    if (!priceValid)
                    {
                        price = 0.01m; // 使用最小有效值
                        _logger.LogWarning("第 {Row} 行產品 SKU '{SKU}' 的價格無效，使用默認值 0.01", row, sku);
                    }
                    
                    if (!costPriceValid)
                    {
                        costPrice = 0.01m; // 使用最小有效值
                        _logger.LogWarning("第 {Row} 行產品 SKU '{SKU}' 的成本價無效，使用默認值 0.01", row, sku);
                    }
                    
                    try
                    {
                        var minStockValue = worksheet.Cell(row, minStockCol).Value;
                        minStock = Convert.ToInt32(minStockValue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("第 {Row} 行最小庫存轉換失敗: {Value}, 使用默認值 0", row, worksheet.Cell(row, minStockCol).Value);
                    }
                    
                    var maxStockCell = worksheet.Cell(row, maxStockCol);
                    var maxStockValue = maxStockCell.GetString();
                    if (!string.IsNullOrWhiteSpace(maxStockValue))
                    {
                        try
                        {
                            maxStock = Convert.ToInt32(maxStockCell.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("第 {Row} 行最大庫存轉換失敗: {Value}, 設為 null", row, maxStockCell.Value);
                        }
                    }
                    
                    var descCell = worksheet.Cell(row, descCol);
                    var description = descCell.GetString();

                    // 處理狀態（如果存在）
                    ProductStatus status = ProductStatus.Active;
                    if (statusCol > 0)
                    {
                        var statusCell = worksheet.Cell(row, statusCol);
                        var statusValue = statusCell.GetString();
                        if (!string.IsNullOrWhiteSpace(statusValue) && Enum.TryParse<ProductStatus>(statusValue, out var parsedStatus))
                        {
                            status = parsedStatus;
                        }
                    }

                    // 處理時間戳記（如果存在）
                    DateTime createdAt = DateTime.Now;
                    DateTime updatedAt = DateTime.Now;
                    if (createdAtCol > 0)
                    {
                        var createdAtCell = worksheet.Cell(row, createdAtCol);
                        var createdAtStr = createdAtCell.GetString();
                        if (!string.IsNullOrWhiteSpace(createdAtStr) && DateTime.TryParse(createdAtStr, out var parsedCreatedAt))
                        {
                            createdAt = parsedCreatedAt;
                        }
                    }
                    if (updatedAtCol > 0)
                    {
                        var updatedAtCell = worksheet.Cell(row, updatedAtCol);
                        var updatedAtStr = updatedAtCell.GetString();
                        if (!string.IsNullOrWhiteSpace(updatedAtStr) && DateTime.TryParse(updatedAtStr, out var parsedUpdatedAt))
                        {
                            updatedAt = parsedUpdatedAt;
                        }
                    }

                    var product = new Product
                    {
                        // 預設 Id=0，若是完整匯出檔會在下方嘗試帶入 Excel 的 ID
                        Id = 0,
                        Name = name,
                        SKU = sku,
                        Brand = brand,
                        Category = category,
                        Color = color,
                        Dimensions = dimensions,
                        Price = price,
                        CostPrice = costPrice,
                        MinStockLevel = minStock,
                        MaxStockLevel = maxStock,
                        Description = description,
                        Status = status,
                        CreatedAt = createdAt,
                        UpdatedAt = updatedAt,
                        CreatedBy = "System Import",
                        UpdatedBy = "System Import"
                    };

                    // 如果是完整匯出檔案，嘗試使用 Excel 中的產品 ID，讓清空後重新匯入時 ID 一致
                    if (idCol > 0)
                    {
                        try
                        {
                            var idCell = worksheet.Cell(row, idCol);
                            var idStr = idCell.GetString();
                            if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out var parsedId) && parsedId > 0)
                            {
                                product.Id = parsedId;
                                _logger.LogInformation("使用匯出檔中的產品 ID: {Id} (SKU={SKU})", parsedId, sku);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "讀取產品 ID 失敗，將改用資料庫自動產生的 ID。Row={Row}, SKU={SKU}", row, sku);
                        }
                    }

                    try
                    {
                        var createdProduct = await _productService.CreateProductAsync(product);
                    existingSKUs.Add(sku);
                    count++;
                        _logger.LogInformation("成功匯入產品: SKU={SKU}, Name={Name}, Price={Price}, CostPrice={CostPrice}, 資料庫中的實際 SKU={DbSKU}", 
                            sku, name, price, costPrice, createdProduct.SKU);
                    }
                    catch (Exception createEx)
                    {
                        _logger.LogError(createEx, "創建產品時發生錯誤: SKU={SKU}, Name={Name}, Error={Error}", 
                            sku, name, createEx.Message);
                        // 繼續處理下一行，不中斷整個匯入過程
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "匯入產品資料第 {Row} 行時發生錯誤: {ErrorMessage}, StackTrace: {StackTrace}", 
                        row, ex.Message, ex.StackTrace);
                }
            }

            _logger.LogInformation("產品資料匯入完成，成功匯入 {Count} 筆，跳過 {SkippedCount} 筆（SKU 已存在）", count, skippedCount);
            
            // 如果所有記錄都被跳過，記錄警告
            if (count == 0 && skippedCount > 0)
            {
                _logger.LogWarning("所有產品資料都因為 SKU 已存在而被跳過，共 {SkippedCount} 筆", skippedCount);
            }

            return count;
        }

        private async Task<int> ImportInventory(IXLWorksheet worksheet, bool isFullExport = false)
        {
            var count = 0;
            var skippedCount = 0;
            
            // 在開始時重新獲取產品列表，確保包含最新匯入的產品
            var products = await _productService.GetAllProductsAsync();
            // 使用 Trim 後的 SKU 作為 key，確保匹配，使用不區分大小寫的比較器
            var productMap = products.ToDictionary(p => p.SKU?.Trim() ?? "", p => p.Id, StringComparer.OrdinalIgnoreCase);
            var allInventory = await _inventoryService.GetAllInventoryAsync();
            // 如果一個產品有多個庫存記錄，只取第一個（或最新的）
            var existingInventory = allInventory
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.UpdatedAt).First());
            _logger.LogInformation("庫存匯入開始 - 資料庫中現有產品數量: {Count}, 現有庫存記錄: {InventoryCount}", productMap.Count, existingInventory.Count);
            _logger.LogInformation("資料庫中的 SKU 列表（前20個）: {SKUList}", string.Join(", ", productMap.Keys.Take(20)));

            // 判斷欄位位置（完整匯出格式 vs 範本格式）
            int idCol = isFullExport ? 1 : -1;   // 完整匯出時，第一欄為庫存 ID
            int skuCol = isFullExport ? 4 : 1;   // 完整匯出：ID, 產品ID, 產品名稱, 產品編號, ...
            int quantityCol = isFullExport ? 5 : 2;
            int safetyStockCol = isFullExport ? 6 : 3;
            int reservedQuantityCol = isFullExport ? 7 : 4;
            int productionDateCol = isFullExport ? 10 : 5;
            int notesCol = isFullExport ? 12 : 6;
            int employeeRetentionCol = isFullExport ? 13 : -1;
            int createdAtCol = isFullExport ? 14 : -1;
            int updatedAtCol = isFullExport ? 15 : -1;

            // 使用多種方法檢查數據行數
            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var rowsUsed = worksheet.RowsUsed().Count();
            var rangeUsed = worksheet.RangeUsed();
            var maxRow = rangeUsed?.LastRow().RowNumber() ?? 0;
            
            _logger.LogInformation("開始匯入庫存資料 - LastRowUsed: {LastRow}, RowsUsed: {RowsUsed}, RangeUsed.MaxRow: {MaxRow}, SKU欄位: {SkuCol}", 
                lastRowUsed, rowsUsed, maxRow, skuCol);
            
            // 使用最大的行數值
            var actualLastRow = Math.Max(Math.Max(lastRowUsed, maxRow), rowsUsed);
            
            // 如果只有標題行，直接返回
            if (actualLastRow <= 1)
            {
                _logger.LogWarning("庫存資料工作表只有標題行，沒有數據 (actualLastRow: {ActualLastRow})", actualLastRow);
                return 0;
            }
            
            _logger.LogInformation("將處理第 2 行到第 {LastRow} 行的數據", actualLastRow);
            
            for (int row = 2; row <= actualLastRow; row++)
            {
                try
                {
                    var skuCell = worksheet.Cell(row, skuCol);
                    var sku = skuCell.GetString()?.Trim();
                    var skuValue = skuCell.Value.ToString();
                    var cellIsBlank = skuCell.Value.IsBlank;
                    
                    _logger.LogInformation("第 {Row} 行，SKU欄位({SkuCol})，GetString(): '{SKU}', Value: '{Value}', IsBlank: {IsBlank}", 
                        row, skuCol, sku, skuValue, cellIsBlank);
                    
                    if (string.IsNullOrWhiteSpace(sku))
                    {
                        _logger.LogWarning("第 {Row} 行 SKU 為空，跳過", row);
                        skippedCount++;
                        continue;
                    }

                    // 由於 productMap 使用大小寫不敏感比較器，直接查找即可
                    if (!productMap.TryGetValue(sku, out var productId))
                    {
                        // 如果第一次查找失敗，重新獲取產品列表（可能產品剛被匯入）
                        products = await _productService.GetAllProductsAsync();
                        productMap = products.ToDictionary(p => p.SKU?.Trim() ?? "", p => p.Id, StringComparer.OrdinalIgnoreCase);
                        _logger.LogInformation("重新獲取產品列表，現在有 {Count} 個產品", productMap.Count);
                        
                        // 再次嘗試查找
                        if (!productMap.TryGetValue(sku, out productId))
                        {
                            // 嘗試更寬鬆的匹配：移除所有空格和特殊字符
                            var cleanSku = sku?.Replace(" ", "").Replace("-", "").Replace("_", "").ToUpperInvariant();
                            var matchedSku = productMap.Keys.FirstOrDefault(k => 
                                k?.Replace(" ", "").Replace("-", "").Replace("_", "").ToUpperInvariant() == cleanSku);
                            
                            if (matchedSku != null && productMap.TryGetValue(matchedSku, out productId))
                            {
                                _logger.LogInformation("使用寬鬆匹配找到產品: Excel SKU '{ExcelSku}' -> 資料庫 SKU '{DbSku}', 產品ID: {ProductId}", 
                                    sku, matchedSku, productId);
                            }
                            else
                            {
                                _logger.LogWarning("產品 SKU '{SKU}' 不存在於資料庫中。Excel SKU 長度: {Length}, 資料庫中的 SKU 列表（前20個）: {SKUList}", 
                                    sku, sku?.Length ?? 0, string.Join(", ", productMap.Keys.Take(20)));
                                skippedCount++;
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogInformation("重新獲取產品列表後，成功匹配產品 SKU '{SKU}'，產品ID: {ProductId}", sku, productId);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("成功匹配產品 SKU '{SKU}'，產品ID: {ProductId}", sku, productId);
                    }
                    
                    int quantity = 0;
                    int safetyStock = 0;
                    int reservedQuantity = 0;
                    
                    try
                    {
                        var quantityCell = worksheet.Cell(row, quantityCol);
                        quantity = Convert.ToInt32(quantityCell.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "第 {Row} 行數量轉換失敗，使用默認值 0", row);
                    }
                    
                    try
                    {
                        var safetyStockCell = worksheet.Cell(row, safetyStockCol);
                        safetyStock = Convert.ToInt32(safetyStockCell.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "第 {Row} 行安全庫存轉換失敗，使用默認值 0", row);
                    }
                    
                    try
                    {
                        var reservedQuantityCell = worksheet.Cell(row, reservedQuantityCol);
                        reservedQuantity = Convert.ToInt32(reservedQuantityCell.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "第 {Row} 行預留數量轉換失敗，使用默認值 0", row);
                    }
                    
                    _logger.LogInformation("第 {Row} 行庫存資料: SKU={SKU}, Quantity={Quantity}, SafetyStock={SafetyStock}, ReservedQuantity={ReservedQuantity}", 
                        row, sku, quantity, safetyStock, reservedQuantity);
                    var productionDateCell = worksheet.Cell(row, productionDateCol);
                    var productionDateValue = productionDateCell.GetString();
                    var productionDate = string.IsNullOrWhiteSpace(productionDateValue) ? (DateTime?)null : (DateTime.TryParse(productionDateValue, out var prodDate) ? prodDate : (DateTime?)null);
                    var notesCell = worksheet.Cell(row, notesCol);
                    var notes = notesCell.GetString() ?? "";
                    var employeeRetentionCell = employeeRetentionCol > 0 ? worksheet.Cell(row, employeeRetentionCol) : null;
                    string? employeeRetention = null;
                    if (employeeRetentionCell != null)
                    {
                        var employeeRetentionValue = employeeRetentionCell.GetString();
                        // 如果值為空、空白、或 "0"，則設為 null
                        if (string.IsNullOrWhiteSpace(employeeRetentionValue) || employeeRetentionValue == "0")
                        {
                            employeeRetention = null;
                        }
                        else
                        {
                            employeeRetention = employeeRetentionValue;
                        }
                    }

                    // 處理時間戳記（如果存在）
                    DateTime createdAt = DateTime.Now;
                    DateTime updatedAt = DateTime.Now;
                    if (createdAtCol > 0)
                    {
                        var createdAtCell = worksheet.Cell(row, createdAtCol);
                        var createdAtStr = createdAtCell.GetString();
                        if (!string.IsNullOrWhiteSpace(createdAtStr) && DateTime.TryParse(createdAtStr, out var parsedCreatedAt))
                        {
                            createdAt = parsedCreatedAt;
                        }
                    }
                    if (updatedAtCol > 0)
                    {
                        var updatedAtCell = worksheet.Cell(row, updatedAtCol);
                        var updatedAtStr = updatedAtCell.GetString();
                        if (!string.IsNullOrWhiteSpace(updatedAtStr) && DateTime.TryParse(updatedAtStr, out var parsedUpdatedAt))
                        {
                            updatedAt = parsedUpdatedAt;
                        }
                    }

                    // 如果該產品已有庫存記錄，則跳過（不累加，與產品匯入行為一致）
                    if (existingInventory.ContainsKey(productId))
                    {
                        _logger.LogInformation("產品 SKU '{SKU}' 已有庫存記錄，跳過匯入", sku);
                        skippedCount++;
                        continue;
                    }
                    else
                    {
                        // 創建新的庫存記錄
                        var inventory = new Inventory
                        {
                            // 如果是完整匯出檔案，嘗試使用 Excel 中的 ID，讓清空後重新匯入時 ID 一致
                            Id = 0,
                            ProductId = productId,
                            ProductSKU = sku,
                            Quantity = quantity,
                            SafetyStock = safetyStock,
                            ReservedQuantity = reservedQuantity,
                            ProductionDate = productionDate,
                            Notes = notes,
                            EmployeeRetention = employeeRetention ?? string.Empty,
                            Status = InventoryStatus.Normal,
                            Type = InventoryType.Stock,
                            CreatedAt = createdAt,
                            UpdatedAt = updatedAt,
                            CreatedBy = "System Import",
                            UpdatedBy = "System Import"
                        };

                        if (idCol > 0)
                        {
                            try
                            {
                                var idCell = worksheet.Cell(row, idCol);
                                var idStr = idCell.GetString();
                                if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out var parsedId) && parsedId > 0)
                                {
                                    inventory.Id = parsedId;
                                    _logger.LogInformation("使用匯出檔中的庫存 ID: {Id} (SKU={SKU})", parsedId, sku);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "讀取庫存 ID 失敗，將改用資料庫自動產生的 ID。Row={Row}, SKU={SKU}", row, sku);
                            }
                        }

                        try
                        {
                        await _inventoryService.CreateInventoryAsync(inventory);
                        existingInventory[productId] = inventory; // 更新本地快取
                            _logger.LogInformation("創建新庫存成功：產品 {SKU} 數量 {Quantity} 件", sku, quantity);
                        }
                        catch (Exception createEx)
                        {
                            _logger.LogError(createEx, "創建庫存記錄時發生錯誤: SKU={SKU}, ProductId={ProductId}, Quantity={Quantity}, Error={Error}", 
                                sku, productId, quantity, createEx.Message);
                            throw; // 重新拋出異常，讓外層 catch 處理
                        }
                    }

                    count++;
                    _logger.LogInformation("成功處理庫存資料第 {Row} 行: SKU={SKU}, Quantity={Quantity}", row, sku, quantity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "匯入庫存資料第 {Row} 行時發生錯誤: {ErrorMessage}, StackTrace: {StackTrace}", row, ex.Message, ex.StackTrace);
                    skippedCount++; // 發生異常時也計入跳過數量
                }
            }

            _logger.LogInformation("庫存資料匯入完成，成功匯入 {Count} 筆，跳過 {SkippedCount} 筆", count, skippedCount);
            
            // 如果所有記錄都被跳過，記錄警告和詳細信息
            if (count == 0 && skippedCount > 0)
            {
                _logger.LogWarning("所有庫存資料都因為產品不存在而被跳過，共 {SkippedCount} 筆", skippedCount);
                // 記錄前10個被跳過的 SKU
                var skippedSKUs = new List<string>();
                for (int row = 2; row <= Math.Min(actualLastRow, 12); row++)
                {
                    try
                    {
                        var skuCell = worksheet.Cell(row, skuCol);
                        var sku = skuCell.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(sku))
                        {
                            if (!productMap.ContainsKey(sku))
                            {
                                skippedSKUs.Add(sku);
                            }
                        }
                    }
                    catch { }
                }
                _logger.LogWarning("被跳過的 SKU 示例（前10個）: {SkippedSKUs}", string.Join(", ", skippedSKUs.Take(10)));
            }

            return count;
        }

        private async Task<(int count, int skippedCount, Dictionary<string, int> skippedReasons)> ImportSales(IXLWorksheet worksheet, bool isFullExport = false)
        {
            var count = 0;
            var skippedCount = 0;
            var skippedReasons = new Dictionary<string, int>();
            
            // 在開始時重新獲取產品列表，確保包含最新匯入的產品
            var products = await _productService.GetAllProductsAsync();
            // 使用 Trim 後的 SKU 作為 key，確保匹配，使用不區分大小寫的比較器
            var productMap = products.ToDictionary(p => p.SKU?.Trim() ?? "", p => p.Id, StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation("銷售匯入開始 - 資料庫中現有產品數量: {Count}", productMap.Count);
            _logger.LogInformation("資料庫中的 SKU 列表（前20個）: {SKUList}", string.Join(", ", productMap.Keys.Take(20)));

            // 判斷欄位位置（完整匯出格式 vs 範本格式）
            int idCol = isFullExport ? 1 : -1;   // 完整匯出時，第 1 欄為銷售 ID
            int skuCol = isFullExport ? 4 : 1;   // 完整匯出：ID, 產品ID, 產品名稱, 產品編號, ...
            int quantityCol = isFullExport ? 5 : 2;
            int unitPriceCol = isFullExport ? 6 : 3;
            int customerNameCol = isFullExport ? 8 : 4;
            int createdAtCol = isFullExport ? 9 : -1;
            int updatedAtCol = isFullExport ? 10 : -1;

            // 使用多種方法檢查數據行數
            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var rowsUsed = worksheet.RowsUsed().Count();
            var rangeUsed = worksheet.RangeUsed();
            var maxRow = rangeUsed?.LastRow().RowNumber() ?? 0;
            
            _logger.LogInformation("開始匯入銷售資料 - LastRowUsed: {LastRow}, RowsUsed: {RowsUsed}, RangeUsed.MaxRow: {MaxRow}, SKU欄位: {SkuCol}", 
                lastRowUsed, rowsUsed, maxRow, skuCol);
            
            // 使用最大的行數值
            var actualLastRow = Math.Max(Math.Max(lastRowUsed, maxRow), rowsUsed);
            
            // 如果只有標題行，直接返回
            if (actualLastRow <= 1)
            {
                _logger.LogWarning("銷售資料工作表只有標題行，沒有數據 (actualLastRow: {ActualLastRow})", actualLastRow);
                return (0, 0, new Dictionary<string, int>());
            }
            
            _logger.LogInformation("將處理第 2 行到第 {LastRow} 行的數據", actualLastRow);
            
            for (int row = 2; row <= actualLastRow; row++)
            {
                try
                {
                    var skuCell = worksheet.Cell(row, skuCol);
                    var sku = skuCell.GetString()?.Trim();
                    var skuValue = skuCell.Value.ToString();
                    var cellIsBlank = skuCell.Value.IsBlank;
                    
                    _logger.LogInformation("第 {Row} 行，SKU欄位({SkuCol})，GetString(): '{SKU}', Value: '{Value}', IsBlank: {IsBlank}", 
                        row, skuCol, sku, skuValue, cellIsBlank);
                    
                    if (string.IsNullOrWhiteSpace(sku))
                    {
                        _logger.LogWarning("第 {Row} 行 SKU 為空，跳過", row);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "SKU為空");
                        continue;
                    }
                    
                    // 由於 productMap 使用大小寫不敏感比較器，直接查找即可
                    if (!productMap.TryGetValue(sku, out var productId))
                    {
                        // 如果第一次查找失敗，重新獲取產品列表（可能產品剛被匯入）
                        products = await _productService.GetAllProductsAsync();
                        productMap = products.ToDictionary(p => p.SKU?.Trim() ?? "", p => p.Id, StringComparer.OrdinalIgnoreCase);
                        _logger.LogInformation("重新獲取產品列表，現在有 {Count} 個產品", productMap.Count);
                        
                        // 再次嘗試查找
                        if (!productMap.TryGetValue(sku, out productId))
                        {
                            // 嘗試更寬鬆的匹配：移除所有空格和特殊字符
                            var cleanSku = sku?.Replace(" ", "").Replace("-", "").Replace("_", "").ToUpperInvariant();
                            var matchedSku = productMap.Keys.FirstOrDefault(k => 
                                k?.Replace(" ", "").Replace("-", "").Replace("_", "").ToUpperInvariant() == cleanSku);
                            
                            if (matchedSku != null && productMap.TryGetValue(matchedSku, out productId))
                            {
                                _logger.LogInformation("使用寬鬆匹配找到產品: Excel SKU '{ExcelSku}' -> 資料庫 SKU '{DbSku}', 產品ID: {ProductId}", 
                                    sku, matchedSku, productId);
                            }
                            else
                            {
                                _logger.LogWarning("產品 SKU '{SKU}' 不存在於資料庫中。Excel SKU 長度: {Length}, 資料庫中的 SKU 列表（前20個）: {SKUList}", 
                                    sku, sku?.Length ?? 0, string.Join(", ", productMap.Keys.Take(20)));
                                skippedCount++;
                                IncrementSkippedReason(skippedReasons, "產品SKU不存在");
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogInformation("重新獲取產品列表後，成功匹配產品 SKU '{SKU}'，產品ID: {ProductId}", sku, productId);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("成功匹配產品 SKU '{SKU}'，產品ID: {ProductId}", sku, productId);
                    }

                    int quantity = 0;
                    decimal unitPrice = 0;
                    
                    // 改進數量轉換邏輯，支援多種格式
                    var quantityCell = worksheet.Cell(row, quantityCol);
                    var quantityValue = quantityCell.Value;
                    var quantityStr = quantityCell.GetString();
                    var quantityIsBlank = quantityCell.Value.IsBlank;
                    
                    _logger.LogInformation("第 {Row} 行，數量欄位({QuantityCol})，GetString(): '{QuantityStr}', Value: '{Value}', IsBlank: {IsBlank}", 
                        row, quantityCol, quantityStr, quantityValue, quantityIsBlank);
                    
                    bool quantityValid = false;
                    if (!quantityIsBlank)
                    {
                        try
                        {
                            // 方法1: 嘗試直接轉換為 int（適用於數字格式）
                            try
                            {
                                quantity = Convert.ToInt32(quantityValue);
                                quantityValid = true;
                                _logger.LogInformation("第 {Row} 行數量直接轉換成功: {Quantity}", row, quantity);
                            }
                            catch
                            {
                                // 如果直接轉換失敗，嘗試從字符串解析
                            }
                            
                            // 方法2: 如果直接轉換失敗，嘗試從字符串解析（適用於文本格式）
                            if (!quantityValid && !string.IsNullOrWhiteSpace(quantityStr))
                            {
                                // 移除可能的千分位符號和空格
                                var cleanQuantityStr = quantityStr.Replace(",", "").Replace(" ", "").Trim();
                                if (int.TryParse(cleanQuantityStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedQuantity))
                                {
                                    quantity = parsedQuantity;
                                    quantityValid = true;
                                    _logger.LogInformation("第 {Row} 行數量從字符串解析成功: '{CleanStr}' -> {Quantity}", row, cleanQuantityStr, quantity);
                                }
                                else
                                {
                                    // 嘗試使用當前文化設置解析
                                    if (int.TryParse(cleanQuantityStr, out parsedQuantity))
                                    {
                                        quantity = parsedQuantity;
                                        quantityValid = true;
                                        _logger.LogInformation("第 {Row} 行數量使用當前文化設置解析成功: '{CleanStr}' -> {Quantity}", row, cleanQuantityStr, quantity);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "第 {Row} 行數量轉換發生異常: {Value}", row, quantityValue);
                        }
                    }
                    
                    if (!quantityValid)
                    {
                        _logger.LogWarning("第 {Row} 行數量轉換失敗: Value='{Value}', String='{Str}'，跳過此記錄", row, quantityValue, quantityStr);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "數量轉換失敗");
                        continue;
                    }
                    
                    // 改進單價轉換邏輯，支援多種格式
                    var unitPriceCell = worksheet.Cell(row, unitPriceCol);
                    var unitPriceValue = unitPriceCell.Value;
                    var unitPriceStr = unitPriceCell.GetString();
                    var unitPriceIsBlank = unitPriceCell.Value.IsBlank;
                    
                    _logger.LogInformation("第 {Row} 行，單價欄位({UnitPriceCol})，GetString(): '{UnitPriceStr}', Value: '{Value}', IsBlank: {IsBlank}", 
                        row, unitPriceCol, unitPriceStr, unitPriceValue, unitPriceIsBlank);
                    
                    bool unitPriceValid = false;
                    if (!unitPriceIsBlank)
                    {
                        try
                        {
                            // 方法1: 嘗試直接轉換為 decimal（適用於數字格式）
                            try
                            {
                                unitPrice = Convert.ToDecimal(unitPriceValue);
                                unitPriceValid = true;
                                _logger.LogInformation("第 {Row} 行單價直接轉換成功: {UnitPrice}", row, unitPrice);
                            }
                            catch
                            {
                                // 如果直接轉換失敗，嘗試從字符串解析
                            }
                            
                            // 方法2: 如果直接轉換失敗，嘗試從字符串解析（適用於文本格式）
                            if (!unitPriceValid && !string.IsNullOrWhiteSpace(unitPriceStr))
                            {
                                // 移除可能的貨幣符號和千分位符號
                                var cleanUnitPriceStr = unitPriceStr.Replace("$", "").Replace(",", "").Replace("NT$", "").Replace("NT", "").Replace(" ", "").Trim();
                                if (decimal.TryParse(cleanUnitPriceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedUnitPrice))
                                {
                                    unitPrice = parsedUnitPrice;
                                    unitPriceValid = true;
                                    _logger.LogInformation("第 {Row} 行單價從字符串解析成功: '{CleanStr}' -> {UnitPrice}", row, cleanUnitPriceStr, unitPrice);
                                }
                                else
                                {
                                    // 嘗試使用當前文化設置解析
                                    if (decimal.TryParse(cleanUnitPriceStr, out parsedUnitPrice))
                                    {
                                        unitPrice = parsedUnitPrice;
                                        unitPriceValid = true;
                                        _logger.LogInformation("第 {Row} 行單價使用當前文化設置解析成功: '{CleanStr}' -> {UnitPrice}", row, cleanUnitPriceStr, unitPrice);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "第 {Row} 行單價轉換發生異常: {Value}", row, unitPriceValue);
                        }
                    }
                    
                    if (!unitPriceValid)
                    {
                        _logger.LogWarning("第 {Row} 行單價轉換失敗: Value='{Value}', String='{Str}'，跳過此記錄", row, unitPriceValue, unitPriceStr);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "單價轉換失敗");
                        continue;
                    }
                    
                    var customerNameCell = worksheet.Cell(row, customerNameCol);
                    var customerName = customerNameCell.GetString() ?? "";
                    
                    _logger.LogInformation("第 {Row} 行銷售資料: SKU={SKU}, ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}, CustomerName='{CustomerName}'", 
                        row, sku, productId, quantity, unitPrice, customerName);
                    
                    // 驗證必要欄位
                    if (string.IsNullOrWhiteSpace(customerName))
                    {
                        _logger.LogWarning("第 {Row} 行客戶名稱為空，跳過此記錄", row);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "客戶名稱為空");
                        continue;
                    }
                    
                    if (quantity <= 0)
                    {
                        _logger.LogWarning("第 {Row} 行數量為 {Quantity}，必須大於 0，跳過此記錄", row, quantity);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "數量必須大於0");
                        continue;
                    }
                    
                    if (unitPrice <= 0)
                    {
                        _logger.LogWarning("第 {Row} 行單價為 {UnitPrice}，必須大於 0，跳過此記錄", row, unitPrice);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "單價必須大於0");
                        continue;
                    }

                    // 處理時間戳記（如果存在）
                    // 注意：使用當前時間而不是 Excel 中的時間，避免重複提交防護誤判
                    DateTime createdAt = DateTime.Now;
                    DateTime updatedAt = DateTime.Now;
                    DateTime saleDate = DateTime.Now;
                    
                    // 讀取銷售日期（如果存在）
                    if (isFullExport && createdAtCol > 0)
                    {
                        // 完整匯出格式中，銷售日期可能在 CreatedAt 欄位，或者有專門的銷售日期欄位
                        // 先嘗試從 CreatedAt 讀取
                        var saleDateCell = worksheet.Cell(row, createdAtCol);
                        var saleDateStr = saleDateCell.GetString();
                        if (!string.IsNullOrWhiteSpace(saleDateStr) && DateTime.TryParse(saleDateStr, out var parsedSaleDate))
                        {
                            saleDate = parsedSaleDate;
                        }
                    }
                    
                    // 如果 Excel 中有時間戳記，可以選擇使用，但為了避免重複提交防護問題，我們使用當前時間
                    // 如果需要保留原始時間，可以取消下面的註釋
                    /*
                    if (createdAtCol > 0)
                    {
                        var createdAtCell = worksheet.Cell(row, createdAtCol);
                        var createdAtStr = createdAtCell.GetString();
                        if (!string.IsNullOrWhiteSpace(createdAtStr) && DateTime.TryParse(createdAtStr, out var parsedCreatedAt))
                        {
                            createdAt = parsedCreatedAt;
                        }
                    }
                    if (updatedAtCol > 0)
                    {
                        var updatedAtCell = worksheet.Cell(row, updatedAtCol);
                        var updatedAtStr = updatedAtCell.GetString();
                        if (!string.IsNullOrWhiteSpace(updatedAtStr) && DateTime.TryParse(updatedAtStr, out var parsedUpdatedAt))
                        {
                            updatedAt = parsedUpdatedAt;
                        }
                    }
                    */

                    var sale = new Sale
                    {
                        // 預設 Id=0，若是完整匯出檔會在下方嘗試帶入 Excel 的 ID
                        Id = 0,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalAmount = quantity * unitPrice,
                        SaleDate = saleDate,
                        CustomerName = customerName,
                        CreatedAt = createdAt,
                        UpdatedAt = updatedAt,
                        CreatedBy = "System Import",
                        UpdatedBy = "System Import"
                    };

                    // 如果是完整匯出檔案，嘗試使用 Excel 中的銷售 ID，讓清空後重新匯入時 ID 一致
                    if (idCol > 0)
                    {
                        try
                        {
                            var idCell = worksheet.Cell(row, idCol);
                            var idStr = idCell.GetString();
                            if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out var parsedId) && parsedId > 0)
                            {
                                sale.Id = parsedId;
                                _logger.LogInformation("使用匯出檔中的銷售 ID: {Id} (SKU={SKU})", parsedId, sku);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "讀取銷售 ID 失敗，將改用資料庫自動產生的 ID。Row={Row}, SKU={SKU}", row, sku);
                        }
                    }

                    var saleResult = await _salesService.CreateSaleAsync(sale);
                    if (saleResult.IsValid)
                    {
                    count++;
                        _logger.LogInformation("成功處理銷售資料第 {Row} 行: SKU={SKU}, Quantity={Quantity}", row, sku, quantity);
                    }
                    else
                    {
                        var errors = string.Join(", ", saleResult.Errors);
                        _logger.LogWarning("銷售資料第 {Row} 行創建失敗: SKU={SKU}, Errors={Errors}", row, sku, errors);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, $"創建失敗: {errors}");
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" | 內部錯誤: {ex.InnerException.Message}";
                    }
                    
                    // 嘗試從 Excel 讀取 SKU（如果可能）
                    string? skuForLog = null;
                    try
                    {
                        var skuCell = worksheet.Cell(row, skuCol);
                        skuForLog = skuCell.GetString()?.Trim();
                    }
                    catch { }
                    
                    // 檢查是否是實體追蹤衝突
                    if (errorMessage.Contains("cannot be tracked") || errorMessage.Contains("already being tracked"))
                    {
                        _logger.LogWarning("銷售資料第 {Row} 行發生實體追蹤衝突: SKU={SKU}, Error={Error}", row, skuForLog ?? "未知", errorMessage);
                        // 實體追蹤衝突通常是因為同一個實體被多次追蹤
                        // 這可能是因為在匯入過程中，某些實體被重用
                        // 建議：檢查是否有實體被多次查詢和追蹤
                    }
                    
                    // 提取更簡潔的錯誤訊息（避免過長的內部錯誤）
                    var shortErrorMessage = errorMessage;
                    if (errorMessage.Length > 200)
                    {
                        shortErrorMessage = errorMessage.Substring(0, 200) + "...";
                    }
                    
                    _logger.LogError(ex, "匯入銷售資料第 {Row} 行時發生錯誤: SKU={SKU}, ErrorMessage={ErrorMessage}, StackTrace={StackTrace}", row, skuForLog ?? "未知", errorMessage, ex.StackTrace);
                    skippedCount++; // 發生異常時也計入跳過數量
                    IncrementSkippedReason(skippedReasons, $"異常: {shortErrorMessage}");
                }
            }

            _logger.LogInformation("銷售資料匯入完成，成功匯入 {Count} 筆，跳過 {SkippedCount} 筆", count, skippedCount);
            if (skippedCount > 0 && skippedReasons.Count > 0)
            {
                _logger.LogInformation("跳過原因統計: {Reasons}", string.Join(", ", skippedReasons.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
            }
            
            return (count, skippedCount, skippedReasons);
        }

        private async Task<(int count, int skippedCount, Dictionary<string, int> skippedReasons)> ImportPurchases(IXLWorksheet worksheet, bool isFullExport = false)
        {
            var count = 0;
            var skippedCount = 0;
            var skippedReasons = new Dictionary<string, int>();
            
            // 在開始時重新獲取產品列表，確保包含最新匯入的產品
            var products = await _productService.GetAllProductsAsync();
            // 使用 Trim 後的 SKU 作為 key，確保匹配，使用不區分大小寫的比較器
            var productMap = products.ToDictionary(p => p.SKU?.Trim() ?? "", p => p.Id, StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation("進貨匯入開始 - 資料庫中現有產品數量: {Count}", productMap.Count);
            _logger.LogInformation("資料庫中的 SKU 列表（前20個）: {SKUList}", string.Join(", ", productMap.Keys.Take(20)));

            // 判斷欄位位置（完整匯出格式 vs 範本格式）
            int idCol = isFullExport ? 1 : -1;   // 完整匯出時，第 1 欄為進貨 ID
            int skuCol = isFullExport ? 4 : 1;   // 完整匯出：ID, 產品ID, 產品名稱, 產品編號, ...
            int quantityCol = isFullExport ? 5 : 2;
            int unitPriceCol = isFullExport ? 6 : 3;
            int supplierCol = isFullExport ? 8 : 4;
            int employeeRetentionCol = isFullExport ? 9 : 5;
            int purchaseDateCol = isFullExport ? 10 : 6;
            int createdAtCol = isFullExport ? 11 : -1;
            int updatedAtCol = isFullExport ? 12 : -1;

            // 使用多種方法檢查數據行數
            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var rowsUsed = worksheet.RowsUsed().Count();
            var rangeUsed = worksheet.RangeUsed();
            var maxRow = rangeUsed?.LastRow().RowNumber() ?? 0;
            
            _logger.LogInformation("開始匯入進貨資料 - LastRowUsed: {LastRow}, RowsUsed: {RowsUsed}, RangeUsed.MaxRow: {MaxRow}, SKU欄位: {SkuCol}", 
                lastRowUsed, rowsUsed, maxRow, skuCol);
            
            // 使用最大的行數值
            var actualLastRow = Math.Max(Math.Max(lastRowUsed, maxRow), rowsUsed);
            
            // 如果只有標題行，直接返回
            if (actualLastRow <= 1)
            {
                _logger.LogWarning("進貨資料工作表只有標題行，沒有數據 (actualLastRow: {ActualLastRow})", actualLastRow);
                return (0, 0, new Dictionary<string, int>());
            }
            
            _logger.LogInformation("將處理第 2 行到第 {LastRow} 行的數據", actualLastRow);
            
            for (int row = 2; row <= actualLastRow; row++)
            {
                try
                {
                    var skuCell = worksheet.Cell(row, skuCol);
                    var sku = skuCell.GetString()?.Trim();
                    var skuValue = skuCell.Value.ToString();
                    var cellIsBlank = skuCell.Value.IsBlank;
                    
                    _logger.LogInformation("第 {Row} 行，SKU欄位({SkuCol})，GetString(): '{SKU}', Value: '{Value}', IsBlank: {IsBlank}", 
                        row, skuCol, sku, skuValue, cellIsBlank);
                    
                    if (string.IsNullOrWhiteSpace(sku))
                    {
                        _logger.LogWarning("第 {Row} 行 SKU 為空，跳過", row);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "SKU為空");
                        continue;
                    }
                    
                    // 由於 productMap 使用大小寫不敏感比較器，直接查找即可
                    if (!productMap.TryGetValue(sku, out var productId))
                    {
                        // 如果第一次查找失敗，重新獲取產品列表（可能產品剛被匯入）
                        products = await _productService.GetAllProductsAsync();
                        productMap = products.ToDictionary(p => p.SKU?.Trim() ?? "", p => p.Id, StringComparer.OrdinalIgnoreCase);
                        _logger.LogInformation("重新獲取產品列表，現在有 {Count} 個產品", productMap.Count);
                        
                        // 再次嘗試查找
                        if (!productMap.TryGetValue(sku, out productId))
                        {
                            // 嘗試更寬鬆的匹配：移除所有空格和特殊字符
                            var cleanSku = sku?.Replace(" ", "").Replace("-", "").Replace("_", "").ToUpperInvariant();
                            var matchedSku = productMap.Keys.FirstOrDefault(k => 
                                k?.Replace(" ", "").Replace("-", "").Replace("_", "").ToUpperInvariant() == cleanSku);
                            
                            if (matchedSku != null && productMap.TryGetValue(matchedSku, out productId))
                            {
                                _logger.LogInformation("使用寬鬆匹配找到產品: Excel SKU '{ExcelSku}' -> 資料庫 SKU '{DbSku}', 產品ID: {ProductId}", 
                                    sku, matchedSku, productId);
                            }
                            else
                            {
                                _logger.LogWarning("產品 SKU '{SKU}' 不存在於資料庫中。Excel SKU 長度: {Length}, 資料庫中的 SKU 列表（前20個）: {SKUList}", 
                                    sku, sku?.Length ?? 0, string.Join(", ", productMap.Keys.Take(20)));
                                skippedCount++;
                                IncrementSkippedReason(skippedReasons, "產品SKU不存在");
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogInformation("重新獲取產品列表後，成功匹配產品 SKU '{SKU}'，產品ID: {ProductId}", sku, productId);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("成功匹配產品 SKU '{SKU}'，產品ID: {ProductId}", sku, productId);
                    }

                    int quantity = 0;
                    decimal unitPrice = 0;
                    
                    // 改進數量轉換邏輯，支援多種格式
                    var quantityCell = worksheet.Cell(row, quantityCol);
                    var quantityValue = quantityCell.Value;
                    var quantityStr = quantityCell.GetString();
                    var quantityIsBlank = quantityCell.Value.IsBlank;
                    
                    _logger.LogInformation("第 {Row} 行，數量欄位({QuantityCol})，GetString(): '{QuantityStr}', Value: '{Value}', IsBlank: {IsBlank}", 
                        row, quantityCol, quantityStr, quantityValue, quantityIsBlank);
                    
                    bool quantityValid = false;
                    if (!quantityIsBlank)
                    {
                        try
                        {
                            // 方法1: 嘗試直接轉換為 int（適用於數字格式）
                            try
                            {
                                quantity = Convert.ToInt32(quantityValue);
                                quantityValid = true;
                                _logger.LogInformation("第 {Row} 行數量直接轉換成功: {Quantity}", row, quantity);
                            }
                            catch
                            {
                                // 如果直接轉換失敗，嘗試從字符串解析
                            }
                            
                            // 方法2: 如果直接轉換失敗，嘗試從字符串解析（適用於文本格式）
                            if (!quantityValid && !string.IsNullOrWhiteSpace(quantityStr))
                            {
                                // 移除可能的千分位符號和空格
                                var cleanQuantityStr = quantityStr.Replace(",", "").Replace(" ", "").Trim();
                                if (int.TryParse(cleanQuantityStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedQuantity))
                                {
                                    quantity = parsedQuantity;
                                    quantityValid = true;
                                    _logger.LogInformation("第 {Row} 行數量從字符串解析成功: '{CleanStr}' -> {Quantity}", row, cleanQuantityStr, quantity);
                                }
                                else
                                {
                                    // 嘗試使用當前文化設置解析
                                    if (int.TryParse(cleanQuantityStr, out parsedQuantity))
                                    {
                                        quantity = parsedQuantity;
                                        quantityValid = true;
                                        _logger.LogInformation("第 {Row} 行數量使用當前文化設置解析成功: '{CleanStr}' -> {Quantity}", row, cleanQuantityStr, quantity);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "第 {Row} 行數量轉換發生異常: {Value}", row, quantityValue);
                        }
                    }
                    
                    if (!quantityValid)
                    {
                        _logger.LogWarning("第 {Row} 行數量轉換失敗: Value='{Value}', String='{Str}'，跳過此記錄", row, quantityValue, quantityStr);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "數量轉換失敗");
                        continue;
                    }
                    
                    // 改進單價轉換邏輯，支援多種格式
                    var unitPriceCell = worksheet.Cell(row, unitPriceCol);
                    var unitPriceValue = unitPriceCell.Value;
                    var unitPriceStr = unitPriceCell.GetString();
                    var unitPriceIsBlank = unitPriceCell.Value.IsBlank;
                    
                    _logger.LogInformation("第 {Row} 行，單價欄位({UnitPriceCol})，GetString(): '{UnitPriceStr}', Value: '{Value}', IsBlank: {IsBlank}", 
                        row, unitPriceCol, unitPriceStr, unitPriceValue, unitPriceIsBlank);
                    
                    bool unitPriceValid = false;
                    if (!unitPriceIsBlank)
                    {
                        try
                        {
                            // 方法1: 嘗試直接轉換為 decimal（適用於數字格式）
                            try
                            {
                                unitPrice = Convert.ToDecimal(unitPriceValue);
                                unitPriceValid = true;
                                _logger.LogInformation("第 {Row} 行單價直接轉換成功: {UnitPrice}", row, unitPrice);
                            }
                            catch
                            {
                                // 如果直接轉換失敗，嘗試從字符串解析
                            }
                            
                            // 方法2: 如果直接轉換失敗，嘗試從字符串解析（適用於文本格式）
                            if (!unitPriceValid && !string.IsNullOrWhiteSpace(unitPriceStr))
                            {
                                // 移除可能的貨幣符號和千分位符號
                                var cleanUnitPriceStr = unitPriceStr.Replace("$", "").Replace(",", "").Replace("NT$", "").Replace("NT", "").Replace(" ", "").Trim();
                                if (decimal.TryParse(cleanUnitPriceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedUnitPrice))
                                {
                                    unitPrice = parsedUnitPrice;
                                    unitPriceValid = true;
                                    _logger.LogInformation("第 {Row} 行單價從字符串解析成功: '{CleanStr}' -> {UnitPrice}", row, cleanUnitPriceStr, unitPrice);
                                }
                                else
                                {
                                    // 嘗試使用當前文化設置解析
                                    if (decimal.TryParse(cleanUnitPriceStr, out parsedUnitPrice))
                                    {
                                        unitPrice = parsedUnitPrice;
                                        unitPriceValid = true;
                                        _logger.LogInformation("第 {Row} 行單價使用當前文化設置解析成功: '{CleanStr}' -> {UnitPrice}", row, cleanUnitPriceStr, unitPrice);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "第 {Row} 行單價轉換發生異常: {Value}", row, unitPriceValue);
                        }
                    }
                    
                    if (!unitPriceValid)
                    {
                        _logger.LogWarning("第 {Row} 行單價轉換失敗: Value='{Value}', String='{Str}'，跳過此記錄", row, unitPriceValue, unitPriceStr);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "單價轉換失敗");
                        continue;
                    }
                    
                    var supplierCell = worksheet.Cell(row, supplierCol);
                    var supplier = supplierCell.GetString() ?? "";
                    var employeeRetentionCell = worksheet.Cell(row, employeeRetentionCol);
                    var employeeRetention = employeeRetentionCell.GetString();
                    var purchaseDateCell = worksheet.Cell(row, purchaseDateCol);
                    var purchaseDateStr = purchaseDateCell.GetString();
                    var purchaseDate = string.IsNullOrWhiteSpace(purchaseDateStr) ? DateTime.Now : (DateTime.TryParse(purchaseDateStr, out var date) ? date : DateTime.Now);
                    
                    _logger.LogInformation("第 {Row} 行進貨資料: SKU={SKU}, ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}, Supplier='{Supplier}'", 
                        row, sku, productId, quantity, unitPrice, supplier);
                    
                    // 驗證必要欄位
                    if (string.IsNullOrWhiteSpace(supplier))
                    {
                        _logger.LogWarning("第 {Row} 行供應商為空，跳過此記錄", row);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "供應商為空");
                        continue;
                    }
                    
                    if (quantity <= 0)
                    {
                        _logger.LogWarning("第 {Row} 行數量為 {Quantity}，必須大於 0，跳過此記錄", row, quantity);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "數量必須大於0");
                        continue;
                    }
                    
                    if (unitPrice <= 0)
                    {
                        _logger.LogWarning("第 {Row} 行單價為 {UnitPrice}，必須大於 0，跳過此記錄", row, unitPrice);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, "單價必須大於0");
                        continue;
                    }

                    // 處理時間戳記（如果存在）
                    // 注意：使用當前時間而不是 Excel 中的時間，避免重複提交防護誤判
                    DateTime createdAt = DateTime.Now;
                    DateTime updatedAt = DateTime.Now;
                    // 如果 Excel 中有時間戳記，可以選擇使用，但為了避免重複提交防護問題，我們使用當前時間
                    // 如果需要保留原始時間，可以取消下面的註釋
                    /*
                    if (createdAtCol > 0)
                    {
                        var createdAtCell = worksheet.Cell(row, createdAtCol);
                        var createdAtStr = createdAtCell.GetString();
                        if (!string.IsNullOrWhiteSpace(createdAtStr) && DateTime.TryParse(createdAtStr, out var parsedCreatedAt))
                        {
                            createdAt = parsedCreatedAt;
                        }
                    }
                    if (updatedAtCol > 0)
                    {
                        var updatedAtCell = worksheet.Cell(row, updatedAtCol);
                        var updatedAtStr = updatedAtCell.GetString();
                        if (!string.IsNullOrWhiteSpace(updatedAtStr) && DateTime.TryParse(updatedAtStr, out var parsedUpdatedAt))
                        {
                            updatedAt = parsedUpdatedAt;
                        }
                    }
                    */

                    var purchase = new Purchase
                    {
                        // 預設 Id=0，若是完整匯出檔會在下方嘗試帶入 Excel 的 ID
                        Id = 0,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalAmount = quantity * unitPrice,
                        Supplier = supplier,
                        EmployeeRetention = string.IsNullOrWhiteSpace(employeeRetention) ? null : employeeRetention,
                        PurchaseDate = purchaseDate,
                        CreatedAt = createdAt,
                        UpdatedAt = updatedAt,
                        CreatedBy = "System Import",
                        UpdatedBy = "System Import"
                    };

                    // 如果是完整匯出檔案，嘗試使用 Excel 中的進貨 ID，讓清空後重新匯入時 ID 一致
                    if (idCol > 0)
                    {
                        try
                        {
                            var idCell = worksheet.Cell(row, idCol);
                            var idStr = idCell.GetString();
                            if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out var parsedId) && parsedId > 0)
                            {
                                purchase.Id = parsedId;
                                _logger.LogInformation("使用匯出檔中的進貨 ID: {Id} (SKU={SKU})", parsedId, sku);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "讀取進貨 ID 失敗，將改用資料庫自動產生的 ID。Row={Row}, SKU={SKU}", row, sku);
                        }
                    }

                    var purchaseResult = await _purchasesService.CreatePurchaseAsync(purchase);
                    if (purchaseResult.IsValid)
                    {
                    count++;
                        _logger.LogInformation("成功處理進貨資料第 {Row} 行: SKU={SKU}, Quantity={Quantity}", row, sku, quantity);
                    }
                    else
                    {
                        var errors = string.Join(", ", purchaseResult.Errors);
                        _logger.LogWarning("進貨資料第 {Row} 行創建失敗: SKU={SKU}, Errors={Errors}", row, sku, errors);
                        skippedCount++;
                        IncrementSkippedReason(skippedReasons, $"創建失敗: {errors}");
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" | 內部錯誤: {ex.InnerException.Message}";
                    }
                    
                    // 嘗試從 Excel 讀取 SKU（如果可能）
                    string? skuForLog = null;
                    try
                    {
                        var skuCell = worksheet.Cell(row, skuCol);
                        skuForLog = skuCell.GetString()?.Trim();
                    }
                    catch { }
                    
                    // 檢查是否是實體追蹤衝突
                    if (errorMessage.Contains("cannot be tracked") || errorMessage.Contains("already being tracked"))
                    {
                        _logger.LogWarning("進貨資料第 {Row} 行發生實體追蹤衝突: SKU={SKU}, Error={Error}", row, skuForLog ?? "未知", errorMessage);
                        // 實體追蹤衝突通常是因為同一個實體被多次追蹤
                        // 這可能是因為在匯入過程中，某些實體被重用
                        // 建議：檢查是否有實體被多次查詢和追蹤
                    }
                    
                    // 提取更簡潔的錯誤訊息（避免過長的內部錯誤）
                    var shortErrorMessage = errorMessage;
                    if (errorMessage.Length > 200)
                    {
                        shortErrorMessage = errorMessage.Substring(0, 200) + "...";
                    }
                    
                    _logger.LogError(ex, "匯入進貨資料第 {Row} 行時發生錯誤: SKU={SKU}, ErrorMessage={ErrorMessage}, StackTrace={StackTrace}", row, skuForLog ?? "未知", errorMessage, ex.StackTrace);
                    skippedCount++; // 發生異常時也計入跳過數量
                    IncrementSkippedReason(skippedReasons, $"異常: {shortErrorMessage}");
                }
            }

            _logger.LogInformation("進貨資料匯入完成，成功匯入 {Count} 筆，跳過 {SkippedCount} 筆", count, skippedCount);
            if (skippedCount > 0 && skippedReasons.Count > 0)
            {
                _logger.LogInformation("跳過原因統計: {Reasons}", string.Join(", ", skippedReasons.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
            }
            
            return (count, skippedCount, skippedReasons);
        }

        /// <summary>
        /// 顯示清空數據確認頁面
        /// </summary>
        [HttpGet]
        public IActionResult ClearData()
        {
            return View();
        }

        /// <summary>
        /// 執行清空所有系統數據（危險操作）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearData(bool confirmClear, bool createBackup = true)
        {
            if (!confirmClear)
            {
                TempData["ErrorMessage"] = "請確認要清空所有數據";
                return View();
            }

            try
            {
                _logger.LogWarning("用戶請求清空所有系統數據");

                var result = await _databaseManagementService.ClearAllDataAsync(createBackup);

                if (result.Success)
                {
                    var message = $"成功清空所有系統數據！<br/>" +
                                 $"刪除了 {result.DeletedRecords} 條記錄<br/>" +
                                 $"釋放了 {result.FreedSpaceBytes:N0} 字節空間<br/>" +
                                 $"<br/>操作詳情：<br/>" +
                                 string.Join("<br/>", result.CleanupActions);

                    TempData["SuccessMessage"] = message;
                    _logger.LogWarning("成功清空所有系統數據，共刪除 {DeletedRecords} 條記錄", result.DeletedRecords);
                }
                else
                {
                    TempData["ErrorMessage"] = $"清空數據失敗：{result.Message}";
                    if (result.Errors.Any())
                    {
                        TempData["ErrorMessage"] += "<br/>錯誤詳情：<br/>" + string.Join("<br/>", result.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空系統數據時發生錯誤");
                TempData["ErrorMessage"] = $"清空數據時發生錯誤：{ex.Message}";
            }

            return RedirectToAction("Import");
        }

        private void IncrementSkippedReason(Dictionary<string, int> skippedReasons, string reason)
        {
            if (skippedReasons.ContainsKey(reason))
            {
                skippedReasons[reason]++;
            }
            else
            {
                skippedReasons[reason] = 1;
            }
        }
    }
}