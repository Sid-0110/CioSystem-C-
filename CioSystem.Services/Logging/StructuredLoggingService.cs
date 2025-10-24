using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Collections.Concurrent;
using CioSystem.Core.Interfaces;

namespace CioSystem.Services.Logging
{
    /// <summary>
    /// 結構化日誌服務實現
    /// 提供結構化日誌記錄和分析功能
    /// </summary>
    public class StructuredLoggingService : IStructuredLoggingService
    {
        private readonly ILogger<StructuredLoggingService> _logger;
        private readonly ConcurrentQueue<StructuredLogEntry> _logEntries;
        private readonly ConcurrentDictionary<string, long> _logCounters;
        private readonly object _lockObject = new object();
        private long _nextId = 1;

        public StructuredLoggingService(ILogger<StructuredLoggingService> logger)
        {
            _logger = logger;
            _logEntries = new ConcurrentQueue<StructuredLogEntry>();
            _logCounters = new ConcurrentDictionary<string, long>();
        }

        public async Task LogAsync(CioSystem.Core.Interfaces.LogLevel logLevel, string message, Dictionary<string, object>? properties = null, Dictionary<string, double>? metrics = null)
        {
            try
            {
                var entry = new StructuredLogEntry
                {
                    Id = GetNextId(),
                    Timestamp = DateTime.UtcNow,
                    Level = logLevel,
                    Message = message,
                    Category = "General",
                    Properties = properties ?? new Dictionary<string, object>(),
                    Metrics = metrics ?? new Dictionary<string, double>()
                };

                _logEntries.Enqueue(entry);

                // 更新計數器
                _logCounters.AddOrUpdate(logLevel.ToString(), 1, (key, value) => value + 1);

                // 記錄到標準日誌
                var logLevelMap = logLevel switch
                {
                    CioSystem.Core.Interfaces.LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
                    CioSystem.Core.Interfaces.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
                    CioSystem.Core.Interfaces.LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
                    CioSystem.Core.Interfaces.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
                    CioSystem.Core.Interfaces.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
                    CioSystem.Core.Interfaces.LogLevel.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
                    _ => Microsoft.Extensions.Logging.LogLevel.Information
                };

                _logger.Log(logLevelMap, "結構化日誌: {Message}", message);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄結構化日誌時發生錯誤: {Message}", message);
            }
        }

        public async Task LogBusinessEventAsync(string businessEvent, string entityType, int entityId, Dictionary<string, object>? properties = null)
        {
            try
            {
                var entry = new StructuredLogEntry
                {
                    Id = GetNextId(),
                    Timestamp = DateTime.UtcNow,
                    Level = CioSystem.Core.Interfaces.LogLevel.Information,
                    Message = $"業務事件: {businessEvent}",
                    Category = "Business",
                    EntityType = entityType,
                    EntityId = entityId,
                    Properties = properties ?? new Dictionary<string, object>()
                };

                _logEntries.Enqueue(entry);

                _logger.LogInformation("業務事件: {BusinessEvent}, {EntityType}={EntityId}",
                    businessEvent, entityType, entityId);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄業務事件時發生錯誤: {BusinessEvent}", businessEvent);
            }
        }

        public async Task LogUserActionAsync(int userId, string action, string resource, Dictionary<string, object>? properties = null)
        {
            try
            {
                var entry = new StructuredLogEntry
                {
                    Id = GetNextId(),
                    Timestamp = DateTime.UtcNow,
                    Level = CioSystem.Core.Interfaces.LogLevel.Information,
                    Message = $"用戶操作: {action}",
                    Category = "UserAction",
                    UserId = userId,
                    Properties = new Dictionary<string, object>
                    {
                        ["Action"] = action,
                        ["Resource"] = resource
                    }
                };

                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        entry.Properties[prop.Key] = prop.Value;
                    }
                }

                _logEntries.Enqueue(entry);

                _logger.LogInformation("用戶操作: UserId={UserId}, Action={Action}, Resource={Resource}",
                    userId, action, resource);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄用戶操作時發生錯誤: UserId={UserId}, Action={Action}", userId, action);
            }
        }

        public async Task LogPerformanceAsync(string operation, TimeSpan duration, Dictionary<string, object>? properties = null)
        {
            try
            {
                var entry = new StructuredLogEntry
                {
                    Id = GetNextId(),
                    Timestamp = DateTime.UtcNow,
                    Level = CioSystem.Core.Interfaces.LogLevel.Information,
                    Message = $"效能記錄: {operation}",
                    Category = "Performance",
                    Properties = new Dictionary<string, object>
                    {
                        ["Operation"] = operation,
                        ["Duration"] = duration.TotalMilliseconds
                    },
                    Metrics = new Dictionary<string, double>
                    {
                        ["DurationMs"] = duration.TotalMilliseconds
                    }
                };

                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        entry.Properties[prop.Key] = prop.Value;
                    }
                }

                _logEntries.Enqueue(entry);

                _logger.LogInformation("效能記錄: {Operation}, 持續時間: {Duration}ms",
                    operation, duration.TotalMilliseconds);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄效能日誌時發生錯誤: {Operation}", operation);
            }
        }

        public async Task LogErrorAsync(Exception exception, Dictionary<string, object>? context = null, string severity = "Error")
        {
            try
            {
                var entry = new StructuredLogEntry
                {
                    Id = GetNextId(),
                    Timestamp = DateTime.UtcNow,
                    Level = CioSystem.Core.Interfaces.LogLevel.Error,
                    Message = $"異常: {exception.Message}",
                    Category = "Error",
                    Exception = exception.GetType().Name,
                    StackTrace = exception.StackTrace,
                    Properties = new Dictionary<string, object>
                    {
                        ["Severity"] = severity,
                        ["ExceptionType"] = exception.GetType().Name
                    }
                };

                if (context != null)
                {
                    foreach (var prop in context)
                    {
                        entry.Properties[prop.Key] = prop.Value;
                    }
                }

                _logEntries.Enqueue(entry);

                _logger.LogError(exception, "記錄錯誤: {ExceptionType} - {Message}",
                    exception.GetType().Name, exception.Message);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄錯誤日誌時發生錯誤");
            }
        }

        public async Task LogSecurityAsync(string securityEvent, int? userId, string? ipAddress, Dictionary<string, object>? properties = null)
        {
            try
            {
                var entry = new StructuredLogEntry
                {
                    Id = GetNextId(),
                    Timestamp = DateTime.UtcNow,
                    Level = CioSystem.Core.Interfaces.LogLevel.Warning,
                    Message = $"安全事件: {securityEvent}",
                    Category = "Security",
                    UserId = userId,
                    Properties = new Dictionary<string, object>
                    {
                        ["SecurityEvent"] = securityEvent,
                        ["IpAddress"] = ipAddress ?? "Unknown"
                    }
                };

                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        entry.Properties[prop.Key] = prop.Value;
                    }
                }

                _logEntries.Enqueue(entry);

                _logger.LogWarning("安全事件: {SecurityEvent}, UserId={UserId}, IpAddress={IpAddress}",
                    securityEvent, userId, ipAddress);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄安全日誌時發生錯誤: {SecurityEvent}", securityEvent);
            }
        }

        public async Task LogAuditAsync(string auditEvent, string entityType, int entityId, Dictionary<string, object>? changes = null)
        {
            try
            {
                var entry = new StructuredLogEntry
                {
                    Id = GetNextId(),
                    Timestamp = DateTime.UtcNow,
                    Level = CioSystem.Core.Interfaces.LogLevel.Information,
                    Message = $"審計事件: {auditEvent}",
                    Category = "Audit",
                    EntityType = entityType,
                    EntityId = entityId,
                    Properties = new Dictionary<string, object>
                    {
                        ["AuditEvent"] = auditEvent,
                        ["Changes"] = changes ?? new Dictionary<string, object>()
                    }
                };

                _logEntries.Enqueue(entry);

                _logger.LogInformation("審計事件: {AuditEvent}, {EntityType}={EntityId}",
                    auditEvent, entityType, entityId);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄審計日誌時發生錯誤: {AuditEvent}", auditEvent);
            }
        }

        public async Task<IEnumerable<StructuredLogEntry>> QueryLogsAsync(LogQuery query)
        {
            try
            {
                var logs = _logEntries.ToArray().AsEnumerable();

                // 應用過濾條件
                if (query.StartTime.HasValue)
                {
                    logs = logs.Where(l => l.Timestamp >= query.StartTime.Value);
                }

                if (query.EndTime.HasValue)
                {
                    logs = logs.Where(l => l.Timestamp <= query.EndTime.Value);
                }

                if (query.MinLevel.HasValue)
                {
                    logs = logs.Where(l => l.Level >= query.MinLevel.Value);
                }

                if (!string.IsNullOrEmpty(query.Category))
                {
                    logs = logs.Where(l => l.Category == query.Category);
                }

                if (query.UserId.HasValue)
                {
                    logs = logs.Where(l => l.UserId == query.UserId.Value);
                }

                if (!string.IsNullOrEmpty(query.EntityType))
                {
                    logs = logs.Where(l => l.EntityType == query.EntityType);
                }

                if (query.EntityId.HasValue)
                {
                    logs = logs.Where(l => l.EntityId == query.EntityId.Value);
                }

                if (!string.IsNullOrEmpty(query.Message))
                {
                    logs = logs.Where(l => l.Message.Contains(query.Message, StringComparison.OrdinalIgnoreCase));
                }

                // 排序
                if (!string.IsNullOrEmpty(query.OrderBy))
                {
                    logs = query.OrderBy.ToLower() switch
                    {
                        "timestamp" => query.OrderDescending ? logs.OrderByDescending(l => l.Timestamp) : logs.OrderBy(l => l.Timestamp),
                        "level" => query.OrderDescending ? logs.OrderByDescending(l => l.Level) : logs.OrderBy(l => l.Level),
                        "message" => query.OrderDescending ? logs.OrderByDescending(l => l.Message) : logs.OrderBy(l => l.Message),
                        _ => query.OrderDescending ? logs.OrderByDescending(l => l.Timestamp) : logs.OrderBy(l => l.Timestamp)
                    };
                }
                else
                {
                    logs = query.OrderDescending ? logs.OrderByDescending(l => l.Timestamp) : logs.OrderBy(l => l.Timestamp);
                }

                // 分頁
                if (query.Skip > 0)
                {
                    logs = logs.Skip(query.Skip);
                }

                if (query.Take > 0)
                {
                    logs = logs.Take(query.Take);
                }

                return await Task.FromResult(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢日誌時發生錯誤");
                return new List<StructuredLogEntry>();
            }
        }

        public async Task<LogStatistics> GetLogStatisticsAsync(TimeRange timeRange)
        {
            try
            {
                var logs = _logEntries
                    .Where(l => l.Timestamp >= timeRange.Start && l.Timestamp <= timeRange.End)
                    .ToList();

                var statistics = new LogStatistics
                {
                    TimeRange = timeRange,
                    TotalLogs = logs.Count,
                    LogsByLevel = logs.GroupBy(l => l.Level).ToDictionary(g => g.Key, g => (long)g.Count()),
                    LogsByCategory = logs.GroupBy(l => l.Category).ToDictionary(g => g.Key, g => (long)g.Count()),
                    LogsByUser = logs.Where(l => l.UserId.HasValue).GroupBy(l => l.UserId!.Value).ToDictionary(g => g.Key, g => (long)g.Count()),
                    TopMessages = logs.GroupBy(l => l.Message).ToDictionary(g => g.Key, g => (long)g.Count()).OrderByDescending(kvp => kvp.Value).Take(10).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    ErrorCount = logs.Count(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Error),
                    WarningCount = logs.Count(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Warning),
                    InfoCount = logs.Count(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Information)
                };

                var hours = (timeRange.End - timeRange.Start).TotalHours;
                statistics.AverageLogsPerHour = hours > 0 ? logs.Count / hours : 0;

                // 統計屬性
                var allProperties = logs.SelectMany(l => l.Properties.Keys).Distinct();
                statistics.TopProperties = allProperties
                    .ToDictionary(prop => prop, prop => (long)logs.Count(l => l.Properties.ContainsKey(prop)))
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(10)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得日誌統計時發生錯誤");
                return new LogStatistics { TimeRange = timeRange };
            }
        }

        public async Task<LogAnalysis> GetLogAnalysisAsync(TimeRange timeRange)
        {
            try
            {
                var logs = _logEntries
                    .Where(l => l.Timestamp >= timeRange.Start && l.Timestamp <= timeRange.End)
                    .ToList();

                var analysis = new LogAnalysis
                {
                    TimeRange = timeRange,
                    Patterns = AnalyzeLogPatterns(logs),
                    Anomalies = DetectLogAnomalies(logs),
                    Trends = AnalyzeLogTrends(logs, timeRange),
                    Correlations = AnalyzeLogCorrelations(logs),
                    Insights = GenerateLogInsights(logs)
                };

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得日誌分析時發生錯誤");
                return new LogAnalysis { TimeRange = timeRange };
            }
        }

        public async Task<byte[]> ExportLogsAsync(LogQuery query, ExportFormat format)
        {
            try
            {
                var logs = await QueryLogsAsync(query);
                var logList = logs.ToList();

                return format switch
                {
                    ExportFormat.Json => await ExportToJsonAsync(logList),
                    ExportFormat.Csv => await ExportToCsvAsync(logList),
                    ExportFormat.Excel => await ExportToExcelAsync(logList),
                    ExportFormat.Pdf => await ExportToPdfAsync(logList),
                    _ => await ExportToJsonAsync(logList)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯出日誌時發生錯誤");
                return Array.Empty<byte>();
            }
        }

        public async Task CleanupOldLogsAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var oldLogs = _logEntries.Where(l => l.Timestamp < cutoffDate).ToList();

                foreach (var log in oldLogs)
                {
                    _logEntries.TryDequeue(out _);
                }

                _logger.LogInformation("清理了 {Count} 條舊日誌記錄", oldLogs.Count);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理舊日誌時發生錯誤");
            }
        }

        // 私有輔助方法
        private long GetNextId()
        {
            lock (_lockObject)
            {
                return _nextId++;
            }
        }

        private List<LogPattern> AnalyzeLogPatterns(List<StructuredLogEntry> logs)
        {
            var patterns = new List<LogPattern>();

            // 分析錯誤模式
            var errorLogs = logs.Where(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Error).ToList();
            if (errorLogs.Any())
            {
                var errorPatterns = errorLogs
                    .GroupBy(l => l.Exception)
                    .Where(g => g.Count() > 1)
                    .Select(g => new LogPattern
                    {
                        Pattern = g.Key ?? "Unknown Error",
                        Frequency = g.Count(),
                        Confidence = Math.Min(1.0, g.Count() / (double)errorLogs.Count),
                        Examples = g.Take(3).Select(l => l.Message).ToList()
                    })
                    .OrderByDescending(p => p.Frequency)
                    .Take(5);

                patterns.AddRange(errorPatterns);
            }

            return patterns;
        }

        private List<LogAnomaly> DetectLogAnomalies(List<StructuredLogEntry> logs)
        {
            var anomalies = new List<LogAnomaly>();

            // 檢測錯誤率異常
            var errorRate = logs.Count(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Error) / (double)logs.Count;
            if (errorRate > 0.1) // 10% 錯誤率閾值
            {
                anomalies.Add(new LogAnomaly
                {
                    Type = "HighErrorRate",
                    Description = $"錯誤率過高: {errorRate:P2}",
                    DetectedAt = DateTime.UtcNow,
                    Severity = errorRate,
                    AffectedLogs = logs.Where(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Error).Select(l => l.Message).ToList()
                });
            }

            // 檢測日誌量異常
            var hourlyLogs = logs.GroupBy(l => l.Timestamp.Hour).ToDictionary(g => g.Key, g => g.Count());
            var avgLogsPerHour = hourlyLogs.Values.Average();
            var highVolumeHours = hourlyLogs.Where(kvp => kvp.Value > avgLogsPerHour * 2).ToList();

            if (highVolumeHours.Any())
            {
                anomalies.Add(new LogAnomaly
                {
                    Type = "HighLogVolume",
                    Description = $"日誌量異常: {highVolumeHours.Count} 小時",
                    DetectedAt = DateTime.UtcNow,
                    Severity = highVolumeHours.Count / 24.0,
                    AffectedLogs = highVolumeHours.Select(kvp => $"小時 {kvp.Key}: {kvp.Value} 條日誌").ToList()
                });
            }

            return anomalies;
        }

        private List<LogTrend> AnalyzeLogTrends(List<StructuredLogEntry> logs, TimeRange timeRange)
        {
            var trends = new List<LogTrend>();

            // 分析錯誤趨勢
            var errorTrend = new LogTrend
            {
                Metric = "Errors",
                DataPoints = logs
                    .Where(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Error)
                    .GroupBy(l => l.Timestamp.Date)
                    .Select(g => new DataPoint { Timestamp = g.Key, Value = g.Count() })
                    .OrderBy(dp => dp.Timestamp)
                    .ToList()
            };

            if (errorTrend.DataPoints.Count > 1)
            {
                var firstValue = errorTrend.DataPoints.First().Value;
                var lastValue = errorTrend.DataPoints.Last().Value;
                errorTrend.ChangeRate = (lastValue - firstValue) / firstValue;
                errorTrend.Direction = errorTrend.ChangeRate > 0.1 ? TrendDirection.Increasing :
                                     errorTrend.ChangeRate < -0.1 ? TrendDirection.Decreasing :
                                     TrendDirection.Stable;
            }

            trends.Add(errorTrend);

            return trends;
        }

        private List<LogCorrelation> AnalyzeLogCorrelations(List<StructuredLogEntry> logs)
        {
            var correlations = new List<LogCorrelation>();

            // 分析錯誤與用戶操作的關聯
            var errorLogs = logs.Where(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Error).ToList();
            var userActionLogs = logs.Where(l => l.Category == "UserAction").ToList();

            if (errorLogs.Any() && userActionLogs.Any())
            {
                var correlation = new LogCorrelation
                {
                    Event1 = "Errors",
                    Event2 = "UserActions",
                    Correlation = CalculateCorrelation(errorLogs.Count, userActionLogs.Count),
                    TimeDifference = TimeSpan.Zero
                };

                correlations.Add(correlation);
            }

            return correlations;
        }

        private LogInsights GenerateLogInsights(List<StructuredLogEntry> logs)
        {
            var insights = new LogInsights();

            // 生成建議
            var errorRate = logs.Count(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Error) / (double)logs.Count;
            if (errorRate > 0.05)
            {
                insights.Recommendations.Add("錯誤率較高，建議檢查系統穩定性");
            }

            // 生成警告
            var warningCount = logs.Count(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Warning);
            if (warningCount > logs.Count * 0.2)
            {
                insights.Warnings.Add("警告日誌過多，可能存在系統問題");
            }

            // 生成告警
            var criticalCount = logs.Count(l => l.Level == CioSystem.Core.Interfaces.LogLevel.Critical);
            if (criticalCount > 0)
            {
                insights.Alerts.Add($"檢測到 {criticalCount} 條嚴重日誌");
            }

            // 生成摘要
            insights.Summary["TotalLogs"] = logs.Count;
            insights.Summary["ErrorRate"] = errorRate;
            insights.Summary["UniqueUsers"] = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().Count();
            insights.Summary["Categories"] = logs.Select(l => l.Category).Distinct().Count();

            return insights;
        }

        private double CalculateCorrelation(int count1, int count2)
        {
            // 簡化的關聯計算
            return Math.Min(1.0, Math.Abs(count1 - count2) / Math.Max(count1, count2));
        }

        private async Task<byte[]> ExportToJsonAsync(List<StructuredLogEntry> logs)
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        private async Task<byte[]> ExportToCsvAsync(List<StructuredLogEntry> logs)
        {
            var csv = "Id,Timestamp,Level,Message,Category,UserId,EntityType,EntityId\n";
            foreach (var log in logs)
            {
                csv += $"{log.Id},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.Level},{log.Message},{log.Category},{log.UserId},{log.EntityType},{log.EntityId}\n";
            }
            return System.Text.Encoding.UTF8.GetBytes(csv);
        }

        private async Task<byte[]> ExportToExcelAsync(List<StructuredLogEntry> logs)
        {
            // 簡化的 Excel 匯出 - 實際應用中應使用 EPPlus 或 ClosedXML
            return await ExportToCsvAsync(logs);
        }

        private async Task<byte[]> ExportToPdfAsync(List<StructuredLogEntry> logs)
        {
            // 簡化的 PDF 匯出 - 實際應用中應使用 iTextSharp 或其他 PDF 庫
            var content = $"日誌報告\n生成時間: {DateTime.UtcNow}\n\n";
            foreach (var log in logs.Take(100)) // 限制前100條
            {
                content += $"{log.Timestamp:yyyy-MM-dd HH:mm:ss} [{log.Level}] {log.Message}\n";
            }
            return System.Text.Encoding.UTF8.GetBytes(content);
        }
    }
}