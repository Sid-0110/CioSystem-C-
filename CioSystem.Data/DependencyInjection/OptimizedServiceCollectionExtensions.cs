using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CioSystem.Core;
using CioSystem.Data.Repositories;
using System.Data.Common;

namespace CioSystem.Data.DependencyInjection
{
    /// <summary>
    /// 優化的資料庫服務配置
    /// 提供進階的資料庫效能優化設定
    /// </summary>
    public static class OptimizedServiceCollectionExtensions
    {
        /// <summary>
        /// 添加優化的 SQLite 資料庫服務
        /// </summary>
        public static IServiceCollection AddOptimizedSqliteDataLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<CioSystemDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = "Data Source=CioSystem.db";
                }

                // 優化 SQLite 連線字串
                var optimizedConnectionString = OptimizeSqliteConnectionString(connectionString);

                options.UseSqlite(optimizedConnectionString, sqliteOptions =>
                {
                    // 啟用查詢快取
                    sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });

                // 效能優化設定
                ConfigurePerformanceOptions(options);

                // 開發環境設定
                ConfigureDevelopmentOptions(options);

                // 查詢優化設定
                ConfigureQueryOptimization(options);
            });

            // 先註冊基礎服務
            services.AddScoped<DatabasePerformanceService>();
            services.AddScoped<IDatabasePerformanceService, DatabasePerformanceService>();
            services.AddScoped<IQueryOptimizationService, QueryOptimizationService>();

            // 再註冊依賴於基礎服務的服務
            services.AddScoped<IUnitOfWork, OptimizedUnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(OptimizedRepository<>));

            return services;
        }

        /// <summary>
        /// 優化 SQLite 連線字串
        /// </summary>
        private static string OptimizeSqliteConnectionString(string connectionString)
        {
            // 直接返回原始連接字符串，避免使用不支持的參數
            // SQLite 的 Entity Framework 提供者不支援所有連接字符串參數
            return connectionString;
        }

        /// <summary>
        /// 配置效能選項
        /// </summary>
        private static void ConfigurePerformanceOptions(DbContextOptionsBuilder options)
        {
            // 啟用編譯查詢快取
            options.EnableServiceProviderCaching();

            // 啟用查詢編譯快取
            options.EnableSensitiveDataLogging(false);

            // 設定命令逾時（SQLite 不支援）
            // options.CommandTimeout(30);

            // 啟用批次操作（SQLite 不支援）
            // options.UseBatchSize(1000);
        }

        /// <summary>
        /// 配置開發環境選項
        /// </summary>
        private static void ConfigureDevelopmentOptions(DbContextOptionsBuilder options)
        {
#if DEBUG
            // 開發環境專用設定
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.LogTo(Console.WriteLine, LogLevel.Information);

            // 查詢效能分析
            options.EnableServiceProviderCaching();
#endif
        }

        /// <summary>
        /// 配置查詢優化
        /// </summary>
        private static void ConfigureQueryOptimization(DbContextOptionsBuilder options)
        {
            // 忽略警告
            options.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.SensitiveDataLoggingEnabledWarning);
            });

            // 啟用查詢分割（SQLite 不支援）
            // options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }
    }

    /// <summary>
    /// 查詢優化服務介面
    /// </summary>
    public interface IQueryOptimizationService
    {
        Task<IEnumerable<T>> GetOptimizedQueryAsync<T>(IQueryable<T> query) where T : class;
        Task<T> GetOptimizedSingleAsync<T>(IQueryable<T> query) where T : class;
        Task<int> GetOptimizedCountAsync<T>(IQueryable<T> query) where T : class;
        Task<bool> GetOptimizedExistsAsync<T>(IQueryable<T> query) where T : class;
    }

    /// <summary>
    /// 查詢優化服務實現
    /// </summary>
    public class QueryOptimizationService : IQueryOptimizationService
    {
        private readonly CioSystemDbContext _context;
        private readonly ILogger<QueryOptimizationService> _logger;

        public QueryOptimizationService(CioSystemDbContext context, ILogger<QueryOptimizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<T>> GetOptimizedQueryAsync<T>(IQueryable<T> query) where T : class
        {
            try
            {
                // 使用 AsNoTracking 提升只讀查詢效能
                return await query.AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢優化執行時發生錯誤");
                throw;
            }
        }

        public async Task<T> GetOptimizedSingleAsync<T>(IQueryable<T> query) where T : class
        {
            try
            {
                return await query.AsNoTracking().FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "單一查詢優化執行時發生錯誤");
                throw;
            }
        }

        public async Task<int> GetOptimizedCountAsync<T>(IQueryable<T> query) where T : class
        {
            try
            {
                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "計數查詢優化執行時發生錯誤");
                throw;
            }
        }

        public async Task<bool> GetOptimizedExistsAsync<T>(IQueryable<T> query) where T : class
        {
            try
            {
                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "存在性查詢優化執行時發生錯誤");
                throw;
            }
        }
    }

    /// <summary>
    /// 資料庫效能服務介面
    /// </summary>
    public interface IDatabasePerformanceService
    {
        Task AnalyzeQueryPerformanceAsync(string queryName, Func<Task> queryAction);
        Task<DatabasePerformanceMetrics> GetPerformanceMetricsAsync();
        Task OptimizeDatabaseAsync();
        Task VacuumDatabaseAsync();
    }

    /// <summary>
    /// 資料庫效能服務實現
    /// </summary>
    public class DatabasePerformanceService : IDatabasePerformanceService
    {
        private readonly CioSystemDbContext _context;
        private readonly ILogger<DatabasePerformanceService> _logger;

        public DatabasePerformanceService(CioSystemDbContext context, ILogger<DatabasePerformanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AnalyzeQueryPerformanceAsync(string queryName, Func<Task> queryAction)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await queryAction();
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("查詢 {QueryName} 執行時間: {ElapsedMs}ms",
                    queryName, stopwatch.ElapsedMilliseconds);
            }
        }

        public async Task<DatabasePerformanceMetrics> GetPerformanceMetricsAsync()
        {
            try
            {
                await _context.Database.OpenConnectionAsync();

                var metrics = new DatabasePerformanceMetrics();

                // 取得資料庫統計資訊
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "PRAGMA database_list;";
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    metrics.DatabaseCount++;
                }

                // 取得頁面統計
                command.CommandText = "PRAGMA page_count;";
                var pageCount = await command.ExecuteScalarAsync();
                metrics.PageCount = Convert.ToInt32(pageCount);

                command.CommandText = "PRAGMA page_size;";
                var pageSize = await command.ExecuteScalarAsync();
                metrics.PageSize = Convert.ToInt32(pageSize);

                metrics.DatabaseSize = metrics.PageCount * metrics.PageSize;

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得資料庫效能指標時發生錯誤");
                throw;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        public async Task OptimizeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("開始資料庫優化...");

                await _context.Database.OpenConnectionAsync();

                using var command = _context.Database.GetDbConnection().CreateCommand();

                // 分析資料庫
                command.CommandText = "ANALYZE;";
                await command.ExecuteNonQueryAsync();
                _logger.LogInformation("資料庫分析完成");

                // 重建索引
                command.CommandText = "REINDEX;";
                await command.ExecuteNonQueryAsync();
                _logger.LogInformation("索引重建完成");

                _logger.LogInformation("資料庫優化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫優化時發生錯誤");
                throw;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        public async Task VacuumDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("開始資料庫清理...");

                await _context.Database.OpenConnectionAsync();

                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "VACUUM;";
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("資料庫清理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫清理時發生錯誤");
                throw;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }

    /// <summary>
    /// 資料庫效能指標
    /// </summary>
    public class DatabasePerformanceMetrics
    {
        public int DatabaseCount { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public long DatabaseSize { get; set; }
        public DateTime LastAnalyzed { get; set; } = DateTime.UtcNow;
    }
}