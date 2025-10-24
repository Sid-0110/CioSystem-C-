using Microsoft.AspNetCore.Mvc;
using CioSystem.Models;
using System.Text.Json;

namespace CioSystem.Web.Controllers
{
    public class SystemLogsController : Controller
    {
        private readonly ILogger<SystemLogsController> _logger;
        private readonly string _logDirectory;
        private readonly string _logFilePath;

        public SystemLogsController(ILogger<SystemLogsController> logger)
        {
            _logger = logger;
            _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            _logFilePath = Path.Combine(_logDirectory, "system_logs.json");

            // 確保日誌目錄存在
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        /// <summary>
        /// 系統日誌頁面
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? level = null, string? user = null, DateTime? startDate = null, DateTime? endDate = null, string? searchKeyword = null)
        {
            try
            {
                _logger.LogInformation("顯示系統日誌頁面");

                // 初始化示例日誌數據（如果沒有數據的話）
                await InitializeSampleLogsAsync();

                var logs = await LoadLogsAsync();

                // 應用篩選條件
                var filteredLogs = logs.AsEnumerable();

                if (!string.IsNullOrEmpty(level))
                {
                    filteredLogs = filteredLogs.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(user))
                {
                    filteredLogs = filteredLogs.Where(l => l.User.Contains(user, StringComparison.OrdinalIgnoreCase));
                }

                if (startDate.HasValue)
                {
                    filteredLogs = filteredLogs.Where(l => l.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    filteredLogs = filteredLogs.Where(l => l.Timestamp <= endDate.Value.AddDays(1));
                }

                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    filteredLogs = filteredLogs.Where(l =>
                        l.Message.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        l.User.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase));
                }

                // 按時間倒序排列
                filteredLogs = filteredLogs.OrderByDescending(l => l.Timestamp);

                var totalCount = filteredLogs.Count();

                // 分頁
                var pagedLogs = filteredLogs
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 計算統計數據
                var statistics = new LogStatisticsViewModel
                {
                    InfoCount = logs.Count(l => l.Level == "Info"),
                    WarningCount = logs.Count(l => l.Level == "Warning"),
                    ErrorCount = logs.Count(l => l.Level == "Error"),
                    DebugCount = logs.Count(l => l.Level == "Debug"),
                    TotalCount = logs.Count,
                    LastLogTime = logs.OrderByDescending(l => l.Timestamp).FirstOrDefault()?.Timestamp
                };

                // 找出最活躍的用戶
                if (logs.Any())
                {
                    statistics.MostActiveUser = logs
                        .GroupBy(l => l.User)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;
                }

                // 找出最常見的日誌等級
                if (logs.Any())
                {
                    statistics.MostCommonLevel = logs
                        .GroupBy(l => l.Level)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;
                }

                ViewBag.TotalCount = totalCount;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.Statistics = statistics;
                ViewBag.Filter = new LogFilterViewModel
                {
                    Level = level,
                    User = user,
                    StartDate = startDate,
                    EndDate = endDate,
                    SearchKeyword = searchKeyword,
                    Page = page,
                    PageSize = pageSize
                };

                return View(pagedLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入系統日誌時發生錯誤");
                TempData["ErrorMessage"] = "載入系統日誌時發生錯誤，請稍後再試。";
                return View(new List<LogEntryViewModel>());
            }
        }

        /// <summary>
        /// 獲取日誌統計信息
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var logs = await LoadLogsAsync();
                var statistics = new LogStatisticsViewModel
                {
                    InfoCount = logs.Count(l => l.Level == "Info"),
                    WarningCount = logs.Count(l => l.Level == "Warning"),
                    ErrorCount = logs.Count(l => l.Level == "Error"),
                    DebugCount = logs.Count(l => l.Level == "Debug"),
                    TotalCount = logs.Count,
                    LastLogTime = logs.OrderByDescending(l => l.Timestamp).FirstOrDefault()?.Timestamp
                };

                if (logs.Any())
                {
                    statistics.MostActiveUser = logs
                        .GroupBy(l => l.User)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;

                    statistics.MostCommonLevel = logs
                        .GroupBy(l => l.Level)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;
                }

                return Json(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取日誌統計時發生錯誤");
                return Json(new { error = "獲取日誌統計時發生錯誤" });
            }
        }

        /// <summary>
        /// 匯出日誌
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Export([FromBody] LogFilterViewModel filter)
        {
            try
            {
                var logs = await LoadLogsAsync();

                // 應用篩選條件
                var filteredLogs = logs.AsEnumerable();

                if (!string.IsNullOrEmpty(filter.Level))
                {
                    filteredLogs = filteredLogs.Where(l => l.Level.Equals(filter.Level, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(filter.User))
                {
                    filteredLogs = filteredLogs.Where(l => l.User.Contains(filter.User, StringComparison.OrdinalIgnoreCase));
                }

                if (filter.StartDate.HasValue)
                {
                    filteredLogs = filteredLogs.Where(l => l.Timestamp >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    filteredLogs = filteredLogs.Where(l => l.Timestamp <= filter.EndDate.Value.AddDays(1));
                }

                if (!string.IsNullOrEmpty(filter.SearchKeyword))
                {
                    filteredLogs = filteredLogs.Where(l =>
                        l.Message.Contains(filter.SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        l.User.Contains(filter.SearchKeyword, StringComparison.OrdinalIgnoreCase));
                }

                filteredLogs = filteredLogs.OrderByDescending(l => l.Timestamp);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"system_logs_{timestamp}.csv";

                using var writer = new StringWriter();
                await writer.WriteLineAsync("ID,時間,等級,用戶,訊息");

                foreach (var log in filteredLogs)
                {
                    var message = log.Message.Replace("\"", "\"\"");
                    await writer.WriteLineAsync($"{log.Id},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.Level},{log.User},\"{message}\"");
                }

                var csvContent = writer.ToString();
                var fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);

                return base.File(fileBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯出日誌時發生錯誤");
                return Json(new { error = "匯出日誌時發生錯誤" });
            }
        }

        /// <summary>
        /// 清理舊日誌
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Cleanup([FromBody] CleanupLogsRequest request)
        {
            try
            {
                var logs = await LoadLogsAsync();
                var cutoffDate = DateTime.Now.AddDays(-request.DaysToKeep);

                var logsToKeep = logs.Where(l => l.Timestamp >= cutoffDate).ToList();
                var removedCount = logs.Count - logsToKeep.Count;

                if (removedCount > 0)
                {
                    await SaveLogsAsync(logsToKeep);
                    _logger.LogInformation("清理了 {Count} 條舊日誌記錄", removedCount);
                }

                return Json(new { success = true, removedCount = removedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理日誌時發生錯誤");
                return Json(new { error = "清理日誌時發生錯誤" });
            }
        }

        private async Task<List<LogEntryViewModel>> LoadLogsAsync()
        {
            try
            {
                if (!System.IO.File.Exists(_logFilePath))
                {
                    return new List<LogEntryViewModel>();
                }

                var json = await System.IO.File.ReadAllTextAsync(_logFilePath);
                var logs = JsonSerializer.Deserialize<List<LogEntryViewModel>>(json) ?? new List<LogEntryViewModel>();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入日誌檔案時發生錯誤");
                return new List<LogEntryViewModel>();
            }
        }

        private async Task SaveLogsAsync(List<LogEntryViewModel> logs)
        {
            try
            {
                var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                await System.IO.File.WriteAllTextAsync(_logFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存日誌檔案時發生錯誤");
            }
        }

        private async Task InitializeSampleLogsAsync()
        {
            try
            {
                var existingLogs = await LoadLogsAsync();
                if (existingLogs.Any())
                {
                    return; // 已經有數據，不需要初始化
                }

                // 創建示例日誌數據
                var sampleLogs = new List<LogEntryViewModel>();
                var sampleData = new[]
                {
                    new { Level = "Info", Message = "系統啟動成功", User = "System" },
                    new { Level = "Info", Message = "用戶 admin 登入成功", User = "admin" },
                    new { Level = "Info", Message = "產品資料同步完成", User = "System" },
                    new { Level = "Warning", Message = "資料庫連線延遲 500ms", User = "System" },
                    new { Level = "Info", Message = "庫存資料更新完成", User = "admin" },
                    new { Level = "Error", Message = "用戶 manager 登入失敗：密碼錯誤", User = "manager" },
                    new { Level = "Info", Message = "銷售記錄匯出完成", User = "admin" },
                    new { Level = "Warning", Message = "記憶體使用率達到 80%", User = "System" },
                    new { Level = "Info", Message = "備份任務執行完成", User = "System" },
                    new { Level = "Error", Message = "資料庫連接超時", User = "System" },
                    new { Level = "Info", Message = "用戶 staff 登入成功", User = "staff" },
                    new { Level = "Info", Message = "庫存警告：產品 A001 庫存不足", User = "System" },
                    new { Level = "Warning", Message = "磁碟空間使用率達到 85%", User = "System" },
                    new { Level = "Info", Message = "報表生成完成", User = "admin" },
                    new { Level = "Error", Message = "檔案上傳失敗：檔案大小超過限制", User = "staff" },
                    new { Level = "Debug", Message = "系統除錯信息：記憶體分配正常", User = "System" },
                    new { Level = "Debug", Message = "API 請求處理完成", User = "System" },
                    new { Level = "Info", Message = "系統設定更新完成", User = "admin" },
                    new { Level = "Warning", Message = "API 請求回應時間過長", User = "System" },
                    new { Level = "Info", Message = "用戶登出成功", User = "staff" }
                };

                for (int i = 0; i < sampleData.Length; i++)
                {
                    var data = sampleData[i];
                    sampleLogs.Add(new LogEntryViewModel
                    {
                        Id = i + 1,
                        Level = data.Level,
                        Message = data.Message,
                        Timestamp = DateTime.Now.AddMinutes(-(i * 5)), // 每5分鐘一條
                        User = data.User
                    });
                }

                await SaveLogsAsync(sampleLogs);
                _logger.LogInformation("初始化了 {Count} 條示例日誌數據", sampleLogs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化示例日誌數據時發生錯誤");
            }
        }
    }

    public class CleanupLogsRequest
    {
        public int DaysToKeep { get; set; } = 30;
    }
}