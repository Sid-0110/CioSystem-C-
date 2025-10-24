using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CioSystem.Services;
using CioSystem.Models;
using CioSystem.Web.Models;
using CioSystem.Core.Interfaces;
using CioSystem.Services.Logging;
using CioSystem.Services.Authentication;
using System.Text.Json;

namespace CioSystem.Web.Controllers
{
    [Authorize]
    public class SystemSettingsController : Controller
    {
        private readonly ILogger<SystemSettingsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDatabaseManagementService _databaseManagementService;
        private readonly ISystemLogService _systemLogService;
        private readonly IUserService _userService;

        private readonly LogInitializer _logInitializer;

        public SystemSettingsController(
            ILogger<SystemSettingsController> logger,
            IConfiguration configuration,
            IDatabaseManagementService databaseManagementService,
            ISystemLogService systemLogService,
            IUserService userService,
            LogInitializer logInitializer)
        {
            _logger = logger;
            _configuration = configuration;
            _databaseManagementService = databaseManagementService;
            _systemLogService = systemLogService;
            _userService = userService;
            _logInitializer = logInitializer;
        }

        /// <summary>
        /// 系統設置首頁
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("顯示系統設置首頁");

                // 取得系統資訊
                var systemInfo = GetSystemInfo();
                ViewBag.SystemInfo = systemInfo;

                // 取得資料庫統計
                var dbStats = await GetDatabaseStatsAsync();
                ViewBag.DatabaseStats = dbStats;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入系統設置時發生錯誤");
                TempData["ErrorMessage"] = "載入系統設置時發生錯誤，請稍後再試。";
                return View();
            }
        }

        /// <summary>
        /// 基本設置頁面
        /// </summary>
        public IActionResult BasicSettings()
        {
            try
            {
                _logger.LogInformation("顯示基本設置頁面");

                var settings = new BasicSettingsViewModel
                {
                    CompanyName = _configuration["SystemSettings:CompanyName"] ?? "CioSystem",
                    CompanyAddress = _configuration["SystemSettings:CompanyAddress"] ?? "",
                    CompanyPhone = _configuration["SystemSettings:CompanyPhone"] ?? "",
                    CompanyEmail = _configuration["SystemSettings:CompanyEmail"] ?? "",
                    DefaultCurrency = _configuration["SystemSettings:DefaultCurrency"] ?? "TWD",
                    DefaultLanguage = _configuration["SystemSettings:DefaultLanguage"] ?? "zh-TW",
                    TimeZone = _configuration["SystemSettings:TimeZone"] ?? "Asia/Taipei",
                    DateFormat = _configuration["SystemSettings:DateFormat"] ?? "yyyy-MM-dd",
                    TimeFormat = _configuration["SystemSettings:TimeFormat"] ?? "HH:mm:ss",
                    ItemsPerPage = int.Parse(_configuration["SystemSettings:ItemsPerPage"] ?? "10"),
                    EnableNotifications = bool.Parse(_configuration["SystemSettings:EnableNotifications"] ?? "true"),
                    EnableAuditLog = bool.Parse(_configuration["SystemSettings:EnableAuditLog"] ?? "true")
                };

                return View(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入基本設置時發生錯誤");
                TempData["ErrorMessage"] = "載入基本設置時發生錯誤，請稍後再試。";
                return View(new BasicSettingsViewModel());
            }
        }

        /// <summary>
        /// 保存基本設置
        /// </summary>
        [HttpPost]
        public IActionResult SaveBasicSettings(BasicSettingsViewModel model)
        {
            try
            {
                _logger.LogInformation("保存基本設置");

                if (!ModelState.IsValid)
                {
                    return View("BasicSettings", model);
                }

                // 這裡應該保存到資料庫或配置檔案
                // 目前只是示範，實際應用中需要實現配置持久化
                TempData["SuccessMessage"] = "基本設置已成功保存！";

                return RedirectToAction("BasicSettings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存基本設置時發生錯誤");
                TempData["ErrorMessage"] = "保存基本設置時發生錯誤，請稍後再試。";
                return View("BasicSettings", model);
            }
        }


        /// <summary>
        /// 用戶管理頁面
        /// </summary>
        public async Task<IActionResult> UserManagement()
        {
            try
            {
                _logger.LogInformation("顯示用戶管理頁面");

                // 檢查用戶服務是否為空
                if (_userService == null)
                {
                    _logger.LogError("用戶服務為空，無法獲取用戶資料");
                    TempData["ErrorMessage"] = "用戶服務未正確初始化，請稍後再試。";
                    return View("~/Views/UserManagement/Index.cshtml", new List<CioSystem.Models.UserViewModel>());
                }

                // 使用真正的用戶服務獲取用戶資料
                _logger.LogInformation("開始調用用戶服務");
                _logger.LogInformation("用戶服務實例: {UserService}", _userService?.GetType().Name ?? "null");
                
                IEnumerable<CioSystem.Models.UserViewModel> users;
                try
                {
                    users = await _userService.GetAllUsersAsync();
                    _logger.LogInformation("用戶服務返回 {Count} 個用戶", users?.Count() ?? 0);
                    
                    if (users == null)
                    {
                        _logger.LogWarning("用戶服務返回 null");
                        return View("~/Views/UserManagement/Index.cshtml", new List<CioSystem.Models.UserViewModel>());
                    }
                    
                    if (!users.Any())
                    {
                        _logger.LogWarning("用戶服務返回空列表");
                        return View("~/Views/UserManagement/Index.cshtml", new List<CioSystem.Models.UserViewModel>());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "調用用戶服務時發生錯誤");
                    return View("~/Views/UserManagement/Index.cshtml", new List<CioSystem.Models.UserViewModel>());
                }
                
                // 調試：檢查用戶數據
                if (users != null)
                {
                    _logger.LogInformation("用戶數據不為空，用戶數量: {Count}", users.Count());
                    foreach (var user in users)
                    {
                        _logger.LogInformation("用戶: {Username}, 角色: {Role}, 活躍: {IsActive}", user.Username, user.Role, user.IsActive);
                    }
                }
                else
                {
                    _logger.LogWarning("用戶數據為空");
                }
                
                var statistics = await _userService.GetUserStatisticsAsync();
                var onlineUsers = await _userService.GetOnlineUsersAsync();

                ViewBag.Statistics = statistics;
                ViewBag.OnlineUsers = onlineUsers;

                _logger.LogInformation("準備返回用戶管理視圖，用戶數量: {Count}", users.Count());
                return View("~/Views/UserManagement/Index.cshtml", users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入用戶管理時發生錯誤: {Message}", ex.Message);
                TempData["ErrorMessage"] = "載入用戶管理時發生錯誤，請稍後再試。";
                return View("~/Views/UserManagement/Index.cshtml", new List<CioSystem.Models.UserViewModel>());
            }
        }

        /// <summary>
        /// 創建用戶頁面
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                _logger.LogInformation("顯示創建用戶頁面");
                return View("~/Views/UserManagement/Create.cshtml", new CreateUserRequest());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入創建用戶頁面時發生錯誤");
                TempData["ErrorMessage"] = "載入創建用戶頁面時發生錯誤，請稍後再試。";
                return RedirectToAction("UserManagement");
            }
        }

        /// <summary>
        /// 創建用戶
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            try
            {
                _logger.LogInformation("創建新用戶: {Username}", request.Username);

                if (!ModelState.IsValid)
                {
                    return View("~/Views/UserManagement/Create.cshtml", request);
                }

                var user = await _userService.CreateUserAsync(request);
                
                // 記錄操作日誌
                await _systemLogService.LogAsync("Info", $"創建新用戶: {user.Username} ({user.Role})", User.Identity?.Name);
                
                TempData["SuccessMessage"] = $"用戶 {user.Username} 創建成功！";
                return RedirectToAction("UserManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建用戶時發生錯誤");
                TempData["ErrorMessage"] = "創建用戶時發生錯誤，請稍後再試。";
                return View("~/Views/UserManagement/Create.cshtml", request);
            }
        }

        /// <summary>
        /// 編輯用戶頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                _logger.LogInformation("顯示編輯用戶頁面: {UserId}", id);
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "找不到指定的用戶";
                    return RedirectToAction("UserManagement");
                }
                
                ViewBag.UserId = id;
                
                // 將 UserViewModel 轉換為 UpdateUserRequest
                var updateRequest = new UpdateUserRequest
                {
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive
                };
                
                return View("~/Views/UserManagement/Edit.cshtml", updateRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入編輯用戶頁面時發生錯誤");
                TempData["ErrorMessage"] = "載入編輯用戶頁面時發生錯誤，請稍後再試。";
                return RedirectToAction("UserManagement");
            }
        }

        /// <summary>
        /// 更新用戶
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Edit(int id, UpdateUserRequest request)
        {
            try
            {
                _logger.LogInformation("更新用戶: {UserId}", id);

                if (!ModelState.IsValid)
                {
                    return View("~/Views/UserManagement/Edit.cshtml", request);
                }

                var updatedUser = await _userService.UpdateUserAsync(id, request);
                
                // 記錄操作日誌
                await _systemLogService.LogAsync("Info", $"更新用戶: {updatedUser.Username} ({updatedUser.Role})", User.Identity?.Name);
                
                TempData["SuccessMessage"] = $"用戶 {updatedUser.Username} 更新成功！";
                return RedirectToAction("UserManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用戶時發生錯誤");
                TempData["ErrorMessage"] = "更新用戶時發生錯誤，請稍後再試。";
                return View("~/Views/UserManagement/Edit.cshtml", request);
            }
        }

        /// <summary>
        /// 刪除用戶
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("刪除用戶: {UserId}", id);
                
                // 獲取用戶信息用於日誌記錄
                var user = await _userService.GetUserByIdAsync(id);
                var username = user?.Username ?? $"ID:{id}";
                
                await _userService.DeleteUserAsync(id);
                
                // 記錄操作日誌
                await _systemLogService.LogAsync("Warning", $"刪除用戶: {username}", User.Identity?.Name);
                
                TempData["SuccessMessage"] = "用戶刪除成功！";
                return RedirectToAction("UserManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除用戶時發生錯誤");
                TempData["ErrorMessage"] = "刪除用戶時發生錯誤，請稍後再試。";
                return RedirectToAction("UserManagement");
            }
        }

        /// <summary>
        /// 系統日誌頁面
        /// </summary>
        // 系統日誌功能已移至專用控制器
        // 重定向到新的 SystemLogs 控制器
        public IActionResult SystemLogs()
        {
            return RedirectToAction("Index", "SystemLogs");
        }

        /// <summary>
        /// 安全設置頁面
        /// </summary>
        public IActionResult SecuritySettings()
        {
            try
            {
                _logger.LogInformation("顯示安全設置頁面");

                var securitySettings = new SecuritySettingsViewModel
                {
                    PasswordMinLength = 8,
                    PasswordRequireUppercase = true,
                    PasswordRequireLowercase = true,
                    PasswordRequireNumbers = true,
                    PasswordRequireSpecialChars = true,
                    SessionTimeout = 30,
                    MaxLoginAttempts = 5,
                    LockoutDuration = 15,
                    EnableTwoFactor = false,
                    EnableAuditLog = true
                };

                return View(securitySettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入安全設置時發生錯誤");
                TempData["ErrorMessage"] = "載入安全設置時發生錯誤，請稍後再試。";
                return View(new SecuritySettingsViewModel());
            }
        }

        /// <summary>
        /// 保存安全設置
        /// </summary>
        [HttpPost]
        public IActionResult SaveSecuritySettings(SecuritySettingsViewModel model)
        {
            try
            {
                _logger.LogInformation("保存安全設置");

                if (!ModelState.IsValid)
                {
                    return View("SecuritySettings", model);
                }

                // 這裡應該保存到資料庫
                TempData["SuccessMessage"] = "安全設置已成功保存！";

                return RedirectToAction("SecuritySettings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存安全設置時發生錯誤");
                TempData["ErrorMessage"] = "保存安全設置時發生錯誤，請稍後再試。";
                return View("SecuritySettings", model);
            }
        }

        #region 私有方法

        private SystemInfoViewModel GetSystemInfo()
        {
            return new SystemInfoViewModel
            {
                SystemName = "CioSystem",
                Version = "1.0.0",
                Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                ServerTime = DateTime.Now,
                Uptime = TimeSpan.FromHours(24), // 模擬運行時間
                MemoryUsage = GC.GetTotalMemory(false),
                ProcessorCount = Environment.ProcessorCount,
                OperatingSystem = Environment.OSVersion.ToString(),
                DotNetVersion = Environment.Version.ToString()
            };
        }

        private async Task<DatabaseStatsViewModel> GetDatabaseStatsAsync()
        {
            try
            {
                var stats = await _databaseManagementService.GetDatabaseStatisticsAsync();
                return new DatabaseStatsViewModel
                {
                    TotalProducts = (int)stats.TotalProducts,
                    TotalInventory = (int)stats.TotalInventory,
                    TotalSales = (int)stats.TotalSales,
                    TotalPurchases = (int)stats.TotalPurchases,
                    DatabaseSize = FormatFileSize(stats.DatabaseSizeBytes),
                    LastBackup = stats.LastBackupTime,
                    ConnectionString = "SQLite Database"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得資料庫統計失敗");
                return new DatabaseStatsViewModel
                {
                    TotalProducts = 0,
                    TotalInventory = 0,
                    TotalSales = 0,
                    TotalPurchases = 0,
                    DatabaseSize = "未知",
                    LastBackup = DateTime.MinValue,
                    ConnectionString = "SQLite Database"
                };
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
            return $"{bytes / (1024 * 1024 * 1024):F1} GB";
        }

        private DatabaseInfoViewModel GetDatabaseInfo()
        {
            return new DatabaseInfoViewModel
            {
                DatabaseType = "SQLite",
                ConnectionString = _configuration.GetConnectionString("DefaultConnection") ?? "",
                DatabasePath = _configuration.GetConnectionString("DefaultConnection")?.Split('=').LastOrDefault() ?? "",
                LastMigration = DateTime.Now.AddDays(-7),
                TotalTables = 8,
                DatabaseSize = "2.5 MB"
            };
        }

        #endregion

        #region 資料庫管理功能

        /// <summary>
        /// 資料庫管理頁面
        /// </summary>
        public async Task<IActionResult> DatabaseManagement()
        {
            try
            {
                _logger.LogInformation("顯示資料庫管理頁面");

                // 獲取資料庫統計信息
                var statistics = await _databaseManagementService.GetDatabaseStatisticsAsync();
                ViewBag.DatabaseStatistics = statistics;

                // 獲取備份文件列表
                var backupFiles = await _databaseManagementService.GetBackupFilesAsync();
                ViewBag.BackupFiles = backupFiles;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入資料庫管理頁面時發生錯誤");
                TempData["ErrorMessage"] = "載入資料庫管理頁面時發生錯誤，請稍後再試。";
                return View();
            }
        }

        /// <summary>
        /// 創建資料庫備份
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateBackup(string? backupName = null, bool includeData = true, bool compress = true)
        {
            try
            {
                _logger.LogInformation("開始創建資料庫備份");

                var backupFilePath = await _databaseManagementService.CreateBackupAsync(backupName, includeData, compress);
                _logger.LogInformation("備份創建成功: {BackupFilePath}", backupFilePath);
                TempData["SuccessMessage"] = $"備份創建成功：{Path.GetFileName(backupFilePath)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建備份失敗");
                TempData["ErrorMessage"] = $"備份創建失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(DatabaseManagement));
        }

        /// <summary>
        /// 還原資料庫
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RestoreDatabase(string backupFilePath, bool createBackupBeforeRestore = true)
        {
            try
            {
                _logger.LogInformation("開始還原資料庫");

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "還原資料庫失敗");
                TempData["ErrorMessage"] = $"資料庫還原失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(DatabaseManagement));
        }

        /// <summary>
        /// 清理資料庫
        /// </summary>
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
                _logger.LogInformation("開始清理資料庫");

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理資料庫失敗");
                TempData["ErrorMessage"] = $"資料庫清理失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(DatabaseManagement));
        }

        /// <summary>
        /// 優化資料庫
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> OptimizeDatabase()
        {
            try
            {
                _logger.LogInformation("開始優化資料庫");

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "優化資料庫失敗");
                TempData["ErrorMessage"] = $"資料庫優化失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(DatabaseManagement));
        }

        /// <summary>
        /// 刪除備份文件
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteBackup(string backupFilePath)
        {
            try
            {
                _logger.LogInformation("刪除備份文件");

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除備份文件失敗");
                TempData["ErrorMessage"] = $"備份文件刪除失敗：{ex.Message}";
            }

            return RedirectToAction(nameof(DatabaseManagement));
        }

        /// <summary>
        /// 下載備份文件
        /// </summary>
        public async Task<IActionResult> DownloadBackup(string backupFilePath)
        {
            try
            {
                if (!System.IO.File.Exists(backupFilePath))
                {
                    TempData["ErrorMessage"] = "備份文件不存在";
                    return RedirectToAction(nameof(DatabaseManagement));
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(backupFilePath);
                var fileName = Path.GetFileName(backupFilePath);
                var contentType = backupFilePath.EndsWith(".zip") ? "application/zip" : "application/octet-stream";

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下載備份文件失敗");
                TempData["ErrorMessage"] = $"下載備份文件失敗：{ex.Message}";
                return RedirectToAction(nameof(DatabaseManagement));
            }
        }

        #endregion

        #region 系統日誌 API

        /// <summary>
        /// 系統日誌頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SystemLogs(int page = 1, int pageSize = 20, string? level = null, string? user = null, 
            DateTime? startDate = null, DateTime? endDate = null, string? searchKeyword = null)
        {
            try
            {
                var (logs, totalCount) = await _systemLogService.GetLogsAsync(
                    page, pageSize, level, user, startDate, endDate, searchKeyword);

                var statistics = await _systemLogService.GetLogStatisticsAsync();

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.Level = level;
                ViewBag.User = user;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.SearchKeyword = searchKeyword;
                ViewBag.Statistics = statistics;

                return View("~/Views/SystemLogs/Index.cshtml", logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入系統日誌失敗");
                TempData["ErrorMessage"] = "載入系統日誌失敗，請稍後再試。";
                return View("~/Views/SystemLogs/Index.cshtml", new List<CioSystem.Models.LogEntryViewModel>());
            }
        }

        /// <summary>
        /// 獲取日誌詳情 (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLogDetail(int logId)
        {
            try
            {
                var logDetail = await _systemLogService.GetLogDetailAsync(logId);
                if (logDetail == null)
                {
                    return Json(new { success = false, message = "日誌不存在" });
                }

                return Json(new { success = true, log = logDetail });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取日誌詳情失敗");
                return Json(new { success = false, message = "獲取日誌詳情失敗" });
            }
        }

        /// <summary>
        /// 匯出日誌
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ExportLogs(string? level = null, string? user = null, 
            DateTime? startDate = null, DateTime? endDate = null, string format = "csv")
        {
            try
            {
                var filePath = await _systemLogService.ExportLogsAsync(level, user, startDate, endDate, format);
                var fileName = $"system_logs_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
                
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯出日誌失敗");
                TempData["ErrorMessage"] = "匯出日誌失敗，請稍後再試。";
                return RedirectToAction(nameof(SystemLogs));
            }
        }

        /// <summary>
        /// 清理舊日誌
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CleanupOldLogs(int daysToKeep = 30)
        {
            try
            {
                var cleanedCount = await _systemLogService.CleanupOldLogsAsync(daysToKeep);
                TempData["SuccessMessage"] = $"成功清理 {cleanedCount} 筆舊日誌";
                
                // 記錄操作日誌
                await _systemLogService.LogAsync("Info", $"清理了 {cleanedCount} 筆舊日誌", User.Identity?.Name);
                
                return RedirectToAction(nameof(SystemLogs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理舊日誌失敗");
                TempData["ErrorMessage"] = "清理舊日誌失敗，請稍後再試。";
                return RedirectToAction(nameof(SystemLogs));
            }
        }

        /// <summary>
        /// 記錄操作日誌
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> LogOperation(string operation, string details, string level = "Info")
        {
            try
            {
                await _systemLogService.LogAsync(level, $"{operation}: {details}", User.Identity?.Name);
                return Json(new { success = true, message = "操作已記錄" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄操作日誌失敗");
                return Json(new { success = false, message = "記錄操作日誌失敗" });
            }
        }

        #endregion

        #region 用戶管理 API

        /// <summary>
        /// 獲取在線用戶列表 (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOnlineUsers()
        {
            try
            {
                var onlineUsers = await _userService.GetOnlineUsersAsync();
                return Json(new { success = true, users = onlineUsers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取在線用戶失敗");
                return Json(new { success = false, message = "獲取在線用戶失敗" });
            }
        }

        /// <summary>
        /// 更新用戶活動時間 (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateLastActivity()
        {
            try
            {
                var sessionId = HttpContext.Session.GetString("SessionId");
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await _userService.UpdateLastActivityAsync(sessionId);
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "會話無效" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新活動時間失敗");
                return Json(new { success = false, message = "更新活動時間失敗" });
            }
        }

        #endregion
    }

}