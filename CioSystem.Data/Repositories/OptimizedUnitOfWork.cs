using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CioSystem.Core;
using CioSystem.Data.DependencyInjection;

namespace CioSystem.Data.Repositories
{
    /// <summary>
    /// 優化的 UnitOfWork 實現
    /// 提供進階的事務管理和效能優化
    /// </summary>
    public class OptimizedUnitOfWork : IUnitOfWork
    {
        private readonly CioSystemDbContext _context;
        private readonly ILogger<OptimizedUnitOfWork> _logger;
        private readonly IDatabasePerformanceService _performanceService;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, object> _repositories;
        private bool _disposed = false;

        public OptimizedUnitOfWork(
            CioSystemDbContext context,
            ILogger<OptimizedUnitOfWork> logger,
            DatabasePerformanceService performanceService,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _performanceService = performanceService;
            _serviceProvider = serviceProvider;
            _repositories = new Dictionary<Type, object>();
        }

        /// <summary>
        /// 取得 Repository（優化版本）
        /// </summary>
        public IRepository<T> GetRepository<T>() where T : BaseEntity
        {
            var type = typeof(T);

            if (!_repositories.ContainsKey(type))
            {
                // 創建特定類型的 logger
                var repositoryLogger = _serviceProvider.GetRequiredService<ILogger<OptimizedRepository<T>>>();
                var repository = new OptimizedRepository<T>(_context, repositoryLogger);
                _repositories[type] = repository;

                _logger.LogDebug("創建新的 Repository: {Type}", type.Name);
            }

            return (IRepository<T>)_repositories[type];
        }

        /// <summary>
        /// 儲存變更（優化版本）
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            try
            {
                _logger.LogDebug("開始儲存變更");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await _context.SaveChangesAsync();
                stopwatch.Stop();

                _logger.LogInformation("儲存變更完成，影響記錄數: {Count}, 耗時: {ElapsedMs}ms",
                    result, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存變更時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 儲存變更（同步版本）
        /// </summary>
        public int SaveChanges()
        {
            try
            {
                _logger.LogDebug("開始儲存變更（同步）");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = _context.SaveChanges();
                stopwatch.Stop();

                _logger.LogInformation("儲存變更完成（同步），影響記錄數: {Count}, 耗時: {ElapsedMs}ms",
                    result, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存變更時發生錯誤（同步）");
                throw;
            }
        }

        /// <summary>
        /// 執行事務（優化版本）
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogDebug("開始執行事務");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await operation();
                await transaction.CommitAsync();
                stopwatch.Stop();

                _logger.LogInformation("事務執行完成，耗時: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "事務執行時發生錯誤，開始回滾");
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 執行事務（無回傳值）
        /// </summary>
        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogDebug("開始執行事務（無回傳值）");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await operation();
                await transaction.CommitAsync();
                stopwatch.Stop();

                _logger.LogInformation("事務執行完成（無回傳值），耗時: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "事務執行時發生錯誤（無回傳值），開始回滾");
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 批量操作（優化版本）
        /// </summary>
        public async Task<int> BulkInsertAsync<T>(IEnumerable<T> entities) where T : class
        {
            try
            {
                _logger.LogDebug("開始批量插入，數量: {Count}", entities.Count());

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await _context.Set<T>().AddRangeAsync(entities);
                var result = await _context.SaveChangesAsync();
                stopwatch.Stop();

                _logger.LogInformation("批量插入完成，影響記錄數: {Count}, 耗時: {ElapsedMs}ms",
                    result, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量插入時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 批量更新（優化版本）
        /// </summary>
        public async Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class
        {
            try
            {
                _logger.LogDebug("開始批量更新，數量: {Count}", entities.Count());

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                _context.Set<T>().UpdateRange(entities);
                var result = await _context.SaveChangesAsync();
                stopwatch.Stop();

                _logger.LogInformation("批量更新完成，影響記錄數: {Count}, 耗時: {ElapsedMs}ms",
                    result, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 批量刪除（優化版本）
        /// </summary>
        public async Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities) where T : class
        {
            try
            {
                _logger.LogDebug("開始批量刪除，數量: {Count}", entities.Count());

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                _context.Set<T>().RemoveRange(entities);
                var result = await _context.SaveChangesAsync();
                stopwatch.Stop();

                _logger.LogInformation("批量刪除完成，影響記錄數: {Count}, 耗時: {ElapsedMs}ms",
                    result, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量刪除時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 執行原生 SQL（優化版本）
        /// </summary>
        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            try
            {
                _logger.LogDebug("執行原生 SQL: {Sql}", sql);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await _context.Database.ExecuteSqlRawAsync(sql, parameters);
                stopwatch.Stop();

                _logger.LogInformation("原生 SQL 執行完成，影響記錄數: {Count}, 耗時: {ElapsedMs}ms",
                    result, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行原生 SQL 時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 執行查詢（優化版本）
        /// </summary>
        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, params object[] parameters) where T : class
        {
            try
            {
                _logger.LogDebug("執行查詢 SQL: {Sql}", sql);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await _context.Set<T>().FromSqlRaw(sql, parameters).AsNoTracking().ToListAsync();
                stopwatch.Stop();

                _logger.LogInformation("查詢執行完成，結果數量: {Count}, 耗時: {ElapsedMs}ms",
                    result.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行查詢時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 優化資料庫
        /// </summary>
        public async Task OptimizeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("開始優化資料庫");
                await _performanceService.OptimizeDatabaseAsync();
                _logger.LogInformation("資料庫優化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫優化時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 清理資料庫
        /// </summary>
        public async Task VacuumDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("開始清理資料庫");
                await _performanceService.VacuumDatabaseAsync();
                _logger.LogInformation("資料庫清理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫清理時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 取得效能指標
        /// </summary>
        public async Task<DatabasePerformanceMetrics> GetPerformanceMetricsAsync()
        {
            try
            {
                return await _performanceService.GetPerformanceMetricsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得效能指標時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 開始資料庫交易
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            try
            {
                _logger.LogDebug("開始資料庫交易");
                await _context.Database.BeginTransactionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "開始資料庫交易時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 提交資料庫交易
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            try
            {
                _logger.LogDebug("提交資料庫交易");
                await _context.Database.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交資料庫交易時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 回滾資料庫交易
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            try
            {
                _logger.LogDebug("回滾資料庫交易");
                await _context.Database.RollbackTransactionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回滾資料庫交易時發生錯誤");
                throw;
            }
        }


        /// <summary>
        /// 釋放資源（受保護版本）
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context?.Dispose();
                _repositories?.Clear();
                _disposed = true;

                _logger.LogDebug("UnitOfWork 資源已釋放");
            }
        }
    }
}