using System.Diagnostics;

namespace CioSystem.Services.Monitoring
{
    /// <summary>
    /// 進階監控服務介面
    /// 提供全面的系統監控和效能分析
    /// </summary>
    public interface IAdvancedMonitoringService
    {
        /// <summary>
        /// 開始效能追蹤
        /// </summary>
        /// <param name="operationName">操作名稱</param>
        /// <param name="tags">標籤</param>
        /// <returns>效能追蹤器</returns>
        IDisposable StartPerformanceTracking(string operationName, Dictionary<string, string>? tags = null);

        /// <summary>
        /// 記錄效能指標
        /// </summary>
        /// <param name="metricName">指標名稱</param>
        /// <param name="value">數值</param>
        /// <param name="tags">標籤</param>
        Task RecordMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// 記錄自定義事件
        /// </summary>
        /// <param name="eventName">事件名稱</param>
        /// <param name="properties">屬性</param>
        /// <param name="metrics">指標</param>
        Task RecordEventAsync(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null);

        /// <summary>
        /// 記錄異常
        /// </summary>
        /// <param name="exception">異常</param>
        /// <param name="context">上下文</param>
        /// <param name="severity">嚴重程度</param>
        Task RecordExceptionAsync(Exception exception, Dictionary<string, string>? context = null, string severity = "Error");

        /// <summary>
        /// 記錄用戶行為
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="action">行為</param>
        /// <param name="properties">屬性</param>
        Task RecordUserBehaviorAsync(int userId, string action, Dictionary<string, string>? properties = null);

        /// <summary>
        /// 記錄業務事件
        /// </summary>
        /// <param name="eventType">事件類型</param>
        /// <param name="entityType">實體類型</param>
        /// <param name="entityId">實體ID</param>
        /// <param name="properties">屬性</param>
        Task RecordBusinessEventAsync(BusinessEventType eventType, string entityType, int entityId, Dictionary<string, string>? properties = null);

        /// <summary>
        /// 取得系統健康狀態
        /// </summary>
        /// <returns>健康狀態</returns>
        Task<SystemHealthStatus> GetSystemHealthAsync();

        /// <summary>
        /// 取得效能統計
        /// </summary>
        /// <param name="timeRange">時間範圍</param>
        /// <returns>效能統計</returns>
        Task<PerformanceStatistics> GetPerformanceStatisticsAsync(TimeRange timeRange);

        /// <summary>
        /// 取得錯誤統計
        /// </summary>
        /// <param name="timeRange">時間範圍</param>
        /// <returns>錯誤統計</returns>
        Task<ErrorStatistics> GetErrorStatisticsAsync(TimeRange timeRange);

        /// <summary>
        /// 取得用戶行為分析
        /// </summary>
        /// <param name="timeRange">時間範圍</param>
        /// <returns>用戶行為分析</returns>
        Task<UserBehaviorAnalysis> GetUserBehaviorAnalysisAsync(TimeRange timeRange);

        /// <summary>
        /// 設定告警規則
        /// </summary>
        /// <param name="rule">告警規則</param>
        Task SetAlertRuleAsync(AlertRule rule);

        /// <summary>
        /// 檢查告警條件
        /// </summary>
        Task CheckAlertConditionsAsync();

        /// <summary>
        /// 取得監控儀表板資料
        /// </summary>
        /// <returns>儀表板資料</returns>
        Task<MonitoringDashboard> GetDashboardDataAsync();
    }

    /// <summary>
    /// 系統健康狀態
    /// </summary>
    public class SystemHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
        public Dictionary<string, HealthCheckResult> Components { get; set; } = new();
        public SystemMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// 健康檢查結果
    /// </summary>
    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    /// <summary>
    /// 系統指標
    /// </summary>
    public class SystemMetrics
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public long TotalRequests { get; set; }
        public double AverageResponseTime { get; set; }
        public int ErrorRate { get; set; }
        public int ActiveUsers { get; set; }
    }

    /// <summary>
    /// 效能統計
    /// </summary>
    public class PerformanceStatistics
    {
        public TimeRange TimeRange { get; set; } = new();
        public double AverageResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public long TotalRequests { get; set; }
        public double RequestsPerSecond { get; set; }
        public double ErrorRate { get; set; }
        public Dictionary<string, double> TopSlowOperations { get; set; } = new();
        public Dictionary<string, long> RequestCountByEndpoint { get; set; } = new();
    }

    /// <summary>
    /// 錯誤統計
    /// </summary>
    public class ErrorStatistics
    {
        public TimeRange TimeRange { get; set; } = new();
        public int TotalErrors { get; set; }
        public double ErrorRate { get; set; }
        public Dictionary<string, int> ErrorsByType { get; set; } = new();
        public Dictionary<string, int> ErrorsByEndpoint { get; set; } = new();
        public List<ErrorDetail> TopErrors { get; set; } = new();
    }

    /// <summary>
    /// 錯誤詳情
    /// </summary>
    public class ErrorDetail
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
        public string StackTrace { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用戶行為分析
    /// </summary>
    public class UserBehaviorAnalysis
    {
        public TimeRange TimeRange { get; set; } = new();
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public Dictionary<string, int> TopActions { get; set; } = new();
        public Dictionary<string, int> UserSessions { get; set; } = new();
        public Dictionary<string, double> AverageSessionDuration { get; set; } = new();
        public List<UserJourney> TopUserJourneys { get; set; } = new();
    }

    /// <summary>
    /// 用戶旅程
    /// </summary>
    public class UserJourney
    {
        public string Path { get; set; } = string.Empty;
        public int Count { get; set; }
        public double AverageDuration { get; set; }
        public double ConversionRate { get; set; }
    }

    /// <summary>
    /// 告警規則
    /// </summary>
    public class AlertRule
    {
        public string Name { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// 監控儀表板
    /// </summary>
    public class MonitoringDashboard
    {
        public SystemHealthStatus SystemHealth { get; set; } = new();
        public PerformanceStatistics Performance { get; set; } = new();
        public ErrorStatistics Errors { get; set; } = new();
        public UserBehaviorAnalysis UserBehavior { get; set; } = new();
        public List<Alert> ActiveAlerts { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// 告警
    /// </summary>
    public class Alert
    {
        public string Id { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    /// <summary>
    /// 時間範圍
    /// </summary>
    public class TimeRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    /// <summary>
    /// 業務事件類型
    /// </summary>
    public enum BusinessEventType
    {
        ProductCreated,
        ProductUpdated,
        ProductDeleted,
        InventoryUpdated,
        SaleCreated,
        PurchaseCreated,
        UserLoggedIn,
        UserLoggedOut,
        PermissionChanged,
        DataExported,
        DataImported
    }
}