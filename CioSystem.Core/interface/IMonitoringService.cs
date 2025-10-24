using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CioSystem.Core.Interfaces
{
    /// <summary>
    /// 監控服務接口
    /// 提供系統監控和性能指標收集
    /// </summary>
    public interface IMonitoringService : IService
    {
        /// <summary>
        /// 記錄性能指標
        /// </summary>
        /// <param name="metricName">指標名稱</param>
        /// <param name="value">指標值</param>
        /// <param name="tags">標籤</param>
        Task RecordMetricAsync(string metricName, double value, IDictionary<string, string>? tags = null);

        /// <summary>
        /// 記錄計數器指標
        /// </summary>
        /// <param name="counterName">計數器名稱</param>
        /// <param name="increment">增量</param>
        /// <param name="tags">標籤</param>
        Task IncrementCounterAsync(string counterName, double increment = 1, IDictionary<string, string>? tags = null);

        /// <summary>
        /// 記錄計時器指標
        /// </summary>
        /// <param name="timerName">計時器名稱</param>
        /// <param name="duration">持續時間</param>
        /// <param name="tags">標籤</param>
        Task RecordTimerAsync(string timerName, TimeSpan duration, IDictionary<string, string>? tags = null);

        /// <summary>
        /// 記錄直方圖指標
        /// </summary>
        /// <param name="histogramName">直方圖名稱</param>
        /// <param name="value">值</param>
        /// <param name="tags">標籤</param>
        Task RecordHistogramAsync(string histogramName, double value, IDictionary<string, string>? tags = null);

        /// <summary>
        /// 記錄自定義事件
        /// </summary>
        /// <param name="eventName">事件名稱</param>
        /// <param name="properties">事件屬性</param>
        Task RecordEventAsync(string eventName, IDictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄異常
        /// </summary>
        /// <param name="exception">例外</param>
        /// <param name="context">上下文</param>
        Task RecordExceptionAsync(Exception exception, IDictionary<string, object>? context = null);

        /// <summary>
        /// 記錄依賴調用
        /// </summary>
        /// <param name="dependencyName">依賴名稱</param>
        /// <param name="commandName">命令名稱</param>
        /// <param name="startTime">開始時間</param>
        /// <param name="duration">持續時間</param>
        /// <param name="success">是否成功</param>
        Task RecordDependencyAsync(string dependencyName, string commandName, DateTime startTime, TimeSpan duration, bool success);

        /// <summary>
        /// 記錄請求
        /// </summary>
        /// <param name="requestName">請求名稱</param>
        /// <param name="url">URL</param>
        /// <param name="startTime">開始時間</param>
        /// <param name="duration">持續時間</param>
        /// <param name="responseCode">回應代碼</param>
        /// <param name="success">是否成功</param>
        Task RecordRequestAsync(string requestName, string url, DateTime startTime, TimeSpan duration, int responseCode, bool success);

        /// <summary>
        /// 取得系統健康狀態
        /// </summary>
        /// <returns>系統健康狀態</returns>
        Task<SystemHealthStatus> GetSystemHealthAsync();

        /// <summary>
        /// 取得性能指標
        /// </summary>
        /// <param name="metricName">指標名稱（可選）</param>
        /// <param name="timeRange">時間範圍（可選）</param>
        /// <returns>性能指標列表</returns>
        Task<IEnumerable<MetricData>> GetMetricsAsync(string? metricName = null, TimeRange? timeRange = null);

        /// <summary>
        /// 取得系統資源使用情況
        /// </summary>
        /// <returns>系統資源使用情況</returns>
        Task<SystemResourceUsage> GetResourceUsageAsync();

        /// <summary>
        /// 設定警報規則
        /// </summary>
        /// <param name="rule">警報規則</param>
        Task SetAlertRuleAsync(AlertRule rule);

        /// <summary>
        /// 取得警報規則
        /// </summary>
        /// <returns>警報規則列表</returns>
        Task<IEnumerable<AlertRule>> GetAlertRulesAsync();

        /// <summary>
        /// 檢查警報條件
        /// </summary>
        /// <returns>觸發的警報列表</returns>
        Task<IEnumerable<Alert>> CheckAlertsAsync();
    }

    /// <summary>
    /// 系統健康狀態
    /// </summary>
    public class SystemHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public SystemResourceUsage? ResourceUsage { get; set; }
        public IEnumerable<ServiceHealthStatus>? ServiceStatuses { get; set; }
    }

    /// <summary>
    /// 系統資源使用情況
    /// </summary>
    public class SystemResourceUsage
    {
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public long AvailableMemory { get; set; }
        public long DiskUsage { get; set; }
        public long AvailableDisk { get; set; }
        public int ActiveConnections { get; set; }
        public int ThreadCount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 指標數據
    /// </summary>
    public class MetricData
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public IDictionary<string, string>? Tags { get; set; }
    }

    /// <summary>
    /// 時間範圍
    /// </summary>
    public class TimeRange
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// 警報規則
    /// </summary>
    public class AlertRule
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public TimeSpan EvaluationPeriod { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? NotificationChannel { get; set; }
    }

    /// <summary>
    /// 警報
    /// </summary>
    public class Alert
    {
        public string Id { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    /// <summary>
    /// 警報嚴重程度
    /// </summary>
    public enum AlertSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}