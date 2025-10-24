using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace CioSystem.Services.Monitoring
{
    /// <summary>
    /// 資料庫查詢分析器
    /// 用於分析和優化資料庫查詢效能
    /// </summary>
    public interface IDatabaseQueryAnalyzer
    {
        Task<QueryAnalysisResult> AnalyzeQueryAsync<T>(IQueryable<T> query, string queryName);
        Task<DatabaseStatistics> GetDatabaseStatisticsAsync();
        Task<List<SlowQueryInfo>> GetSlowQueriesAsync(int thresholdMs = 1000);
        Task OptimizeSlowQueriesAsync();
    }

    /// <summary>
    /// 資料庫查詢分析器實現
    /// </summary>
    public class DatabaseQueryAnalyzer : IDatabaseQueryAnalyzer
    {
        private readonly CioSystem.Data.CioSystemDbContext _context;
        private readonly ILogger<DatabaseQueryAnalyzer> _logger;
        private readonly List<QueryExecutionInfo> _queryHistory;
        private readonly object _lockObject = new object();

        public DatabaseQueryAnalyzer(
            CioSystem.Data.CioSystemDbContext context,
            ILogger<DatabaseQueryAnalyzer> logger)
        {
            _context = context;
            _logger = logger;
            _queryHistory = new List<QueryExecutionInfo>();
        }

        /// <summary>
        /// 分析查詢效能
        /// </summary>
        public async Task<QueryAnalysisResult> AnalyzeQueryAsync<T>(IQueryable<T> query, string queryName)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new QueryAnalysisResult
            {
                QueryName = queryName,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // 執行查詢
                var data = await query.ToListAsync();
                stopwatch.Stop();

                result.ExecutionTime = stopwatch.Elapsed;
                result.RecordCount = data.Count;
                result.IsSuccessful = true;

                // 記錄查詢資訊
                var queryInfo = new QueryExecutionInfo
                {
                    QueryName = queryName,
                    ExecutionTime = result.ExecutionTime,
                    RecordCount = result.RecordCount,
                    Timestamp = DateTime.UtcNow
                };

                lock (_lockObject)
                {
                    _queryHistory.Add(queryInfo);

                    // 保持最近1000筆記錄
                    if (_queryHistory.Count > 1000)
                    {
                        _queryHistory.RemoveAt(0);
                    }
                }

                _logger.LogInformation("查詢分析完成: {QueryName}, 執行時間: {ExecutionTime}ms, 記錄數: {RecordCount}",
                    queryName, result.ExecutionTime.TotalMilliseconds, result.RecordCount);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;

                _logger.LogError(ex, "查詢分析失敗: {QueryName}", queryName);
                return result;
            }
        }

        /// <summary>
        /// 取得資料庫統計資訊
        /// </summary>
        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync()
        {
            try
            {
                await _context.Database.OpenConnectionAsync();

                var statistics = new DatabaseStatistics();

                using var command = _context.Database.GetDbConnection().CreateCommand();

                // 取得資料庫大小
                command.CommandText = "PRAGMA page_count;";
                var pageCount = await command.ExecuteScalarAsync();
                statistics.PageCount = Convert.ToInt32(pageCount);

                command.CommandText = "PRAGMA page_size;";
                var pageSize = await command.ExecuteScalarAsync();
                statistics.PageSize = Convert.ToInt32(pageSize);

                statistics.DatabaseSize = statistics.PageCount * statistics.PageSize;

                // 取得表統計
                command.CommandText = @"
                    SELECT name, 
                           (SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=sm.name) as table_count
                    FROM sqlite_master sm 
                    WHERE type='table' AND name NOT LIKE 'sqlite_%'";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    var tableCount = reader.GetInt32(1);
                    statistics.TableStatistics[tableName] = tableCount;
                }

                // 取得查詢統計
                lock (_lockObject)
                {
                    statistics.TotalQueries = _queryHistory.Count;
                    statistics.AverageExecutionTime = _queryHistory.Any()
                        ? TimeSpan.FromMilliseconds(_queryHistory.Average(q => q.ExecutionTime.TotalMilliseconds))
                        : TimeSpan.Zero;
                    statistics.SlowQueries = _queryHistory.Count(q => q.ExecutionTime.TotalMilliseconds > 1000);
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得資料庫統計資訊時發生錯誤");
                throw;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        /// <summary>
        /// 取得慢查詢資訊
        /// </summary>
        public Task<List<SlowQueryInfo>> GetSlowQueriesAsync(int thresholdMs = 1000)
        {
            lock (_lockObject)
            {
                var slowQueries = _queryHistory
                    .Where(q => q.ExecutionTime.TotalMilliseconds > thresholdMs)
                    .OrderByDescending(q => q.ExecutionTime)
                    .Select(q => new SlowQueryInfo
                    {
                        QueryName = q.QueryName,
                        ExecutionTime = q.ExecutionTime,
                        RecordCount = q.RecordCount,
                        Timestamp = q.Timestamp
                    })
                    .ToList();

                return Task.FromResult(slowQueries);
            }
        }

        /// <summary>
        /// 優化慢查詢
        /// </summary>
        public async Task OptimizeSlowQueriesAsync()
        {
            try
            {
                _logger.LogInformation("開始優化慢查詢");

                // 分析慢查詢
                var slowQueries = await GetSlowQueriesAsync();

                if (!slowQueries.Any())
                {
                    _logger.LogInformation("沒有發現慢查詢");
                    return;
                }

                _logger.LogInformation("發現 {Count} 個慢查詢，開始優化", slowQueries.Count);

                // 執行資料庫優化
                await _context.Database.OpenConnectionAsync();

                using var command = _context.Database.GetDbConnection().CreateCommand();

                // 分析資料庫
                command.CommandText = "ANALYZE;";
                await command.ExecuteNonQueryAsync();

                // 重建索引
                command.CommandText = "REINDEX;";
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("慢查詢優化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "優化慢查詢時發生錯誤");
                throw;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }

    /// <summary>
    /// 查詢分析結果
    /// </summary>
    public class QueryAnalysisResult
    {
        public string QueryName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public int RecordCount { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 查詢執行資訊
    /// </summary>
    public class QueryExecutionInfo
    {
        public string QueryName { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public int RecordCount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 資料庫統計資訊
    /// </summary>
    public class DatabaseStatistics
    {
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public long DatabaseSize { get; set; }
        public Dictionary<string, int> TableStatistics { get; set; } = new();
        public int TotalQueries { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public int SlowQueries { get; set; }
        public DateTime LastAnalyzed { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 慢查詢資訊
    /// </summary>
    public class SlowQueryInfo
    {
        public string QueryName { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public int RecordCount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}