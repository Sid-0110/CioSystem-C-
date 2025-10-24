using CioSystem.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CioSystem.API.Controllers
{
    /// <summary>
    /// API 管理控制器
    /// 提供 API 效能監控、快取管理和版本控制功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ApiManagementController : ControllerBase
    {
        private readonly IApiPerformanceService _performanceService;
        private readonly IApiCacheService _cacheService;
        private readonly IApiVersionService _versionService;
        private readonly ILogger<ApiManagementController> _logger;

        public ApiManagementController(
            IApiPerformanceService performanceService,
            IApiCacheService cacheService,
            IApiVersionService versionService,
            ILogger<ApiManagementController> logger)
        {
            _performanceService = performanceService;
            _cacheService = cacheService;
            _versionService = versionService;
            _logger = logger;
        }

        /// <summary>
        /// 取得 API 效能統計
        /// </summary>
        /// <returns>效能統計資料</returns>
        [HttpGet("performance")]
        public async Task<ActionResult<ApiPerformanceStats>> GetPerformanceStats()
        {
            try
            {
                var stats = await _performanceService.GetPerformanceStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得 API 效能統計時發生錯誤");
                return StatusCode(500, "取得效能統計時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得 API 健康狀態
        /// </summary>
        /// <returns>健康狀態</returns>
        [HttpGet("health")]
        public async Task<ActionResult<ApiHealthStatus>> GetHealthStatus()
        {
            try
            {
                var health = await _performanceService.GetHealthStatusAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得 API 健康狀態時發生錯誤");
                return StatusCode(500, "取得健康狀態時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得快取統計
        /// </summary>
        /// <returns>快取統計資料</returns>
        [HttpGet("cache/stats")]
        public async Task<ActionResult<ApiCacheStats>> GetCacheStats()
        {
            try
            {
                var stats = await _cacheService.GetCacheStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得快取統計時發生錯誤");
                return StatusCode(500, "取得快取統計時發生內部錯誤");
            }
        }

        /// <summary>
        /// 清除所有快取
        /// </summary>
        /// <returns>操作結果</returns>
        [HttpPost("cache/clear")]
        public async Task<ActionResult> ClearAllCache()
        {
            try
            {
                await _cacheService.ClearAllCacheAsync();
                _logger.LogInformation("API 快取已清除");
                return Ok(new { message = "快取已成功清除" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除快取時發生錯誤");
                return StatusCode(500, "清除快取時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得 API 版本資訊
        /// </summary>
        /// <returns>版本資訊</returns>
        [HttpGet("version")]
        public ActionResult<ApiVersionInfo> GetVersion()
        {
            try
            {
                var version = _versionService.GetCurrentVersion();
                return Ok(version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得 API 版本資訊時發生錯誤");
                return StatusCode(500, "取得版本資訊時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得所有支援的 API 版本
        /// </summary>
        /// <returns>版本列表</returns>
        [HttpGet("versions")]
        public ActionResult<IEnumerable<ApiVersionInfo>> GetSupportedVersions()
        {
            try
            {
                var versions = _versionService.GetSupportedVersions();
                return Ok(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得支援的 API 版本時發生錯誤");
                return StatusCode(500, "取得版本列表時發生內部錯誤");
            }
        }

        /// <summary>
        /// 檢查版本相容性
        /// </summary>
        /// <param name="version">請求的版本</param>
        /// <returns>相容性結果</returns>
        [HttpGet("compatibility/{version}")]
        public ActionResult<ApiCompatibilityResult> CheckCompatibility(string version)
        {
            try
            {
                var result = _versionService.CheckCompatibility(version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查版本相容性時發生錯誤: {Version}", version);
                return StatusCode(500, "檢查版本相容性時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得版本變更日誌
        /// </summary>
        /// <param name="fromVersion">起始版本</param>
        /// <param name="toVersion">目標版本</param>
        /// <returns>變更日誌</returns>
        [HttpGet("changelog")]
        public ActionResult<ApiChangeLog> GetChangeLog([FromQuery] string fromVersion, [FromQuery] string toVersion)
        {
            try
            {
                var changeLog = _versionService.GetChangeLog(fromVersion, toVersion);
                return Ok(changeLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得版本變更日誌時發生錯誤: {FromVersion} -> {ToVersion}", fromVersion, toVersion);
                return StatusCode(500, "取得變更日誌時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得 API 文檔
        /// </summary>
        /// <returns>API 文檔</returns>
        [HttpGet("docs")]
        public ActionResult<object> GetApiDocumentation()
        {
            try
            {
                var documentation = new
                {
                    Title = "CioSystem API",
                    Version = _versionService.GetCurrentVersion().Version,
                    Description = "CioSystem 庫存管理系統 API",
                    BaseUrl = $"{Request.Scheme}://{Request.Host}",
                    Endpoints = new[]
                    {
                        new { Path = "/api/products", Method = "GET", Description = "取得產品列表" },
                        new { Path = "/api/products/{id}", Method = "GET", Description = "取得特定產品" },
                        new { Path = "/api/inventory", Method = "GET", Description = "取得庫存列表" },
                        new { Path = "/api/sales", Method = "GET", Description = "取得銷售列表" },
                        new { Path = "/api/purchases", Method = "GET", Description = "取得進貨列表" },
                        new { Path = "/api/management/performance", Method = "GET", Description = "取得效能統計" },
                        new { Path = "/api/management/health", Method = "GET", Description = "取得健康狀態" },
                        new { Path = "/api/management/cache/stats", Method = "GET", Description = "取得快取統計" }
                    },
                    Features = new[]
                    {
                        "自動快取",
                        "效能監控",
                        "版本控制",
                        "健康檢查",
                        "錯誤處理"
                    }
                };

                return Ok(documentation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得 API 文檔時發生錯誤");
                return StatusCode(500, "取得 API 文檔時發生內部錯誤");
            }
        }
    }
}