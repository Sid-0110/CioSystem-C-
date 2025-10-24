using System.Diagnostics;

namespace CioSystem.API.Services
{
    /// <summary>
    /// API 效能服務介面
    /// 提供 API 效能監控和優化功能
    /// </summary>
    public interface IApiPerformanceService
    {
        /// <summary>
        /// 記錄 API 請求開始
        /// </summary>
        /// <param name="controllerName">控制器名稱</param>
        /// <param name="actionName">動作名稱</param>
        /// <param name="requestId">請求 ID</param>
        /// <returns>效能追蹤器</returns>
        IDisposable StartRequestTracking(string controllerName, string actionName, string requestId);

        /// <summary>
        /// 記錄 API 請求結束
        /// </summary>
        /// <param name="requestId">請求 ID</param>
        /// <param name="statusCode">狀態碼</param>
        /// <param name="responseSize">響應大小</param>
        Task RecordRequestCompletionAsync(string requestId, int statusCode, long responseSize);

        /// <summary>
        /// 取得 API 效能統計
        /// </summary>
        /// <returns>效能統計資料</returns>
        Task<ApiPerformanceStats> GetPerformanceStatsAsync();

        /// <summary>
        /// 檢查 API 健康狀態
        /// </summary>
        /// <returns>健康狀態</returns>
        Task<ApiHealthStatus> GetHealthStatusAsync();
    }

    /// <summary>
    /// API 效能統計資料
    /// </summary>
    public class ApiPerformanceStats
    {
        public int TotalRequests { get; set; }
        public double AverageResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public int ErrorCount { get; set; }
        public double ErrorRate { get; set; }
        public long TotalResponseSize { get; set; }
        public double AverageResponseSize { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// API 健康狀態
    /// </summary>
    public class ApiHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }
}