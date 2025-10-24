using CioSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CioSystem.Services.Monitoring
{
    /// <summary>
    /// 系統監控服務實現
    /// 提供系統性能監控和指標收集
    /// </summary>
    public class SystemMonitoringService : IMonitoringService
    {
        private readonly ILogger<SystemMonitoringService> _logger;
        private readonly MonitoringConfiguration _config;
        private readonly ConcurrentDictionary<string, List<MetricData>> _metrics;
        private readonly ConcurrentDictionary<string, CioSystem.Core.Interfaces.AlertRule> _alertRules;
        private readonly ConcurrentQueue<CioSystem.Core.Interfaces.Alert> _alerts;
        // PerformanceCounter 在 macOS 上不可用，使用替代方案
        // private readonly System.Diagnostics.PerformanceCounter? _cpuCounter;
        // private readonly System.Diagnostics.PerformanceCounter? _memoryCounter;
        private readonly DateTime _startTime;

        public string ServiceName => "SystemMonitoringService";
        public string Version => "1.0.0";
        public bool IsAvailable => true;

        public SystemMonitoringService(
            ILogger<SystemMonitoringService> logger,
            MonitoringConfiguration config)
        {
            _logger = logger;
            _config = config;
            _metrics = new ConcurrentDictionary<string, List<MetricData>>();
            _alertRules = new ConcurrentDictionary<string, CioSystem.Core.Interfaces.AlertRule>();
            _alerts = new ConcurrentQueue<CioSystem.Core.Interfaces.Alert>();
            _startTime = DateTime.UtcNow;

            // PerformanceCounter 在 macOS 上不可用，使用替代方案
            // try
            // {
            //     // 初始化性能計數器（僅在 Windows 上可用）
            //     if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            //     {
            //         _cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
            //         _memoryCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
            //     }
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogWarning(ex, "無法初始化性能計數器，將使用替代方法");
            // }
        }

        public async Task RecordMetricAsync(string metricName, double value, IDictionary<string, string>? tags = null)
        {
            try
            {
                var metric = new MetricData
                {
                    Name = metricName,
                    Value = value,
                    Timestamp = DateTime.UtcNow,
                    Tags = tags
                };

                _metrics.AddOrUpdate(metricName,
                    new List<MetricData> { metric },
                    (key, existing) =>
                    {
                        existing.Add(metric);
                        // 保持最近 1000 個指標
                        if (existing.Count > 1000)
                        {
                            existing.RemoveAt(0);
                        }
                        return existing;
                    });

                _logger.LogDebug("記錄指標: {MetricName} = {Value}", metricName, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄指標時發生錯誤: {MetricName}", metricName);
            }
        }

        public async Task IncrementCounterAsync(string counterName, double increment = 1, IDictionary<string, string>? tags = null)
        {
            await RecordMetricAsync(counterName, increment, tags);
        }

        public async Task RecordTimerAsync(string timerName, TimeSpan duration, IDictionary<string, string>? tags = null)
        {
            await RecordMetricAsync(timerName, duration.TotalMilliseconds, tags);
        }

        public async Task RecordHistogramAsync(string histogramName, double value, IDictionary<string, string>? tags = null)
        {
            await RecordMetricAsync(histogramName, value, tags);
        }

        public async Task RecordEventAsync(string eventName, IDictionary<string, object>? properties = null)
        {
            try
            {
                var tags = properties?.ToDictionary(p => p.Key, p => p.Value?.ToString() ?? string.Empty);
                await RecordMetricAsync($"event.{eventName}", 1, tags);
                _logger.LogInformation("記錄事件: {EventName}", eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄事件時發生錯誤: {EventName}", eventName);
            }
        }

        public async Task RecordExceptionAsync(Exception exception, IDictionary<string, object>? context = null)
        {
            try
            {
                var tags = new Dictionary<string, string>
                {
                    ["ExceptionType"] = exception.GetType().Name,
                    ["ExceptionMessage"] = exception.Message
                };

                if (context != null)
                {
                    foreach (var item in context)
                    {
                        tags[item.Key] = item.Value?.ToString() ?? string.Empty;
                    }
                }

                await RecordMetricAsync("exceptions", 1, tags);
                _logger.LogError(exception, "記錄例外: {ExceptionType}", exception.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄例外時發生錯誤");
            }
        }

        public async Task RecordDependencyAsync(string dependencyName, string commandName, DateTime startTime, TimeSpan duration, bool success)
        {
            try
            {
                var tags = new Dictionary<string, string>
                {
                    ["DependencyName"] = dependencyName,
                    ["CommandName"] = commandName,
                    ["Success"] = success.ToString()
                };

                await RecordTimerAsync($"dependency.{dependencyName}", duration, tags);
                await RecordMetricAsync($"dependency.{dependencyName}.success", success ? 1 : 0, tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄依賴調用時發生錯誤: {DependencyName}", dependencyName);
            }
        }

        public async Task RecordRequestAsync(string requestName, string url, DateTime startTime, TimeSpan duration, int responseCode, bool success)
        {
            try
            {
                var tags = new Dictionary<string, string>
                {
                    ["RequestName"] = requestName,
                    ["Url"] = url,
                    ["ResponseCode"] = responseCode.ToString(),
                    ["Success"] = success.ToString()
                };

                await RecordTimerAsync($"request.{requestName}", duration, tags);
                await RecordMetricAsync($"request.{requestName}.success", success ? 1 : 0, tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄請求時發生錯誤: {RequestName}", requestName);
            }
        }

        public async Task<CioSystem.Core.Interfaces.SystemHealthStatus> GetSystemHealthAsync()
        {
            try
            {
                var resourceUsage = await GetResourceUsageAsync();
                var isHealthy = resourceUsage.CpuUsage < 90 && resourceUsage.MemoryUsage < 90;

                return new CioSystem.Core.Interfaces.SystemHealthStatus
                {
                    IsHealthy = isHealthy,
                    Status = isHealthy ? "Healthy" : "Unhealthy",
                    Message = isHealthy ? "系統運行正常" : "系統資源使用率過高",
                    CheckedAt = DateTime.UtcNow,
                    ResourceUsage = resourceUsage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得系統健康狀態時發生錯誤");
                return new CioSystem.Core.Interfaces.SystemHealthStatus
                {
                    IsHealthy = false,
                    Status = "Error",
                    Message = $"取得系統健康狀態失敗: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<IEnumerable<MetricData>> GetMetricsAsync(string? metricName = null, CioSystem.Core.Interfaces.TimeRange? timeRange = null)
        {
            try
            {
                var allMetrics = new List<MetricData>();

                if (string.IsNullOrEmpty(metricName))
                {
                    foreach (var metricList in _metrics.Values)
                    {
                        allMetrics.AddRange(metricList);
                    }
                }
                else if (_metrics.TryGetValue(metricName, out var metrics))
                {
                    allMetrics.AddRange(metrics);
                }

                if (timeRange != null)
                {
                    allMetrics = allMetrics
                        .Where(m => m.Timestamp >= timeRange.StartTime && m.Timestamp <= timeRange.EndTime)
                        .ToList();
                }

                return allMetrics.OrderByDescending(m => m.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得指標時發生錯誤");
                return Enumerable.Empty<MetricData>();
            }
        }

        public async Task<SystemResourceUsage> GetResourceUsageAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var workingSet = process.WorkingSet64;
                var totalMemory = GC.GetTotalMemory(false);

                double cpuUsage = 0;
                long availableMemory = 0;

                // PerformanceCounter 在 macOS 上不可用，使用替代方案
                // if (_cpuCounter != null)
                // {
                //     cpuUsage = _cpuCounter.NextValue();
                // }

                // if (_memoryCounter != null)
                // {
                //     availableMemory = (long)(_memoryCounter.NextValue() * 1024 * 1024); // 轉換為字節
                // }

                return new SystemResourceUsage
                {
                    CpuUsage = cpuUsage,
                    MemoryUsage = (long)((double)workingSet / (1024 * 1024)), // MB
                    AvailableMemory = availableMemory,
                    DiskUsage = 0, // 需要額外實現
                    AvailableDisk = 0, // 需要額外實現
                    ActiveConnections = 0, // 需要額外實現
                    ThreadCount = process.Threads.Count,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得系統資源使用情況時發生錯誤");
                return new SystemResourceUsage
                {
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task SetAlertRuleAsync(CioSystem.Core.Interfaces.AlertRule rule)
        {
            try
            {
                _alertRules.AddOrUpdate(rule.Id, rule, (key, existing) => rule);
                _logger.LogInformation("設定警報規則: {RuleId} - {RuleName}", rule.Id, rule.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定警報規則時發生錯誤: {RuleId}", rule.Id);
            }
        }

        public async Task<IEnumerable<CioSystem.Core.Interfaces.AlertRule>> GetAlertRulesAsync()
        {
            return _alertRules.Values.ToList();
        }

        public async Task<IEnumerable<CioSystem.Core.Interfaces.Alert>> CheckAlertsAsync()
        {
            var triggeredAlerts = new List<CioSystem.Core.Interfaces.Alert>();

            try
            {
                foreach (var rule in _alertRules.Values.Where(r => r.IsEnabled))
                {
                    var metrics = await GetMetricsAsync(rule.MetricName, new CioSystem.Core.Interfaces.TimeRange
                    {
                        StartTime = DateTime.UtcNow.Add(-rule.EvaluationPeriod),
                        EndTime = DateTime.UtcNow
                    });

                    if (metrics.Any())
                    {
                        var averageValue = metrics.Average(m => m.Value);
                        var shouldTrigger = rule.Condition switch
                        {
                            "greater_than" => averageValue > rule.Threshold,
                            "less_than" => averageValue < rule.Threshold,
                            "equals" => Math.Abs(averageValue - rule.Threshold) < 0.01,
                            _ => false
                        };

                        if (shouldTrigger)
                        {
                            var alert = new CioSystem.Core.Interfaces.Alert
                            {
                                Id = Guid.NewGuid().ToString(),
                                RuleId = rule.Id,
                                Message = $"警報觸發: {rule.Name} - 當前值: {averageValue:F2}, 閾值: {rule.Threshold}",
                                Severity = CioSystem.Core.Interfaces.AlertSeverity.Medium,
                                TriggeredAt = DateTime.UtcNow
                            };

                            triggeredAlerts.Add(alert);
                            _alerts.Enqueue(alert);
                            _logger.LogWarning("警報觸發: {RuleName} - {Message}", rule.Name, alert.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查警報時發生錯誤");
            }

            return triggeredAlerts;
        }

        public async Task<ServiceHealthStatus> HealthCheckAsync()
        {
            try
            {
                var systemHealth = await GetSystemHealthAsync();
                return new ServiceHealthStatus
                {
                    IsHealthy = systemHealth.IsHealthy,
                    Status = systemHealth.Status,
                    Message = systemHealth.Message,
                    CheckedAt = DateTime.UtcNow,
                    ResponseTime = TimeSpan.FromMilliseconds(1)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "監控服務健康檢查失敗");
                return new ServiceHealthStatus
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    Message = $"監控服務健康檢查失敗: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logger.LogInformation("初始化系統監控服務");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化系統監控服務失敗");
                return false;
            }
        }

        public async Task<bool> CleanupAsync()
        {
            try
            {
                // PerformanceCounter 在 macOS 上不可用
                // _cpuCounter?.Dispose();
                // _memoryCounter?.Dispose();
                _metrics.Clear();
                _alertRules.Clear();
                _logger.LogInformation("清理監控服務資源");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理監控服務資源失敗");
                return false;
            }
        }
    }

    /// <summary>
    /// 監控配置
    /// </summary>
    public class MonitoringConfiguration
    {
        public bool EnablePerformanceCounters { get; set; } = true;
        public bool EnableAlerting { get; set; } = true;
        public TimeSpan MetricsRetentionPeriod { get; set; } = TimeSpan.FromHours(24);
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
        public int MaxMetricsPerType { get; set; } = 1000;
    }
}