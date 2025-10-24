using Microsoft.Extensions.Logging;
using CioSystem.Core.Interfaces;

namespace CioSystem.Services.Logging
{
    /// <summary>
    /// 結構化日誌服務介面
    /// 提供結構化日誌記錄和分析功能
    /// </summary>
    public interface IStructuredLoggingService
    {
        /// <summary>
        /// 記錄結構化日誌
        /// </summary>
        /// <param name="logLevel">日誌級別</param>
        /// <param name="message">訊息</param>
        /// <param name="properties">屬性</param>
        /// <param name="metrics">指標</param>
        Task LogAsync(CioSystem.Core.Interfaces.LogLevel logLevel, string message, Dictionary<string, object>? properties = null, Dictionary<string, double>? metrics = null);

        /// <summary>
        /// 記錄業務日誌
        /// </summary>
        /// <param name="businessEvent">業務事件</param>
        /// <param name="entityType">實體類型</param>
        /// <param name="entityId">實體ID</param>
        /// <param name="properties">屬性</param>
        Task LogBusinessEventAsync(string businessEvent, string entityType, int entityId, Dictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄用戶操作日誌
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="action">操作</param>
        /// <param name="resource">資源</param>
        /// <param name="properties">屬性</param>
        Task LogUserActionAsync(int userId, string action, string resource, Dictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄效能日誌
        /// </summary>
        /// <param name="operation">操作</param>
        /// <param name="duration">持續時間</param>
        /// <param name="properties">屬性</param>
        Task LogPerformanceAsync(string operation, TimeSpan duration, Dictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄錯誤日誌
        /// </summary>
        /// <param name="exception">異常</param>
        /// <param name="context">上下文</param>
        /// <param name="severity">嚴重程度</param>
        Task LogErrorAsync(Exception exception, Dictionary<string, object>? context = null, string severity = "Error");

        /// <summary>
        /// 記錄安全日誌
        /// </summary>
        /// <param name="securityEvent">安全事件</param>
        /// <param name="userId">用戶ID</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="properties">屬性</param>
        Task LogSecurityAsync(string securityEvent, int? userId, string? ipAddress, Dictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄審計日誌
        /// </summary>
        /// <param name="auditEvent">審計事件</param>
        /// <param name="entityType">實體類型</param>
        /// <param name="entityId">實體ID</param>
        /// <param name="changes">變更</param>
        Task LogAuditAsync(string auditEvent, string entityType, int entityId, Dictionary<string, object>? changes = null);

        /// <summary>
        /// 查詢日誌
        /// </summary>
        /// <param name="query">查詢條件</param>
        /// <returns>日誌記錄</returns>
        Task<IEnumerable<StructuredLogEntry>> QueryLogsAsync(LogQuery query);

        /// <summary>
        /// 取得日誌統計
        /// </summary>
        /// <param name="timeRange">時間範圍</param>
        /// <returns>日誌統計</returns>
        Task<LogStatistics> GetLogStatisticsAsync(TimeRange timeRange);

        /// <summary>
        /// 取得日誌分析
        /// </summary>
        /// <param name="timeRange">時間範圍</param>
        /// <returns>日誌分析</returns>
        Task<LogAnalysis> GetLogAnalysisAsync(TimeRange timeRange);

        /// <summary>
        /// 匯出日誌
        /// </summary>
        /// <param name="query">查詢條件</param>
        /// <param name="format">匯出格式</param>
        /// <returns>匯出資料</returns>
        Task<byte[]> ExportLogsAsync(LogQuery query, ExportFormat format);

        /// <summary>
        /// 清理舊日誌
        /// </summary>
        /// <param name="retentionDays">保留天數</param>
        Task CleanupOldLogsAsync(int retentionDays);
    }

    /// <summary>
    /// 結構化日誌記錄
    /// </summary>
    public class StructuredLogEntry
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public CioSystem.Core.Interfaces.LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? SessionId { get; set; }
        public string? RequestId { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public Dictionary<string, double> Metrics { get; set; } = new();
        public string? Exception { get; set; }
        public string? StackTrace { get; set; }
    }

    /// <summary>
    /// 日誌查詢條件
    /// </summary>
    public class LogQuery
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public CioSystem.Core.Interfaces.LogLevel? MinLevel { get; set; }
        public string? Category { get; set; }
        public int? UserId { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; } = 100;
        public string? OrderBy { get; set; }
        public bool OrderDescending { get; set; } = true;
    }

    /// <summary>
    /// 日誌統計
    /// </summary>
    public class LogStatistics
    {
        public TimeRange TimeRange { get; set; } = new();
        public long TotalLogs { get; set; }
        public Dictionary<CioSystem.Core.Interfaces.LogLevel, long> LogsByLevel { get; set; } = new();
        public Dictionary<string, long> LogsByCategory { get; set; } = new();
        public Dictionary<int, long> LogsByUser { get; set; } = new();
        public Dictionary<string, long> TopMessages { get; set; } = new();
        public Dictionary<string, long> TopProperties { get; set; } = new();
        public double AverageLogsPerHour { get; set; }
        public long ErrorCount { get; set; }
        public long WarningCount { get; set; }
        public long InfoCount { get; set; }
    }

    /// <summary>
    /// 日誌分析
    /// </summary>
    public class LogAnalysis
    {
        public TimeRange TimeRange { get; set; } = new();
        public List<LogPattern> Patterns { get; set; } = new();
        public List<LogAnomaly> Anomalies { get; set; } = new();
        public List<LogTrend> Trends { get; set; } = new();
        public List<LogCorrelation> Correlations { get; set; } = new();
        public LogInsights Insights { get; set; } = new();
    }

    /// <summary>
    /// 日誌模式
    /// </summary>
    public class LogPattern
    {
        public string Pattern { get; set; } = string.Empty;
        public int Frequency { get; set; }
        public double Confidence { get; set; }
        public List<string> Examples { get; set; } = new();
    }

    /// <summary>
    /// 日誌異常
    /// </summary>
    public class LogAnomaly
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public double Severity { get; set; }
        public List<string> AffectedLogs { get; set; } = new();
    }

    /// <summary>
    /// 日誌趨勢
    /// </summary>
    public class LogTrend
    {
        public string Metric { get; set; } = string.Empty;
        public List<DataPoint> DataPoints { get; set; } = new();
        public TrendDirection Direction { get; set; }
        public double ChangeRate { get; set; }
    }

    /// <summary>
    /// 資料點
    /// </summary>
    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// 日誌關聯
    /// </summary>
    public class LogCorrelation
    {
        public string Event1 { get; set; } = string.Empty;
        public string Event2 { get; set; } = string.Empty;
        public double Correlation { get; set; }
        public TimeSpan TimeDifference { get; set; }
    }

    /// <summary>
    /// 日誌洞察
    /// </summary>
    public class LogInsights
    {
        public List<string> Recommendations { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Alerts { get; set; } = new();
        public Dictionary<string, object> Summary { get; set; } = new();
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
    /// 趨勢方向
    /// </summary>
    public enum TrendDirection
    {
        Increasing,
        Decreasing,
        Stable,
        Volatile
    }

    /// <summary>
    /// 匯出格式
    /// </summary>
    public enum ExportFormat
    {
        Json,
        Csv,
        Excel,
        Pdf
    }
}