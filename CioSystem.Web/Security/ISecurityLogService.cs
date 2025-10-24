namespace CioSystem.Web.Security
{
    /// <summary>
    /// 安全日誌服務介面
    /// 提供安全事件記錄和監控功能
    /// </summary>
    public interface ISecurityLogService
    {
        /// <summary>
        /// 記錄安全事件
        /// </summary>
        /// <param name="event">安全事件</param>
        Task LogSecurityEventAsync(SecurityLogEvent @event);

        /// <summary>
        /// 記錄登入事件
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="userAgent">用戶代理</param>
        /// <param name="isSuccess">是否成功</param>
        /// <param name="failureReason">失敗原因</param>
        Task LogLoginEventAsync(int? userId, string ipAddress, string userAgent, bool isSuccess, string? failureReason = null);

        /// <summary>
        /// 記錄權限事件
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="resource">資源</param>
        /// <param name="action">操作</param>
        /// <param name="isAllowed">是否允許</param>
        Task LogPermissionEventAsync(int userId, string resource, string action, bool isAllowed);

        /// <summary>
        /// 記錄資料存取事件
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="dataType">資料類型</param>
        /// <param name="operation">操作</param>
        /// <param name="recordId">記錄ID</param>
        Task LogDataAccessEventAsync(int userId, string dataType, string operation, int? recordId = null);

        /// <summary>
        /// 記錄安全威脅事件
        /// </summary>
        /// <param name="threatType">威脅類型</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="userAgent">用戶代理</param>
        /// <param name="details">詳細資訊</param>
        Task LogSecurityThreatAsync(ThreatType threatType, string ipAddress, string userAgent, string details);

        /// <summary>
        /// 記錄系統事件
        /// </summary>
        /// <param name="eventType">事件類型</param>
        /// <param name="userId">用戶ID</param>
        /// <param name="details">詳細資訊</param>
        Task LogSystemEventAsync(SystemEventType eventType, int? userId, string details);

        /// <summary>
        /// 取得安全日誌
        /// </summary>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <param name="eventType">事件類型</param>
        /// <param name="userId">用戶ID</param>
        /// <returns>安全日誌列表</returns>
        Task<IEnumerable<SecurityLogEvent>> GetSecurityLogsAsync(DateTime? startDate = null, DateTime? endDate = null, SecurityEventType? eventType = null, int? userId = null);

        /// <summary>
        /// 取得安全統計
        /// </summary>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <returns>安全統計</returns>
        Task<SecurityStatistics> GetSecurityStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 檢查異常活動
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="ipAddress">IP地址</param>
        /// <returns>異常活動報告</returns>
        Task<AnomalyReport> CheckAnomalousActivityAsync(int? userId = null, string? ipAddress = null);
    }

    /// <summary>
    /// 安全日誌事件
    /// </summary>
    public class SecurityLogEvent
    {
        public int Id { get; set; }
        public SecurityEventType EventType { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Resource { get; set; }
        public string? Action { get; set; }
        public string? Details { get; set; }
        public string? Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public string? SessionId { get; set; }
        public string? RequestId { get; set; }
    }

    /// <summary>
    /// 安全統計
    /// </summary>
    public class SecurityStatistics
    {
        public int TotalEvents { get; set; }
        public int LoginAttempts { get; set; }
        public int FailedLogins { get; set; }
        public int PermissionDenied { get; set; }
        public int SecurityThreats { get; set; }
        public int DataAccess { get; set; }
        public int SystemEvents { get; set; }
        public Dictionary<string, int> EventsByType { get; set; } = new();
        public Dictionary<string, int> EventsBySeverity { get; set; } = new();
        public Dictionary<string, int> TopIpAddresses { get; set; } = new();
        public Dictionary<string, int> TopUserAgents { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// 異常活動報告
    /// </summary>
    public class AnomalyReport
    {
        public bool HasAnomalies { get; set; }
        public string[] Anomalies { get; set; } = Array.Empty<string>();
        public int FailedLoginAttempts { get; set; }
        public int UnusualIpAddresses { get; set; }
        public int SuspiciousActivities { get; set; }
        public DateTime LastActivity { get; set; }
        public string[] Recommendations { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// 系統事件類型
    /// </summary>
    public enum SystemEventType
    {
        SystemStart,
        SystemStop,
        ConfigurationChange,
        DatabaseBackup,
        DatabaseRestore,
        UserCreated,
        UserDeleted,
        UserModified,
        PermissionChanged,
        SystemMaintenance,
        ErrorOccurred,
        WarningIssued
    }
}