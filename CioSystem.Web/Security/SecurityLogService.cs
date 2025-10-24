using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CioSystem.Web.Security
{
    /// <summary>
    /// 安全日誌服務實現
    /// 提供安全事件記錄和監控功能
    /// </summary>
    public class SecurityLogService : ISecurityLogService
    {
        private readonly ILogger<SecurityLogService> _logger;
        private readonly ConcurrentQueue<SecurityLogEvent> _securityEvents;
        private readonly object _lockObject = new object();

        public SecurityLogService(ILogger<SecurityLogService> logger)
        {
            _logger = logger;
            _securityEvents = new ConcurrentQueue<SecurityLogEvent>();
        }

        public async Task LogSecurityEventAsync(SecurityLogEvent @event)
        {
            try
            {
                @event.Timestamp = DateTime.UtcNow;
                @event.Id = GenerateEventId();

                // 添加到記憶體佇列
                _securityEvents.Enqueue(@event);

                // 記錄到日誌系統
                var logLevel = GetLogLevel(@event.Severity);
                _logger.Log(logLevel, "安全事件: {EventType}, 用戶: {UserId}, IP: {IpAddress}, 詳情: {Details}",
                    @event.EventType, @event.UserId, @event.IpAddress, @event.Details);

                // 在實際應用中，這裡應該將事件保存到資料庫或外部日誌系統
                await SaveSecurityEventToDatabase(@event);

                // 檢查異常活動
                await CheckForAnomalies(@event);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄安全事件時發生錯誤: {EventType}", @event.EventType);
            }
        }

        public async Task LogLoginEventAsync(int? userId, string ipAddress, string userAgent, bool isSuccess, string? failureReason = null)
        {
            var @event = new SecurityLogEvent
            {
                EventType = isSuccess ? SecurityEventType.LoginSuccess : SecurityEventType.LoginFailed,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Details = isSuccess ? "登入成功" : $"登入失敗: {failureReason}",
                Severity = isSuccess ? "Info" : "Warning"
            };

            await LogSecurityEventAsync(@event);
        }

        public async Task LogPermissionEventAsync(int userId, string resource, string action, bool isAllowed)
        {
            var @event = new SecurityLogEvent
            {
                EventType = isAllowed ? SecurityEventType.DataAccess : SecurityEventType.PermissionDenied,
                UserId = userId,
                Resource = resource,
                Action = action,
                Details = isAllowed ? $"允許存取 {resource}/{action}" : $"拒絕存取 {resource}/{action}",
                Severity = isAllowed ? "Info" : "Warning"
            };

            await LogSecurityEventAsync(@event);
        }

        public async Task LogDataAccessEventAsync(int userId, string dataType, string operation, int? recordId = null)
        {
            var @event = new SecurityLogEvent
            {
                EventType = SecurityEventType.DataAccess,
                UserId = userId,
                Resource = dataType,
                Action = operation,
                Details = $"資料存取: {dataType}/{operation}" + (recordId.HasValue ? $" (ID: {recordId})" : ""),
                Severity = "Info"
            };

            await LogSecurityEventAsync(@event);
        }

        public async Task LogSecurityThreatAsync(ThreatType threatType, string ipAddress, string userAgent, string details)
        {
            var @event = new SecurityLogEvent
            {
                EventType = SecurityEventType.SecurityViolation,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Details = $"安全威脅: {threatType} - {details}",
                Severity = "Critical"
            };

            await LogSecurityEventAsync(@event);
        }

        public async Task LogSystemEventAsync(SystemEventType eventType, int? userId, string details)
        {
            var @event = new SecurityLogEvent
            {
                EventType = SecurityEventType.SystemAccess,
                UserId = userId,
                Details = $"系統事件: {eventType} - {details}",
                Severity = "Info"
            };

            await LogSecurityEventAsync(@event);
        }

        public async Task<IEnumerable<SecurityLogEvent>> GetSecurityLogsAsync(DateTime? startDate = null, DateTime? endDate = null, SecurityEventType? eventType = null, int? userId = null)
        {
            try
            {
                var events = _securityEvents.ToArray();

                // 應用過濾條件
                var filteredEvents = events.AsEnumerable();

                if (startDate.HasValue)
                {
                    filteredEvents = filteredEvents.Where(e => e.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    filteredEvents = filteredEvents.Where(e => e.Timestamp <= endDate.Value);
                }

                if (eventType.HasValue)
                {
                    filteredEvents = filteredEvents.Where(e => e.EventType == eventType.Value);
                }

                if (userId.HasValue)
                {
                    filteredEvents = filteredEvents.Where(e => e.UserId == userId.Value);
                }

                return await Task.FromResult(filteredEvents.OrderByDescending(e => e.Timestamp));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得安全日誌時發生錯誤");
                return new List<SecurityLogEvent>();
            }
        }

        public async Task<SecurityStatistics> GetSecurityStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var events = await GetSecurityLogsAsync(startDate, endDate);
                var eventList = events.ToList();

                var statistics = new SecurityStatistics
                {
                    TotalEvents = eventList.Count,
                    LoginAttempts = eventList.Count(e => e.EventType == SecurityEventType.LoginSuccess || e.EventType == SecurityEventType.LoginFailed),
                    FailedLogins = eventList.Count(e => e.EventType == SecurityEventType.LoginFailed),
                    PermissionDenied = eventList.Count(e => e.EventType == SecurityEventType.PermissionDenied),
                    SecurityThreats = eventList.Count(e => e.EventType == SecurityEventType.SecurityViolation),
                    DataAccess = eventList.Count(e => e.EventType == SecurityEventType.DataAccess),
                    SystemEvents = eventList.Count(e => e.EventType == SecurityEventType.SystemAccess),
                    GeneratedAt = DateTime.UtcNow
                };

                // 按事件類型統計
                statistics.EventsByType = eventList
                    .GroupBy(e => e.EventType.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                // 按嚴重程度統計
                statistics.EventsBySeverity = eventList
                    .GroupBy(e => e.Severity ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                // 按 IP 地址統計
                statistics.TopIpAddresses = eventList
                    .Where(e => !string.IsNullOrEmpty(e.IpAddress))
                    .GroupBy(e => e.IpAddress!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 按用戶代理統計
                statistics.TopUserAgents = eventList
                    .Where(e => !string.IsNullOrEmpty(e.UserAgent))
                    .GroupBy(e => e.UserAgent!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得安全統計時發生錯誤");
                return new SecurityStatistics();
            }
        }

        public async Task<AnomalyReport> CheckAnomalousActivityAsync(int? userId = null, string? ipAddress = null)
        {
            try
            {
                var recentEvents = await GetSecurityLogsAsync(DateTime.UtcNow.AddHours(-24));
                var eventList = recentEvents.ToList();

                var anomalies = new List<string>();
                var recommendations = new List<string>();

                // 檢查失敗登入次數
                var failedLogins = eventList.Count(e => e.EventType == SecurityEventType.LoginFailed);
                if (failedLogins > 5)
                {
                    anomalies.Add($"過去24小時內有 {failedLogins} 次失敗登入");
                    recommendations.Add("考慮暫時鎖定帳戶或增加額外驗證");
                }

                // 檢查異常 IP 地址
                var uniqueIps = eventList.Select(e => e.IpAddress).Distinct().Count();
                if (uniqueIps > 10)
                {
                    anomalies.Add($"過去24小時內來自 {uniqueIps} 個不同 IP 地址的活動");
                    recommendations.Add("檢查是否為分散式攻擊");
                }

                // 檢查安全威脅
                var securityThreats = eventList.Count(e => e.EventType == SecurityEventType.SecurityViolation);
                if (securityThreats > 0)
                {
                    anomalies.Add($"過去24小時內檢測到 {securityThreats} 個安全威脅");
                    recommendations.Add("立即檢查系統安全狀態");
                }

                // 檢查權限拒絕
                var permissionDenied = eventList.Count(e => e.EventType == SecurityEventType.PermissionDenied);
                if (permissionDenied > 20)
                {
                    anomalies.Add($"過去24小時內有 {permissionDenied} 次權限拒絕");
                    recommendations.Add("檢查用戶權限配置");
                }

                return new AnomalyReport
                {
                    HasAnomalies = anomalies.Count > 0,
                    Anomalies = anomalies.ToArray(),
                    FailedLoginAttempts = failedLogins,
                    UnusualIpAddresses = uniqueIps,
                    SuspiciousActivities = securityThreats,
                    LastActivity = eventList.Any() ? eventList.Max(e => e.Timestamp) : DateTime.MinValue,
                    Recommendations = recommendations.ToArray()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查異常活動時發生錯誤");
                return new AnomalyReport();
            }
        }

        private int GenerateEventId()
        {
            lock (_lockObject)
            {
                return _securityEvents.Count + 1;
            }
        }

        private LogLevel GetLogLevel(string? severity)
        {
            return severity?.ToLower() switch
            {
                "critical" => LogLevel.Critical,
                "error" => LogLevel.Error,
                "warning" => LogLevel.Warning,
                "info" => LogLevel.Information,
                "debug" => LogLevel.Debug,
                _ => LogLevel.Information
            };
        }

        private async Task SaveSecurityEventToDatabase(SecurityLogEvent @event)
        {
            // 在實際應用中，這裡應該將事件保存到資料庫
            // 例如：使用 Entity Framework 或其他 ORM
            await Task.CompletedTask;
        }

        private async Task CheckForAnomalies(SecurityLogEvent @event)
        {
            // 檢查即時異常
            if (@event.EventType == SecurityEventType.SecurityViolation)
            {
                _logger.LogWarning("檢測到安全威脅: {Details}", @event.Details);
            }

            if (@event.EventType == SecurityEventType.LoginFailed)
            {
                // 檢查是否為暴力攻擊
                var recentFailedLogins = _securityEvents
                    .Where(e => e.EventType == SecurityEventType.LoginFailed &&
                               e.Timestamp > DateTime.UtcNow.AddMinutes(-5))
                    .Count();

                if (recentFailedLogins > 3)
                {
                    _logger.LogWarning("檢測到可能的暴力攻擊: IP {IpAddress}", @event.IpAddress);
                }
            }

            await Task.CompletedTask;
        }
    }
}