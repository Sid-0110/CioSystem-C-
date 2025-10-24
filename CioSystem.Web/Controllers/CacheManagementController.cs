using CioSystem.Services.Cache;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CioSystem.Web.Controllers
{
    /// <summary>
    /// 快取管理控制器
    /// 提供快取管理、監控和統計功能
    /// </summary>
    public class CacheManagementController : Controller
    {
        private readonly IMultiLayerCacheService _multiLayerCache;
        private readonly ICacheWarmupService _cacheWarmupService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly ILogger<CacheManagementController> _logger;

        public CacheManagementController(
            IMultiLayerCacheService multiLayerCache,
            ICacheWarmupService cacheWarmupService,
            ICacheInvalidationService cacheInvalidationService,
            ILogger<CacheManagementController> logger)
        {
            _multiLayerCache = multiLayerCache;
            _cacheWarmupService = cacheWarmupService;
            _cacheInvalidationService = cacheInvalidationService;
            _logger = logger;
        }

        /// <summary>
        /// 快取管理主頁
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var statistics = await _multiLayerCache.GetStatisticsAsync();
                var warmupStatus = await _cacheWarmupService.GetWarmupStatusAsync();
                var invalidationStats = await _cacheInvalidationService.GetInvalidationStatisticsAsync();

                ViewBag.CacheStatistics = statistics;
                ViewBag.WarmupStatus = warmupStatus;
                ViewBag.InvalidationStatistics = invalidationStats;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得快取管理頁面時發生錯誤");
                TempData["ErrorMessage"] = "載入快取管理頁面時發生錯誤。";
                return View();
            }
        }

        /// <summary>
        /// 清除所有快取
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ClearAllCache()
        {
            try
            {
                await _multiLayerCache.ClearAllAsync();
                TempData["SuccessMessage"] = "所有快取已清除。";
                _logger.LogInformation("管理員清除了所有快取");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除所有快取時發生錯誤");
                TempData["ErrorMessage"] = "清除快取時發生錯誤。";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 根據標籤清除快取
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ClearCacheByTag(string tag)
        {
            try
            {
                if (string.IsNullOrEmpty(tag))
                {
                    TempData["ErrorMessage"] = "標籤不能為空。";
                    return RedirectToAction(nameof(Index));
                }

                await _multiLayerCache.RemoveByTagAsync(tag);
                TempData["SuccessMessage"] = $"標籤 '{tag}' 的快取已清除。";
                _logger.LogInformation("管理員清除了標籤 '{Tag}' 的快取", tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據標籤清除快取時發生錯誤: {Tag}", tag);
                TempData["ErrorMessage"] = "清除快取時發生錯誤。";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 預熱快取
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> WarmupCache()
        {
            try
            {
                await _cacheWarmupService.WarmupAllAsync();
                TempData["SuccessMessage"] = "快取預熱已啟動。";
                _logger.LogInformation("管理員啟動了快取預熱");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "預熱快取時發生錯誤");
                TempData["ErrorMessage"] = "預熱快取時發生錯誤。";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 重新預熱指定類型的快取
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RewarmupCache(string cacheType)
        {
            try
            {
                if (string.IsNullOrEmpty(cacheType))
                {
                    TempData["ErrorMessage"] = "快取類型不能為空。";
                    return RedirectToAction(nameof(Index));
                }

                await _cacheWarmupService.RewarmupAsync(cacheType);
                TempData["SuccessMessage"] = $"快取類型 '{cacheType}' 的重新預熱已啟動。";
                _logger.LogInformation("管理員重新預熱了快取類型 '{CacheType}'", cacheType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新預熱快取時發生錯誤: {CacheType}", cacheType);
                TempData["ErrorMessage"] = "重新預熱快取時發生錯誤。";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 取得快取統計 API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCacheStatistics()
        {
            try
            {
                var statistics = await _multiLayerCache.GetStatisticsAsync();
                return Json(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得快取統計時發生錯誤");
                return Json(new { error = "取得快取統計時發生錯誤" });
            }
        }

        /// <summary>
        /// 取得預熱狀態 API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWarmupStatus()
        {
            try
            {
                var status = await _cacheWarmupService.GetWarmupStatusAsync();
                return Json(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得預熱狀態時發生錯誤");
                return Json(new { error = "取得預熱狀態時發生錯誤" });
            }
        }

        /// <summary>
        /// 取得失效統計 API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetInvalidationStatistics()
        {
            try
            {
                var statistics = await _cacheInvalidationService.GetInvalidationStatisticsAsync();
                return Json(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得失效統計時發生錯誤");
                return Json(new { error = "取得失效統計時發生錯誤" });
            }
        }

        /// <summary>
        /// 檢查快取是否存在
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckCacheExists(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    return Json(new { exists = false, error = "鍵不能為空" });
                }

                var exists = await _multiLayerCache.ExistsAsync(key);
                return Json(new { exists = exists, key = key });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查快取存在性時發生錯誤: {Key}", key);
                return Json(new { exists = false, error = "檢查快取存在性時發生錯誤" });
            }
        }
    }
}