using CioSystem.Models;

namespace CioSystem.Services.Logging
{
    /// <summary>
    /// 系統日誌服務接口
    /// </summary>
    public interface ISystemLogService
    {
        /// <summary>
        /// 獲取系統日誌列表
        /// </summary>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="level">日誌等級</param>
        /// <param name="user">用戶</param>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <param name="searchKeyword">搜尋關鍵字</param>
        /// <returns>日誌列表和總數</returns>
        Task<(IEnumerable<LogEntryViewModel> logs, int totalCount)> GetLogsAsync(
            int page = 1,
            int pageSize = 20,
            string? level = null,
            string? user = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? searchKeyword = null);

        /// <summary>
        /// 獲取日誌統計信息
        /// </summary>
        /// <returns>日誌統計</returns>
        Task<LogStatisticsViewModel> GetLogStatisticsAsync();

        /// <summary>
        /// 獲取日誌詳情
        /// </summary>
        /// <param name="logId">日誌ID</param>
        /// <returns>日誌詳情</returns>
        Task<LogEntryViewModel?> GetLogDetailAsync(int logId);

        /// <summary>
        /// 匯出日誌
        /// </summary>
        /// <param name="level">日誌等級</param>
        /// <param name="user">用戶</param>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <param name="format">匯出格式 (csv, json)</param>
        /// <returns>匯出檔案路徑</returns>
        Task<string> ExportLogsAsync(
            string? level = null,
            string? user = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string format = "csv");

        /// <summary>
        /// 清理舊日誌
        /// </summary>
        /// <param name="daysToKeep">保留天數</param>
        /// <returns>清理的日誌數量</returns>
        Task<int> CleanupOldLogsAsync(int daysToKeep = 30);

        /// <summary>
        /// 記錄系統日誌
        /// </summary>
        /// <param name="level">日誌等級</param>
        /// <param name="message">訊息</param>
        /// <param name="user">用戶</param>
        /// <param name="exception">例外</param>
        Task LogAsync(string level, string message, string? user = null, Exception? exception = null);
    }
}