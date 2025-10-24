using CioSystem.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 快取失效服務實現
    /// 負責智慧快取失效和更新策略
    /// </summary>
    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly IMultiLayerCacheService _multiLayerCache;
        private readonly ILogger<CacheInvalidationService> _logger;
        private readonly ConcurrentDictionary<string, long> _invalidationCounts;
        private readonly ConcurrentDictionary<string, DateTime> _lastInvalidationTimes;
        private readonly ConcurrentDictionary<string, TimeSpan> _invalidationTimes;

        public CacheInvalidationService(
            IMultiLayerCacheService multiLayerCache,
            ILogger<CacheInvalidationService> logger)
        {
            _multiLayerCache = multiLayerCache;
            _logger = logger;
            _invalidationCounts = new ConcurrentDictionary<string, long>();
            _lastInvalidationTimes = new ConcurrentDictionary<string, DateTime>();
            _invalidationTimes = new ConcurrentDictionary<string, TimeSpan>();
        }

        /// <summary>
        /// 根據實體類型失效相關快取
        /// </summary>
        public async Task InvalidateByEntityTypeAsync<T>(T entity, string operation) where T : class
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var entityType = typeof(T).Name;

                _logger.LogDebug("開始根據實體類型失效快取: {EntityType}, 操作: {Operation}", entityType, operation);

                // 根據實體類型決定失效策略
                switch (entityType)
                {
                    case nameof(Product):
                        await InvalidateProductRelatedCache(entity as Product, operation);
                        break;
                    case nameof(Inventory):
                        await InvalidateInventoryRelatedCache(entity as Inventory, operation);
                        break;
                    case nameof(Sale):
                        await InvalidateSaleRelatedCache(entity as Sale, operation);
                        break;
                    case nameof(Purchase):
                        await InvalidatePurchaseRelatedCache(entity as Purchase, operation);
                        break;
                    default:
                        await InvalidateGenericEntityCache(entity, operation);
                        break;
                }

                stopwatch.Stop();
                RecordInvalidation("Entity", stopwatch.Elapsed);

                _logger.LogDebug("實體類型快取失效完成: {EntityType}, 耗時: {ElapsedMs}ms",
                    entityType, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據實體類型失效快取失敗: {EntityType}", typeof(T).Name);
            }
        }

        /// <summary>
        /// 根據標籤失效快取
        /// </summary>
        public async Task InvalidateByTagAsync(string tag)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                _logger.LogDebug("開始根據標籤失效快取: {Tag}", tag);

                await _multiLayerCache.RemoveByTagAsync(tag);

                stopwatch.Stop();
                RecordInvalidation("Tag", stopwatch.Elapsed);

                _logger.LogDebug("標籤快取失效完成: {Tag}, 耗時: {ElapsedMs}ms",
                    tag, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據標籤失效快取失敗: {Tag}", tag);
            }
        }

        /// <summary>
        /// 根據模式失效快取
        /// </summary>
        public async Task InvalidateByPatternAsync(string pattern)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                _logger.LogDebug("開始根據模式失效快取: {Pattern}", pattern);

                // 這裡可以實現更複雜的模式匹配邏輯
                await _multiLayerCache.RemoveAsync(pattern);

                stopwatch.Stop();
                RecordInvalidation("Pattern", stopwatch.Elapsed);

                _logger.LogDebug("模式快取失效完成: {Pattern}, 耗時: {ElapsedMs}ms",
                    pattern, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據模式失效快取失敗: {Pattern}", pattern);
            }
        }

        /// <summary>
        /// 智慧失效（根據資料變更自動判斷）
        /// </summary>
        public async Task SmartInvalidateAsync<T>(T oldEntity, T newEntity, string operation) where T : class
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                _logger.LogDebug("開始智慧失效: {EntityType}, 操作: {Operation}", typeof(T).Name, operation);

                // 比較實體變更，決定失效範圍
                var hasSignificantChanges = HasSignificantChanges(oldEntity, newEntity);

                if (hasSignificantChanges)
                {
                    // 重大變更，失效所有相關快取
                    await InvalidateByEntityTypeAsync(newEntity, operation);
                }
                else
                {
                    // 輕微變更，只失效特定快取
                    await InvalidateSpecificCache(newEntity, operation);
                }

                stopwatch.Stop();
                RecordInvalidation("Smart", stopwatch.Elapsed);

                _logger.LogDebug("智慧失效完成: {EntityType}, 耗時: {ElapsedMs}ms",
                    typeof(T).Name, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "智慧失效失敗: {EntityType}", typeof(T).Name);
            }
        }

        /// <summary>
        /// 延遲失效（在指定時間後失效）
        /// </summary>
        public async Task DelayedInvalidateAsync(string key, TimeSpan delay)
        {
            try
            {
                _logger.LogDebug("設定延遲失效: {Key}, 延遲: {Delay}", key, delay);

                await Task.Delay(delay);
                await _multiLayerCache.RemoveAsync(key);

                RecordInvalidation("Delayed", delay);
                _logger.LogDebug("延遲失效完成: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "延遲失效失敗: {Key}", key);
            }
        }

        /// <summary>
        /// 條件失效（根據條件判斷是否失效）
        /// </summary>
        public async Task ConditionalInvalidateAsync<T>(T entity, Func<T, bool> condition, string operation) where T : class
        {
            try
            {
                _logger.LogDebug("開始條件失效: {EntityType}, 操作: {Operation}", typeof(T).Name, operation);

                if (condition(entity))
                {
                    await InvalidateByEntityTypeAsync(entity, operation);
                    RecordInvalidation("Conditional", TimeSpan.Zero);
                    _logger.LogDebug("條件失效執行: {EntityType}", typeof(T).Name);
                }
                else
                {
                    _logger.LogDebug("條件不滿足，跳過失效: {EntityType}", typeof(T).Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "條件失效失敗: {EntityType}", typeof(T).Name);
            }
        }

        /// <summary>
        /// 批量失效
        /// </summary>
        public async Task BatchInvalidateAsync(string[] keys)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                _logger.LogDebug("開始批量失效: {Count} 個鍵", keys.Length);

                var tasks = keys.Select(key => _multiLayerCache.RemoveAsync(key));
                await Task.WhenAll(tasks);

                stopwatch.Stop();
                RecordInvalidation("Batch", stopwatch.Elapsed);

                _logger.LogDebug("批量失效完成: {Count} 個鍵, 耗時: {ElapsedMs}ms",
                    keys.Length, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量失效失敗");
            }
        }

        /// <summary>
        /// 失效統計
        /// </summary>
        public async Task<InvalidationStatistics> GetInvalidationStatisticsAsync()
        {
            try
            {
                var stats = new InvalidationStatistics
                {
                    TotalInvalidations = _invalidationCounts.Values.Sum(),
                    EntityInvalidations = _invalidationCounts.GetValueOrDefault("Entity", 0),
                    TagInvalidations = _invalidationCounts.GetValueOrDefault("Tag", 0),
                    PatternInvalidations = _invalidationCounts.GetValueOrDefault("Pattern", 0),
                    SmartInvalidations = _invalidationCounts.GetValueOrDefault("Smart", 0),
                    DelayedInvalidations = _invalidationCounts.GetValueOrDefault("Delayed", 0),
                    ConditionalInvalidations = _invalidationCounts.GetValueOrDefault("Conditional", 0),
                    BatchInvalidations = _invalidationCounts.GetValueOrDefault("Batch", 0),
                    LastInvalidation = _lastInvalidationTimes.Values.DefaultIfEmpty(DateTime.MinValue).Max(),
                    AverageInvalidationTime = _invalidationTimes.Values.Any()
                        ? TimeSpan.FromTicks((long)_invalidationTimes.Values.Average(t => t.Ticks))
                        : TimeSpan.Zero
                };

                return await Task.FromResult(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得失效統計失敗");
                return new InvalidationStatistics();
            }
        }

        /// <summary>
        /// 失效產品相關快取
        /// </summary>
        private async Task InvalidateProductRelatedCache(Product product, string operation)
        {
            if (product == null) return;

            var keysToInvalidate = new[]
            {
                "all_products",
                "active_products",
                "product_categories",
                $"product_{product.Id}",
                $"product_sku_{product.SKU}"
            };

            await BatchInvalidateAsync(keysToInvalidate);
            await InvalidateByTagAsync("products");
        }

        /// <summary>
        /// 失效庫存相關快取
        /// </summary>
        private async Task InvalidateInventoryRelatedCache(Inventory inventory, string operation)
        {
            if (inventory == null) return;

            var keysToInvalidate = new[]
            {
                "inventory_stats",
                "low_stock_items",
                $"inventory_{inventory.Id}",
                $"inventory_product_{inventory.ProductId}"
            };

            await BatchInvalidateAsync(keysToInvalidate);
            await InvalidateByTagAsync("inventory");
        }

        /// <summary>
        /// 失效銷售相關快取
        /// </summary>
        private async Task InvalidateSaleRelatedCache(Sale sale, string operation)
        {
            if (sale == null) return;

            var keysToInvalidate = new[]
            {
                "sales_stats",
                "dashboard_stats",
                $"sale_{sale.Id}",
                $"sales_product_{sale.ProductId}"
            };

            await BatchInvalidateAsync(keysToInvalidate);
            await InvalidateByTagAsync("statistics");
        }

        /// <summary>
        /// 失效進貨相關快取
        /// </summary>
        private async Task InvalidatePurchaseRelatedCache(Purchase purchase, string operation)
        {
            if (purchase == null) return;

            var keysToInvalidate = new[]
            {
                "purchases_stats",
                "dashboard_stats",
                $"purchase_{purchase.Id}",
                $"purchases_product_{purchase.ProductId}"
            };

            await BatchInvalidateAsync(keysToInvalidate);
            await InvalidateByTagAsync("statistics");
        }

        /// <summary>
        /// 失效通用實體快取
        /// </summary>
        private async Task InvalidateGenericEntityCache<T>(T entity, string operation) where T : class
        {
            var entityType = typeof(T).Name.ToLower();
            await InvalidateByTagAsync(entityType);
        }

        /// <summary>
        /// 失效特定快取
        /// </summary>
        private async Task InvalidateSpecificCache<T>(T entity, string operation) where T : class
        {
            // 根據操作類型決定失效範圍
            switch (operation.ToLower())
            {
                case "create":
                case "delete":
                    await InvalidateByEntityTypeAsync(entity, operation);
                    break;
                case "update":
                    // 更新操作可能只需要失效部分快取
                    break;
            }
        }

        /// <summary>
        /// 檢查是否有重大變更
        /// </summary>
        private bool HasSignificantChanges<T>(T oldEntity, T newEntity) where T : class
        {
            // 這裡可以實現更複雜的變更檢測邏輯
            // 暫時返回 true，表示總是進行完整失效
            return true;
        }

        /// <summary>
        /// 記錄失效統計
        /// </summary>
        private void RecordInvalidation(string type, TimeSpan elapsed)
        {
            _invalidationCounts.AddOrUpdate(type, 1, (k, v) => v + 1);
            _lastInvalidationTimes[type] = DateTime.UtcNow;
            _invalidationTimes[type] = elapsed;
        }
    }
}