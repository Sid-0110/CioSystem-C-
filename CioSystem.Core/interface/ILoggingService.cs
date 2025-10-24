using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CioSystem.Core.Interfaces
{
    /// <summary>
    /// 日誌服務接口
    /// 提供統一的日誌記錄抽象
    /// </summary>
    public interface ILoggingService : IService
    {
        /// <summary>
        /// 記錄追蹤日誌
        /// </summary>
        /// <param name="message">日誌訊息</param>
        /// <param name="properties">額外屬性</param>
        Task LogTraceAsync(string message, IDictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄除錯日誌
        /// </summary>
        /// <param name="message">日誌訊息</param>
        /// <param name="properties">額外屬性</param>
        Task LogDebugAsync(string message, IDictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄資訊日誌
        /// </summary>
        /// <param name="message">日誌訊息</param>
        /// <param name="properties">額外屬性</param>
        Task LogInformationAsync(string message, IDictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄警告日誌
        /// </summary>
        /// <param name="message">日誌訊息</param>
        /// <param name="exception">例外（可選）</param>
        /// <param name="properties">額外屬性</param>
        Task LogWarningAsync(string message, Exception? exception = null, IDictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄錯誤日誌
        /// </summary>
        /// <param name="message">日誌訊息</param>
        /// <param name="exception">例外（可選）</param>
        /// <param name="properties">額外屬性</param>
        Task LogErrorAsync(string message, Exception? exception = null, IDictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄嚴重錯誤日誌
        /// </summary>
        /// <param name="message">日誌訊息</param>
        /// <param name="exception">例外（可選）</param>
        /// <param name="properties">額外屬性</param>
        Task LogCriticalAsync(string message, Exception? exception = null, IDictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄業務日誌
        /// </summary>
        /// <param name="operation">操作名稱</param>
        /// <param name="entityType">實體類型</param>
        /// <param name="entityId">實體ID</param>
        /// <param name="message">日誌訊息</param>
        /// <param name="properties">額外屬性</param>
        Task LogBusinessAsync(string operation, string entityType, string entityId, string message, IDictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄審計日誌
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="action">操作動作</param>
        /// <param name="resource">資源</param>
        /// <param name="result">操作結果</param>
        /// <param name="properties">額外屬性</param>
        Task LogAuditAsync(string userId, string action, string resource, string result, IDictionary<string, object>? properties = null);

        /// <summary>
        /// 記錄性能日誌
        /// </summary>
        /// <param name="operation">操作名稱</param>
        /// <param name="duration">執行時間</param>
        /// <param name="properties">額外屬性</param>
        Task LogPerformanceAsync(string operation, TimeSpan duration, IDictionary<string, object>? properties = null);

        /// <summary>
        /// 批量記錄日誌
        /// </summary>
        /// <param name="logEntries">日誌條目列表</param>
        /// <returns>成功記錄的數量</returns>
        Task<int> LogBatchAsync(IEnumerable<LogEntry> logEntries);

        /// <summary>
        /// 取得日誌統計資訊
        /// </summary>
        /// <returns>日誌統計資訊</returns>
        Task<LoggingStatistics> GetStatisticsAsync();

        /// <summary>
        /// 設定日誌級別
        /// </summary>
        /// <param name="level">日誌級別</param>
        Task SetLogLevelAsync(LogLevel level);

        /// <summary>
        /// 清除過期日誌
        /// </summary>
        /// <param name="olderThan">清除比此時間更早的日誌</param>
        /// <returns>清除的日誌數量</returns>
        Task<int> CleanupAsync(DateTime olderThan);
    }

    /// <summary>
    /// 日誌級別
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    /// <summary>
    /// 日誌條目
    /// </summary>
    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public IDictionary<string, object>? Properties { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }
        public string? Operation { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
    }

    /// <summary>
    /// 日誌統計資訊
    /// </summary>
    public class LoggingStatistics
    {
        public long TotalLogs { get; set; }
        public long TraceCount { get; set; }
        public long DebugCount { get; set; }
        public long InformationCount { get; set; }
        public long WarningCount { get; set; }
        public long ErrorCount { get; set; }
        public long CriticalCount { get; set; }
        public long BusinessLogCount { get; set; }
        public long AuditLogCount { get; set; }
        public long PerformanceLogCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}