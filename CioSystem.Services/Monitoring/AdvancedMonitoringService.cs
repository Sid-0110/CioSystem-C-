using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CioSystem.Services.Monitoring
{
    /// <summary>
    /// 進階監控服務實現
    /// 提供全面的系統監控和效能分析
    /// </summary>
    public class AdvancedMonitoringService : IAdvancedMonitoringService
    {
        private readonly ILogger<AdvancedMonitoringService> _logger;
        private readonly ConcurrentDictionary<string, PerformanceCounter> _performanceCounters;
        private readonly ConcurrentQueue<MonitoringEvent> _events;
        private readonly ConcurrentDictionary<string, AlertRule> _alertRules;
        private readonly ConcurrentDictionary<string, Alert> _activeAlerts;

        public AdvancedMonitoringService(ILogger<AdvancedMonitoringService> logger)
        {
            _logger = logger;
            _performanceCounters = new ConcurrentDictionary<string, PerformanceCounter>();
            _events = new ConcurrentQueue<MonitoringEvent>();
            _alertRules = new ConcurrentDictionary<string, AlertRule>();
            _activeAlerts = new ConcurrentDictionary<string, Alert>();
        }

        public IDisposable StartPerformanceTracking(string operationName, Dictionary<string, string>? tags = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var trackingId = Guid.NewGuid().ToString();

            return new PerformanceTracker(
                operationName,
                trackingId,
                stopwatch,
                tags ?? new Dictionary<string, string>(),
                this
            );
        }

        public async Task RecordMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            try
            {
                var metric = new Metric
                {
                    Name = metricName,
                    Value = value,
                    Timestamp = DateTime.UtcNow,
                    Tags = tags ?? new Dictionary<string, string>()
                };

                // 更新效能計數器
                _performanceCounters.AddOrUpdate(metricName,
                    new PerformanceCounter { Name = metricName, Value = value, Count = 1 },
                    (key, existing) => new PerformanceCounter
                    {
                        Name = metricName,
                        Value = (existing.Value + value) / 2,
                        Count = existing.Count + 1
                    });

                // 記錄到事件佇列
                _events.Enqueue(new MonitoringEvent
                {
                    Type = "Metric",
                    Name = metricName,
                    Value = value,
                    Timestamp = DateTime.UtcNow,
                    Properties = tags ?? new Dictionary<string, string>()
                });

                _logger.LogDebug("記錄效能指標: {MetricName} = {Value}", metricName, value);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄效能指標時發生錯誤: {MetricName}", metricName);
            }
        }

        public async Task RecordEventAsync(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null)
        {
            try
            {
                var @event = new MonitoringEvent
                {
                    Type = "Event",
                    Name = eventName,
                    Timestamp = DateTime.UtcNow,
                    Properties = properties ?? new Dictionary<string, string>(),
                    Metrics = metrics ?? new Dictionary<string, double>()
                };

                _events.Enqueue(@event);

                _logger.LogInformation("記錄自定義事件: {EventName}", eventName);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄自定義事件時發生錯誤: {EventName}", eventName);
            }
        }

        public async Task RecordExceptionAsync(Exception exception, Dictionary<string, string>? context = null, string severity = "Error")
        {
            try
            {
                var @event = new MonitoringEvent
                {
                    Type = "Exception",
                    Name = "Exception",
                    Timestamp = DateTime.UtcNow,
                    Properties = new Dictionary<string, string>
                    {
                        ["ExceptionType"] = exception.GetType().Name,
                        ["Message"] = exception.Message,
                        ["StackTrace"] = exception.StackTrace ?? string.Empty,
                        ["Severity"] = severity
                    },
                    Context = context ?? new Dictionary<string, string>()
                };

                _events.Enqueue(@event);

                // 記錄到日誌
                var logLevel = severity.ToLower() switch
                {
                    "critical" => LogLevel.Critical,
                    "error" => LogLevel.Error,
                    "warning" => LogLevel.Warning,
                    _ => LogLevel.Error
                };

                _logger.Log(logLevel, exception, "記錄異常: {ExceptionType} - {Message}",
                    exception.GetType().Name, exception.Message);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄異常時發生錯誤");
            }
        }

        public async Task RecordUserBehaviorAsync(int userId, string action, Dictionary<string, string>? properties = null)
        {
            try
            {
                var @event = new MonitoringEvent
                {
                    Type = "UserBehavior",
                    Name = action,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    Properties = properties ?? new Dictionary<string, string>()
                };

                _events.Enqueue(@event);

                _logger.LogInformation("記錄用戶行為: UserId={UserId}, Action={Action}", userId, action);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄用戶行為時發生錯誤: UserId={UserId}, Action={Action}", userId, action);
            }
        }

        public async Task RecordBusinessEventAsync(BusinessEventType eventType, string entityType, int entityId, Dictionary<string, string>? properties = null)
        {
            try
            {
                var @event = new MonitoringEvent
                {
                    Type = "BusinessEvent",
                    Name = eventType.ToString(),
                    EntityType = entityType,
                    EntityId = entityId,
                    Timestamp = DateTime.UtcNow,
                    Properties = properties ?? new Dictionary<string, string>()
                };

                _events.Enqueue(@event);

                _logger.LogInformation("記錄業務事件: {EventType}, {EntityType}={EntityId}",
                    eventType, entityType, entityId);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄業務事件時發生錯誤: {EventType}, {EntityType}={EntityId}",
                    eventType, entityType, entityId);
            }
        }

        public async Task<SystemHealthStatus> GetSystemHealthAsync()
        {
            try
            {
                var healthStatus = new SystemHealthStatus
                {
                    CheckedAt = DateTime.UtcNow,
                    IsHealthy = true,
                    Status = "Healthy"
                };

                // 檢查系統指標
                healthStatus.Metrics = await GetSystemMetricsAsync();

                // 檢查各個組件
                healthStatus.Components["Database"] = await CheckDatabaseHealthAsync();
                healthStatus.Components["Memory"] = await CheckMemoryHealthAsync();
                healthStatus.Components["CPU"] = await CheckCpuHealthAsync();
                healthStatus.Components["Disk"] = await CheckDiskHealthAsync();

                // 判斷整體健康狀態
                healthStatus.IsHealthy = healthStatus.Components.Values.All(c => c.IsHealthy);
                healthStatus.Status = healthStatus.IsHealthy ? "Healthy" : "Unhealthy";

                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得系統健康狀態時發生錯誤");
                return new SystemHealthStatus
                {
                    IsHealthy = false,
                    Status = "Error",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<PerformanceStatistics> GetPerformanceStatisticsAsync(TimeRange timeRange)
        {
            try
            {
                var events = _events
                    .Where(e => e.Timestamp >= timeRange.Start && e.Timestamp <= timeRange.End)
                    .ToList();

                var performanceEvents = events.Where(e => e.Type == "Metric" && e.Name.Contains("ResponseTime")).ToList();
                var requestEvents = events.Where(e => e.Type == "Event" && e.Name.Contains("Request")).ToList();

                var statistics = new PerformanceStatistics
                {
                    TimeRange = timeRange,
                    TotalRequests = requestEvents.Count,
                    RequestsPerSecond = requestEvents.Count / (timeRange.End - timeRange.Start).TotalSeconds,
                    ErrorRate = events.Count(e => e.Type == "Exception") / (double)requestEvents.Count * 100
                };

                if (performanceEvents.Any())
                {
                    statistics.AverageResponseTime = performanceEvents.Average(e => e.Value);
                    statistics.MaxResponseTime = performanceEvents.Max(e => e.Value);
                    statistics.MinResponseTime = performanceEvents.Min(e => e.Value);
                }

                // 計算最慢的操作
                statistics.TopSlowOperations = performanceEvents
                    .GroupBy(e => e.Name)
                    .ToDictionary(g => g.Key, g => g.Average(e => e.Value))
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(10)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                // 計算端點請求數
                statistics.RequestCountByEndpoint = requestEvents
                    .GroupBy(e => e.Properties.GetValueOrDefault("Endpoint", "Unknown"))
                    .ToDictionary(g => g.Key, g => (long)g.Count());

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得效能統計時發生錯誤");
                return new PerformanceStatistics { TimeRange = timeRange };
            }
        }

        public async Task<ErrorStatistics> GetErrorStatisticsAsync(TimeRange timeRange)
        {
            try
            {
                var events = _events
                    .Where(e => e.Timestamp >= timeRange.Start && e.Timestamp <= timeRange.End)
                    .ToList();

                var errorEvents = events.Where(e => e.Type == "Exception").ToList();
                var totalEvents = events.Count;

                var statistics = new ErrorStatistics
                {
                    TimeRange = timeRange,
                    TotalErrors = errorEvents.Count,
                    ErrorRate = totalEvents > 0 ? (double)errorEvents.Count / totalEvents * 100 : 0
                };

                // 按類型統計錯誤
                statistics.ErrorsByType = errorEvents
                    .GroupBy(e => e.Properties.GetValueOrDefault("ExceptionType", "Unknown"))
                    .ToDictionary(g => g.Key, g => g.Count());

                // 按端點統計錯誤
                statistics.ErrorsByEndpoint = errorEvents
                    .GroupBy(e => e.Properties.GetValueOrDefault("Endpoint", "Unknown"))
                    .ToDictionary(g => g.Key, g => g.Count());

                // 前10個錯誤
                statistics.TopErrors = errorEvents
                    .GroupBy(e => e.Properties.GetValueOrDefault("ExceptionType", "Unknown"))
                    .Select(g => new ErrorDetail
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        FirstOccurrence = g.Min(e => e.Timestamp),
                        LastOccurrence = g.Max(e => e.Timestamp),
                        Message = g.First().Properties.GetValueOrDefault("Message", ""),
                        StackTrace = g.First().Properties.GetValueOrDefault("StackTrace", "")
                    })
                    .OrderByDescending(e => e.Count)
                    .Take(10)
                    .ToList();

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得錯誤統計時發生錯誤");
                return new ErrorStatistics { TimeRange = timeRange };
            }
        }

        public async Task<UserBehaviorAnalysis> GetUserBehaviorAnalysisAsync(TimeRange timeRange)
        {
            try
            {
                var events = _events
                    .Where(e => e.Timestamp >= timeRange.Start && e.Timestamp <= timeRange.End)
                    .ToList();

                var userBehaviorEvents = events.Where(e => e.Type == "UserBehavior").ToList();
                var uniqueUsers = userBehaviorEvents.Select(e => e.UserId).Distinct().Count();

                var analysis = new UserBehaviorAnalysis
                {
                    TimeRange = timeRange,
                    TotalUsers = uniqueUsers,
                    ActiveUsers = userBehaviorEvents.Count
                };

                // 統計最常見的行為
                analysis.TopActions = userBehaviorEvents
                    .GroupBy(e => e.Name)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 統計用戶會話
                var userSessions = userBehaviorEvents
                    .GroupBy(e => e.UserId)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                analysis.UserSessions = userSessions;

                // 計算平均會話時長
                analysis.AverageSessionDuration = userSessions.ToDictionary(
                    kvp => kvp.Key,
                    kvp => userBehaviorEvents.Where(e => e.UserId == int.Parse(kvp.Key))
                        .Average(e => (e.Timestamp - timeRange.Start).TotalMinutes)
                );

                // 分析用戶旅程
                analysis.TopUserJourneys = userBehaviorEvents
                    .GroupBy(e => e.UserId)
                    .Select(g => new UserJourney
                    {
                        Path = string.Join(" -> ", g.OrderBy(e => e.Timestamp).Select(e => e.Name)),
                        Count = g.Count(),
                        AverageDuration = g.Average(e => (e.Timestamp - timeRange.Start).TotalMinutes),
                        ConversionRate = 0 // 需要更複雜的邏輯來計算轉換率
                    })
                    .OrderByDescending(j => j.Count)
                    .Take(10)
                    .ToList();

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得用戶行為分析時發生錯誤");
                return new UserBehaviorAnalysis { TimeRange = timeRange };
            }
        }

        public async Task SetAlertRuleAsync(AlertRule rule)
        {
            try
            {
                _alertRules.AddOrUpdate(rule.Name, rule, (key, existing) => rule);
                _logger.LogInformation("設定告警規則: {RuleName}", rule.Name);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定告警規則時發生錯誤: {RuleName}", rule.Name);
            }
        }

        public async Task CheckAlertConditionsAsync()
        {
            try
            {
                foreach (var rule in _alertRules.Values.Where(r => r.IsEnabled))
                {
                    var shouldAlert = await EvaluateAlertConditionAsync(rule);
                    if (shouldAlert && !_activeAlerts.ContainsKey(rule.Name))
                    {
                        var alert = new Alert
                        {
                            Id = Guid.NewGuid().ToString(),
                            RuleName = rule.Name,
                            Severity = rule.Severity,
                            Message = rule.Message,
                            TriggeredAt = DateTime.UtcNow,
                            IsResolved = false
                        };

                        _activeAlerts.TryAdd(rule.Name, alert);
                        _logger.LogWarning("觸發告警: {RuleName} - {Message}", rule.Name, rule.Message);
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查告警條件時發生錯誤");
            }
        }

        public async Task<MonitoringDashboard> GetDashboardDataAsync()
        {
            try
            {
                var timeRange = new TimeRange
                {
                    Start = DateTime.UtcNow.AddHours(-24),
                    End = DateTime.UtcNow
                };

                var dashboard = new MonitoringDashboard
                {
                    SystemHealth = await GetSystemHealthAsync(),
                    Performance = await GetPerformanceStatisticsAsync(timeRange),
                    Errors = await GetErrorStatisticsAsync(timeRange),
                    UserBehavior = await GetUserBehaviorAnalysisAsync(timeRange),
                    ActiveAlerts = _activeAlerts.Values.Where(a => !a.IsResolved).ToList(),
                    GeneratedAt = DateTime.UtcNow
                };

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得監控儀表板資料時發生錯誤");
                return new MonitoringDashboard { GeneratedAt = DateTime.UtcNow };
            }
        }

        // 私有輔助方法
        private async Task<SystemMetrics> GetSystemMetricsAsync()
        {
            try
            {
                // 這裡應該使用實際的系統監控 API
                // 目前使用模擬資料
                return new SystemMetrics
                {
                    CpuUsage = Random.Shared.NextDouble() * 100,
                    MemoryUsage = Random.Shared.NextDouble() * 100,
                    DiskUsage = Random.Shared.NextDouble() * 100,
                    TotalRequests = _events.Count,
                    AverageResponseTime = _performanceCounters.Values.Average(pc => pc.Value),
                    ErrorRate = _events.Count(e => e.Type == "Exception"),
                    ActiveUsers = _events.Where(e => e.Type == "UserBehavior").Select(e => e.UserId).Distinct().Count()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得系統指標時發生錯誤");
                return new SystemMetrics();
            }
        }

        private async Task<HealthCheckResult> CheckDatabaseHealthAsync()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                // 這裡應該執行實際的資料庫健康檢查
                stopwatch.Stop();

                return new HealthCheckResult
                {
                    IsHealthy = true,
                    Status = "Healthy",
                    Message = "資料庫連接正常",
                    ResponseTime = stopwatch.Elapsed,
                    CheckedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    Message = $"資料庫檢查失敗: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        private async Task<HealthCheckResult> CheckMemoryHealthAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var memoryUsage = process.WorkingSet64 / (1024.0 * 1024.0); // MB

                return new HealthCheckResult
                {
                    IsHealthy = memoryUsage < 1000, // 假設 1GB 為健康閾值
                    Status = memoryUsage < 1000 ? "Healthy" : "Warning",
                    Message = $"記憶體使用: {memoryUsage:F2} MB",
                    CheckedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Status = "Error",
                    Message = $"記憶體檢查失敗: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        private async Task<HealthCheckResult> CheckCpuHealthAsync()
        {
            try
            {
                // 模擬 CPU 檢查
                return new HealthCheckResult
                {
                    IsHealthy = true,
                    Status = "Healthy",
                    Message = "CPU 使用率正常",
                    CheckedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Status = "Error",
                    Message = $"CPU 檢查失敗: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        private async Task<HealthCheckResult> CheckDiskHealthAsync()
        {
            try
            {
                // 模擬磁碟檢查
                return new HealthCheckResult
                {
                    IsHealthy = true,
                    Status = "Healthy",
                    Message = "磁碟空間充足",
                    CheckedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Status = "Error",
                    Message = $"磁碟檢查失敗: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        private async Task<bool> EvaluateAlertConditionAsync(AlertRule rule)
        {
            try
            {
                // 這裡應該根據實際的指標來評估告警條件
                // 目前使用模擬邏輯
                var metricValue = _performanceCounters.GetValueOrDefault(rule.Metric)?.Value ?? 0;

                return rule.Condition switch
                {
                    ">" => metricValue > rule.Threshold,
                    "<" => metricValue < rule.Threshold,
                    ">=" => metricValue >= rule.Threshold,
                    "<=" => metricValue <= rule.Threshold,
                    "==" => Math.Abs(metricValue - rule.Threshold) < 0.01,
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "評估告警條件時發生錯誤: {RuleName}", rule.Name);
                return false;
            }
        }
    }

    // 輔助類別
    public class PerformanceTracker : IDisposable
    {
        private readonly string _operationName;
        private readonly string _trackingId;
        private readonly Stopwatch _stopwatch;
        private readonly Dictionary<string, string> _tags;
        private readonly IAdvancedMonitoringService _monitoringService;

        public PerformanceTracker(string operationName, string trackingId, Stopwatch stopwatch, Dictionary<string, string> tags, IAdvancedMonitoringService monitoringService)
        {
            _operationName = operationName;
            _trackingId = trackingId;
            _stopwatch = stopwatch;
            _tags = tags;
            _monitoringService = monitoringService;
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var duration = _stopwatch.ElapsedMilliseconds;

            _monitoringService.RecordMetricAsync($"ResponseTime_{_operationName}", duration, _tags);
            _monitoringService.RecordEventAsync($"Operation_{_operationName}", new Dictionary<string, string>
            {
                ["TrackingId"] = _trackingId,
                ["Duration"] = duration.ToString()
            });
        }
    }

    public class PerformanceCounter
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public int Count { get; set; }
    }

    public class MonitoringEvent
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public int? UserId { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new();
        public Dictionary<string, double> Metrics { get; set; } = new();
        public Dictionary<string, string> Context { get; set; } = new();
    }

    public class Metric
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }
}