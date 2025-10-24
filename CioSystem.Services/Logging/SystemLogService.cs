using CioSystem.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CioSystem.Services.Logging
{
    /// <summary>
    /// 系統日誌服務實現
    /// </summary>
    public class SystemLogService : ISystemLogService
    {
        private readonly ILogger<SystemLogService> _logger;
        private readonly string _logDirectory;
        private readonly string _logFilePath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public SystemLogService(ILogger<SystemLogService> logger)
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

        public async Task<(IEnumerable<LogEntryViewModel> logs, int totalCount)> GetLogsAsync(
            int page = 1,
            int pageSize = 20,
            string? level = null,
            string? user = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? searchKeyword = null)
        {
            try
            {
                await _semaphore.WaitAsync();

                var allLogs = await LoadLogsAsync();

                // 應用篩選條件
                var filteredLogs = allLogs.AsEnumerable();

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

                return (pagedLogs, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取系統日誌時發生錯誤");
                return (new List<LogEntryViewModel>(), 0);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<LogStatisticsViewModel> GetLogStatisticsAsync()
        {
            try
            {
                var allLogs = await LoadLogsAsync();

                var stats = new LogStatisticsViewModel
                {
                    InfoCount = allLogs.Count(l => l.Level == "Info"),
                    WarningCount = allLogs.Count(l => l.Level == "Warning"),
                    ErrorCount = allLogs.Count(l => l.Level == "Error"),
                    DebugCount = allLogs.Count(l => l.Level == "Debug"),
                    TotalCount = allLogs.Count,
                    LastLogTime = allLogs.OrderByDescending(l => l.Timestamp).FirstOrDefault()?.Timestamp
                };

                // 找出最活躍的用戶
                if (allLogs.Any())
                {
                    stats.MostActiveUser = allLogs
                        .GroupBy(l => l.User)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;
                }

                // 找出最常見的日誌等級
                if (allLogs.Any())
                {
                    stats.MostCommonLevel = allLogs
                        .GroupBy(l => l.Level)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取日誌統計時發生錯誤");
                return new LogStatisticsViewModel();
            }
        }

        public async Task<LogEntryViewModel?> GetLogDetailAsync(int logId)
        {
            try
            {
                var allLogs = await LoadLogsAsync();
                return allLogs.FirstOrDefault(l => l.Id == logId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取日誌詳情時發生錯誤");
                return null;
            }
        }

        public async Task<string> ExportLogsAsync(
            string? level = null,
            string? user = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string format = "csv")
        {
            try
            {
                var (logs, _) = await GetLogsAsync(
                    page: 1,
                    pageSize: int.MaxValue,
                    level,
                    user,
                    startDate,
                    endDate);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"system_logs_{timestamp}.{format}";
                var filePath = Path.Combine(_logDirectory, fileName);

                if (format.ToLower() == "csv")
                {
                    await ExportToCsvAsync(logs, filePath);
                }
                else if (format.ToLower() == "json")
                {
                    await ExportToJsonAsync(logs, filePath);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯出日誌時發生錯誤");
                throw;
            }
        }

        public async Task<int> CleanupOldLogsAsync(int daysToKeep = 30)
        {
            try
            {
                await _semaphore.WaitAsync();

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var allLogs = await LoadLogsAsync();

                var logsToKeep = allLogs.Where(l => l.Timestamp >= cutoffDate).ToList();
                var removedCount = allLogs.Count - logsToKeep.Count;

                if (removedCount > 0)
                {
                    await SaveLogsAsync(logsToKeep);
                    _logger.LogInformation("清理了 {Count} 條舊日誌記錄", removedCount);
                }

                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理舊日誌時發生錯誤");
                return 0;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task LogAsync(string level, string message, string? user = null, Exception? exception = null)
        {
            try
            {
                await _semaphore.WaitAsync();

                var logEntry = new LogEntryViewModel
                {
                    Id = await GetNextLogIdAsync(),
                    Level = level,
                    Message = exception != null ? $"{message} - {exception.Message}" : message,
                    Timestamp = DateTime.Now,
                    User = user ?? "System"
                };

                var allLogs = await LoadLogsAsync();
                allLogs.Add(logEntry);

                // 保持最近的1000條日誌記錄
                if (allLogs.Count > 1000)
                {
                    allLogs = allLogs.OrderByDescending(l => l.Timestamp).Take(1000).ToList();
                }

                await SaveLogsAsync(allLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄系統日誌時發生錯誤");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<List<LogEntryViewModel>> LoadLogsAsync()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    return new List<LogEntryViewModel>();
                }

                var json = await File.ReadAllTextAsync(_logFilePath);
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
                await File.WriteAllTextAsync(_logFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存日誌檔案時發生錯誤");
            }
        }

        private async Task<int> GetNextLogIdAsync()
        {
            var logs = await LoadLogsAsync();
            return logs.Count > 0 ? logs.Max(l => l.Id) + 1 : 1;
        }

        private async Task ExportToCsvAsync(IEnumerable<LogEntryViewModel> logs, string filePath)
        {
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

            // 寫入CSV標題行
            await writer.WriteLineAsync("ID,時間,等級,用戶,訊息");

            // 寫入日誌數據
            foreach (var log in logs)
            {
                var message = log.Message.Replace("\"", "\"\"");
                await writer.WriteLineAsync($"{log.Id},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.Level},{log.User},\"{message}\"");
            }
        }

        private async Task ExportToJsonAsync(IEnumerable<LogEntryViewModel> logs, string filePath)
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            await File.WriteAllTextAsync(filePath, json);
        }
    }
}