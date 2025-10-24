using System.Reflection;

namespace CioSystem.API.Services
{
    /// <summary>
    /// API 版本控制服務實現
    /// 提供 API 版本管理和相容性檢查
    /// </summary>
    public class ApiVersionService : IApiVersionService
    {
        private readonly ILogger<ApiVersionService> _logger;
        private readonly List<ApiVersionInfo> _supportedVersions;

        public ApiVersionService(ILogger<ApiVersionService> logger)
        {
            _logger = logger;
            _supportedVersions = InitializeVersions();
        }

        public ApiVersionInfo GetCurrentVersion()
        {
            return _supportedVersions.FirstOrDefault(v => v.Version == "2.0")
                   ?? _supportedVersions.OrderByDescending(v => v.ReleaseDate).First();
        }

        public IEnumerable<ApiVersionInfo> GetSupportedVersions()
        {
            return _supportedVersions.OrderByDescending(v => v.ReleaseDate);
        }

        public ApiCompatibilityResult CheckCompatibility(string requestedVersion)
        {
            var currentVersion = GetCurrentVersion();
            var requestedVersionInfo = _supportedVersions.FirstOrDefault(v => v.Version == requestedVersion);

            if (requestedVersionInfo == null)
            {
                return new ApiCompatibilityResult
                {
                    IsCompatible = false,
                    Status = "Unsupported",
                    Message = $"不支援的 API 版本: {requestedVersion}",
                    RecommendedVersion = currentVersion.Version,
                    Warnings = new[] { $"請使用支援的版本: {string.Join(", ", _supportedVersions.Select(v => v.Version))}" }
                };
            }

            if (requestedVersionInfo.IsDeprecated)
            {
                return new ApiCompatibilityResult
                {
                    IsCompatible = true,
                    Status = "Deprecated",
                    Message = $"API 版本 {requestedVersion} 已棄用",
                    RecommendedVersion = currentVersion.Version,
                    Warnings = new[] { $"建議升級到最新版本: {currentVersion.Version}" }
                };
            }

            return new ApiCompatibilityResult
            {
                IsCompatible = true,
                Status = "Supported",
                Message = "版本相容",
                RecommendedVersion = currentVersion.Version
            };
        }

        public ApiChangeLog GetChangeLog(string fromVersion, string toVersion)
        {
            var fromVersionInfo = _supportedVersions.FirstOrDefault(v => v.Version == fromVersion);
            var toVersionInfo = _supportedVersions.FirstOrDefault(v => v.Version == toVersion);

            if (fromVersionInfo == null || toVersionInfo == null)
            {
                return new ApiChangeLog
                {
                    FromVersion = fromVersion,
                    ToVersion = toVersion,
                    GeneratedAt = DateTime.UtcNow
                };
            }

            return new ApiChangeLog
            {
                FromVersion = fromVersion,
                ToVersion = toVersion,
                BreakingChanges = toVersionInfo.BreakingChanges,
                NewFeatures = toVersionInfo.NewFeatures,
                BugFixes = new[] { "修復已知問題", "改善效能" },
                Improvements = new[] { "優化響應時間", "改善錯誤處理" },
                GeneratedAt = DateTime.UtcNow
            };
        }

        private List<ApiVersionInfo> InitializeVersions()
        {
            return new List<ApiVersionInfo>
            {
                new ApiVersionInfo
                {
                    Version = "1.0",
                    Name = "初始版本",
                    Description = "CioSystem API 初始版本",
                    ReleaseDate = new DateTime(2024, 1, 1),
                    IsDeprecated = true,
                    DeprecationDate = new DateTime(2024, 6, 1),
                    BreakingChanges = Array.Empty<string>(),
                    NewFeatures = new[] { "基本 CRUD 操作", "產品管理", "庫存管理" }
                },
                new ApiVersionInfo
                {
                    Version = "1.1",
                    Name = "效能優化版本",
                    Description = "效能優化和錯誤修復",
                    ReleaseDate = new DateTime(2024, 3, 1),
                    IsDeprecated = false,
                    BreakingChanges = Array.Empty<string>(),
                    NewFeatures = new[] { "快取支援", "分頁優化", "效能監控" }
                },
                new ApiVersionInfo
                {
                    Version = "2.0",
                    Name = "現代化版本",
                    Description = "現代化 API 設計和進階功能",
                    ReleaseDate = new DateTime(2024, 6, 1),
                    IsDeprecated = false,
                    BreakingChanges = new[] { "移除舊版端點", "變更響應格式" },
                    NewFeatures = new[] { "多層快取", "進階查詢", "即時更新", "API 版本控制" }
                }
            };
        }
    }
}