using CioSystem.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CioSystem.Web.Controllers
{
    /// <summary>
    /// 資料庫管理 MVC 控制器
    /// 提供資料庫管理的視圖頁面
    /// </summary>
    public class DatabaseManagementMvcController : Controller
    {
        private readonly IDatabaseManagementService _databaseManagementService;
        private readonly ILogger<DatabaseManagementMvcController> _logger;

        public DatabaseManagementMvcController(
            IDatabaseManagementService databaseManagementService,
            ILogger<DatabaseManagementMvcController> logger)
        {
            _databaseManagementService = databaseManagementService;
            _logger = logger;
        }

        /// <summary>
        /// 資料庫管理主頁
        /// </summary>
        /// <returns>資料庫管理視圖</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 創建備份（MVC 版本）
        /// </summary>
        /// <param name="backupName">備份名稱</param>
        /// <param name="includeData">是否包含數據</param>
        /// <param name="compress">是否壓縮</param>
        /// <returns>重定向到主頁</returns>
        [HttpPost]
        public async Task<IActionResult> CreateBackup(string? backupName, bool includeData = true, bool compress = true)
        {
            try
            {
                var backupFilePath = await _databaseManagementService.CreateBackupAsync(backupName, includeData, compress);
                TempData["SuccessMessage"] = $"備份創建成功：{backupFilePath}";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "創建備份失敗");
                TempData["ErrorMessage"] = $"備份創建失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 還原資料庫（MVC 版本）
        /// </summary>
        /// <param name="backupFilePath">備份文件路徑</param>
        /// <param name="createBackupBeforeRestore">還原前是否創建備份</param>
        /// <returns>重定向到主頁</returns>
        [HttpPost]
        public async Task<IActionResult> RestoreDatabase(string backupFilePath, bool createBackupBeforeRestore = true)
        {
            try
            {
                var result = await _databaseManagementService.RestoreFromBackupAsync(backupFilePath, createBackupBeforeRestore);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"資料庫還原成功，還原了 {result.RestoredRecords} 條記錄";
                }
                else
                {
                    TempData["ErrorMessage"] = $"資料庫還原失敗：{result.Message}";
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "還原資料庫失敗");
                TempData["ErrorMessage"] = $"資料庫還原失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 清理資料庫（MVC 版本）
        /// </summary>
        /// <param name="keepInventoryMovementsDays">保留庫存移動記錄天數</param>
        /// <param name="keepLogsDays">保留日誌記錄天數</param>
        /// <param name="cleanEmptyInventory">清理空的庫存記錄</param>
        /// <param name="cleanInvalidProducts">清理無效的產品記錄</param>
        /// <param name="optimizeDatabase">優化資料庫</param>
        /// <param name="createBackupBeforeCleanup">清理前創建備份</param>
        /// <returns>重定向到主頁</returns>
        [HttpPost]
        public async Task<IActionResult> CleanupDatabase(
            int? keepInventoryMovementsDays = null,
            int? keepLogsDays = null,
            bool cleanEmptyInventory = true,
            bool cleanInvalidProducts = true,
            bool optimizeDatabase = true,
            bool createBackupBeforeCleanup = true)
        {
            try
            {
                var options = new DatabaseCleanupOptions
                {
                    KeepInventoryMovementsDays = keepInventoryMovementsDays,
                    KeepLogsDays = keepLogsDays,
                    CleanEmptyInventory = cleanEmptyInventory,
                    CleanInvalidProducts = cleanInvalidProducts,
                    OptimizeDatabase = optimizeDatabase,
                    CreateBackupBeforeCleanup = createBackupBeforeCleanup
                };

                var result = await _databaseManagementService.CleanupDatabaseAsync(options);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"資料庫清理成功，刪除了 {result.DeletedRecords} 條記錄，釋放了 {result.FreedSpaceBytes} 字節空間";
                }
                else
                {
                    TempData["ErrorMessage"] = $"資料庫清理失敗：{result.Message}";
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "清理資料庫失敗");
                TempData["ErrorMessage"] = $"資料庫清理失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 優化資料庫（MVC 版本）
        /// </summary>
        /// <returns>重定向到主頁</returns>
        [HttpPost]
        public async Task<IActionResult> OptimizeDatabase()
        {
            try
            {
                var result = await _databaseManagementService.OptimizeDatabaseAsync();

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"資料庫優化成功，釋放了 {result.FreedSpaceBytes} 字節空間";
                }
                else
                {
                    TempData["ErrorMessage"] = $"資料庫優化失敗：{result.Message}";
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "優化資料庫失敗");
                TempData["ErrorMessage"] = $"資料庫優化失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 刪除備份（MVC 版本）
        /// </summary>
        /// <param name="backupFilePath">備份文件路徑</param>
        /// <returns>重定向到主頁</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteBackup(string backupFilePath)
        {
            try
            {
                var success = await _databaseManagementService.DeleteBackupAsync(backupFilePath);

                if (success)
                {
                    TempData["SuccessMessage"] = "備份文件刪除成功";
                }
                else
                {
                    TempData["ErrorMessage"] = "備份文件刪除失敗";
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "刪除備份失敗");
                TempData["ErrorMessage"] = $"備份文件刪除失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}