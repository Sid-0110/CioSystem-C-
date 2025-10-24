namespace CioSystem.API.Services
{
    /// <summary>
    /// API 版本控制服務介面
    /// 提供 API 版本管理和相容性檢查
    /// </summary>
    public interface IApiVersionService
    {
        /// <summary>
        /// 取得當前 API 版本
        /// </summary>
        /// <returns>API 版本資訊</returns>
        ApiVersionInfo GetCurrentVersion();

        /// <summary>
        /// 取得所有支援的 API 版本
        /// </summary>
        /// <returns>版本列表</returns>
        IEnumerable<ApiVersionInfo> GetSupportedVersions();

        /// <summary>
        /// 檢查版本相容性
        /// </summary>
        /// <param name="requestedVersion">請求的版本</param>
        /// <returns>相容性結果</returns>
        ApiCompatibilityResult CheckCompatibility(string requestedVersion);

        /// <summary>
        /// 取得版本變更日誌
        /// </summary>
        /// <param name="fromVersion">起始版本</param>
        /// <param name="toVersion">目標版本</param>
        /// <returns>變更日誌</returns>
        ApiChangeLog GetChangeLog(string fromVersion, string toVersion);
    }

    /// <summary>
    /// API 版本資訊
    /// </summary>
    public class ApiVersionInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public bool IsDeprecated { get; set; }
        public DateTime? DeprecationDate { get; set; }
        public string[] BreakingChanges { get; set; } = Array.Empty<string>();
        public string[] NewFeatures { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// API 相容性結果
    /// </summary>
    public class ApiCompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RecommendedVersion { get; set; } = string.Empty;
        public string[] Warnings { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// API 變更日誌
    /// </summary>
    public class ApiChangeLog
    {
        public string FromVersion { get; set; } = string.Empty;
        public string ToVersion { get; set; } = string.Empty;
        public string[] BreakingChanges { get; set; } = Array.Empty<string>();
        public string[] NewFeatures { get; set; } = Array.Empty<string>();
        public string[] BugFixes { get; set; } = Array.Empty<string>();
        public string[] Improvements { get; set; } = Array.Empty<string>();
        public DateTime GeneratedAt { get; set; }
    }
}