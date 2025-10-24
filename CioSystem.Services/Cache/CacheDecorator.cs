using CioSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 快取裝飾器
    /// 為現有服務添加快取功能
    /// </summary>
    /// <typeparam name="TService">被裝飾的服務類型</typeparam>
    public class CacheDecorator<TService> where TService : class
    {
        private readonly TService _service;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheDecorator<TService>> _logger;
        private readonly CacheConfiguration _config;

        public CacheDecorator(
            TService service,
            ICacheService cacheService,
            ILogger<CacheDecorator<TService>> logger,
            CacheConfiguration config)
        {
            _service = service;
            _cacheService = cacheService;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// 執行帶快取的異步操作
        /// </summary>
        /// <typeparam name="TResult">結果類型</typeparam>
        /// <param name="cacheKey">快取鍵</param>
        /// <param name="operation">操作函數</param>
        /// <param name="expiration">過期時間（可選）</param>
        /// <param name="tags">標籤（可選）</param>
        /// <returns>操作結果</returns>
        public async Task<TResult> ExecuteWithCacheAsync<TResult>(
            string cacheKey,
            Func<Task<TResult>> operation,
            TimeSpan? expiration = null,
            IEnumerable<string>? tags = null) where TResult : class
        {
            try
            {
                // 嘗試從快取取得
                var cachedResult = await _cacheService.GetAsync<TResult>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("快取命中: {CacheKey}", cacheKey);
                    return cachedResult;
                }

                // 執行實際操作
                _logger.LogDebug("快取未命中，執行操作: {CacheKey}", cacheKey);
                var result = await operation();

                if (result != null)
                {
                    // 將結果存入快取
                    var cacheExpiration = expiration ?? TimeSpan.FromMinutes(_config.DefaultExpirationMinutes);
                    if (tags != null)
                    {
                        await _cacheService.SetWithTagsAsync(cacheKey, result, tags, cacheExpiration);
                    }
                    else
                    {
                        await _cacheService.SetAsync(cacheKey, result, cacheExpiration);
                    }

                    _logger.LogDebug("結果已存入快取: {CacheKey}", cacheKey);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行帶快取的操作時發生錯誤: {CacheKey}", cacheKey);
                throw;
            }
        }

        /// <summary>
        /// 執行帶快取的同步操作
        /// </summary>
        /// <typeparam name="TResult">結果類型</typeparam>
        /// <param name="cacheKey">快取鍵</param>
        /// <param name="operation">操作函數</param>
        /// <param name="expiration">過期時間（可選）</param>
        /// <param name="tags">標籤（可選）</param>
        /// <returns>操作結果</returns>
        public async Task<TResult> ExecuteWithCacheAsync<TResult>(
            string cacheKey,
            Func<TResult> operation,
            TimeSpan? expiration = null,
            IEnumerable<string>? tags = null) where TResult : class
        {
            return await ExecuteWithCacheAsync(cacheKey, () => Task.FromResult(operation()), expiration, tags);
        }

        /// <summary>
        /// 使快取失效
        /// </summary>
        /// <param name="cacheKey">快取鍵</param>
        /// <returns>是否成功</returns>
        public async Task<bool> InvalidateCacheAsync(string cacheKey)
        {
            try
            {
                var result = await _cacheService.RemoveAsync(cacheKey);
                _logger.LogDebug("快取失效: {CacheKey}, 成功: {Success}", cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "使快取失效時發生錯誤: {CacheKey}", cacheKey);
                return false;
            }
        }

        /// <summary>
        /// 根據標籤使快取失效
        /// </summary>
        /// <param name="tag">標籤</param>
        /// <returns>失效的項目數量</returns>
        public async Task<int> InvalidateCacheByTagAsync(string tag)
        {
            try
            {
                var result = await _cacheService.RemoveByTagAsync(tag);
                _logger.LogDebug("根據標籤使快取失效: {Tag}, 失效數量: {Count}", tag, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據標籤使快取失效時發生錯誤: {Tag}", tag);
                return 0;
            }
        }

        /// <summary>
        /// 根據多個標籤使快取失效
        /// </summary>
        /// <param name="tags">標籤列表</param>
        /// <returns>失效的項目數量</returns>
        public async Task<int> InvalidateCacheByTagsAsync(IEnumerable<string> tags)
        {
            try
            {
                var result = await _cacheService.RemoveByTagsAsync(tags);
                _logger.LogDebug("根據多個標籤使快取失效: {Tags}, 失效數量: {Count}", string.Join(",", tags), result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據多個標籤使快取失效時發生錯誤: {Tags}", string.Join(",", tags));
                return 0;
            }
        }

        /// <summary>
        /// 取得快取統計資訊
        /// </summary>
        /// <returns>快取統計資訊</returns>
        public async Task<CioSystem.Core.Interfaces.CacheStatistics> GetCacheStatisticsAsync()
        {
            return await _cacheService.GetStatisticsAsync();
        }
    }
}