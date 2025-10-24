using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CioSystem.Services.Monitoring
{
    /// <summary>
    /// 效能監控指標服務
    /// </summary>
    public interface IPerformanceMetricsService
    {
        void RecordRequestDuration(string endpoint, TimeSpan duration);
        void RecordDatabaseQuery(string query, TimeSpan duration);
        void RecordCacheHit(string cacheKey);
        void RecordCacheMiss(string cacheKey);
        void RecordMemoryUsage(long bytes);
        void RecordCpuUsage(double percentage);
        PerformanceMetrics GetMetrics();
        void ResetMetrics();
    }

    /// <summary>
    /// 效能監控指標服務實現
    /// </summary>
    public class PerformanceMetricsService : IPerformanceMetricsService
    {
        private readonly ILogger<PerformanceMetricsService> _logger;
        private readonly object _lock = new object();
        private readonly PerformanceMetrics _metrics = new PerformanceMetrics();
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public PerformanceMetricsService(ILogger<PerformanceMetricsService> logger)
        {
            _logger = logger;
        }

        public void RecordRequestDuration(string endpoint, TimeSpan duration)
        {
            lock (_lock)
            {
                _metrics.TotalRequests++;
                _metrics.TotalRequestDuration += duration;

                if (!_metrics.EndpointDurations.ContainsKey(endpoint))
                {
                    _metrics.EndpointDurations[endpoint] = new List<TimeSpan>();
                }

                _metrics.EndpointDurations[endpoint].Add(duration);

                if (duration > _metrics.MaxRequestDuration)
                {
                    _metrics.MaxRequestDuration = duration;
                }

                if (_metrics.MinRequestDuration == TimeSpan.Zero || duration < _metrics.MinRequestDuration)
                {
                    _metrics.MinRequestDuration = duration;
                }
            }

            _logger.LogDebug("請求效能記錄: {Endpoint}, 耗時: {Duration}ms", endpoint, duration.TotalMilliseconds);
        }

        public void RecordDatabaseQuery(string query, TimeSpan duration)
        {
            lock (_lock)
            {
                _metrics.TotalDatabaseQueries++;
                _metrics.TotalDatabaseDuration += duration;

                if (!_metrics.QueryDurations.ContainsKey(query))
                {
                    _metrics.QueryDurations[query] = new List<TimeSpan>();
                }

                _metrics.QueryDurations[query].Add(duration);
            }

            _logger.LogDebug("資料庫查詢效能記錄: {Query}, 耗時: {Duration}ms", query, duration.TotalMilliseconds);
        }

        public void RecordCacheHit(string cacheKey)
        {
            lock (_lock)
            {
                _metrics.CacheHits++;
            }

            _logger.LogDebug("快取命中: {CacheKey}", cacheKey);
        }

        public void RecordCacheMiss(string cacheKey)
        {
            lock (_lock)
            {
                _metrics.CacheMisses++;
            }

            _logger.LogDebug("快取未命中: {CacheKey}", cacheKey);
        }

        public void RecordMemoryUsage(long bytes)
        {
            lock (_lock)
            {
                _metrics.CurrentMemoryUsage = bytes;
                if (bytes > _metrics.MaxMemoryUsage)
                {
                    _metrics.MaxMemoryUsage = bytes;
                }
            }
        }

        public void RecordCpuUsage(double percentage)
        {
            lock (_lock)
            {
                _metrics.CurrentCpuUsage = percentage;
                if (percentage > _metrics.MaxCpuUsage)
                {
                    _metrics.MaxCpuUsage = percentage;
                }
            }
        }

        public PerformanceMetrics GetMetrics()
        {
            lock (_lock)
            {
                _metrics.Uptime = _stopwatch.Elapsed;
                _metrics.AverageRequestDuration = _metrics.TotalRequests > 0
                    ? TimeSpan.FromMilliseconds(_metrics.TotalRequestDuration.TotalMilliseconds / _metrics.TotalRequests)
                    : TimeSpan.Zero;
                _metrics.AverageDatabaseDuration = _metrics.TotalDatabaseQueries > 0
                    ? TimeSpan.FromMilliseconds(_metrics.TotalDatabaseDuration.TotalMilliseconds / _metrics.TotalDatabaseQueries)
                    : TimeSpan.Zero;
                _metrics.CacheHitRate = _metrics.CacheHits + _metrics.CacheMisses > 0
                    ? (double)_metrics.CacheHits / (_metrics.CacheHits + _metrics.CacheMisses) * 100
                    : 0;

                return new PerformanceMetrics
                {
                    TotalRequests = _metrics.TotalRequests,
                    TotalRequestDuration = _metrics.TotalRequestDuration,
                    AverageRequestDuration = _metrics.AverageRequestDuration,
                    MaxRequestDuration = _metrics.MaxRequestDuration,
                    MinRequestDuration = _metrics.MinRequestDuration,
                    TotalDatabaseQueries = _metrics.TotalDatabaseQueries,
                    TotalDatabaseDuration = _metrics.TotalDatabaseDuration,
                    AverageDatabaseDuration = _metrics.AverageDatabaseDuration,
                    CacheHits = _metrics.CacheHits,
                    CacheMisses = _metrics.CacheMisses,
                    CacheHitRate = _metrics.CacheHitRate,
                    CurrentMemoryUsage = _metrics.CurrentMemoryUsage,
                    MaxMemoryUsage = _metrics.MaxMemoryUsage,
                    CurrentCpuUsage = _metrics.CurrentCpuUsage,
                    MaxCpuUsage = _metrics.MaxCpuUsage,
                    Uptime = _metrics.Uptime,
                    EndpointDurations = new Dictionary<string, List<TimeSpan>>(_metrics.EndpointDurations),
                    QueryDurations = new Dictionary<string, List<TimeSpan>>(_metrics.QueryDurations)
                };
            }
        }

        public void ResetMetrics()
        {
            lock (_lock)
            {
                _metrics.TotalRequests = 0;
                _metrics.TotalRequestDuration = TimeSpan.Zero;
                _metrics.AverageRequestDuration = TimeSpan.Zero;
                _metrics.MaxRequestDuration = TimeSpan.Zero;
                _metrics.MinRequestDuration = TimeSpan.Zero;
                _metrics.TotalDatabaseQueries = 0;
                _metrics.TotalDatabaseDuration = TimeSpan.Zero;
                _metrics.AverageDatabaseDuration = TimeSpan.Zero;
                _metrics.CacheHits = 0;
                _metrics.CacheMisses = 0;
                _metrics.CacheHitRate = 0;
                _metrics.CurrentMemoryUsage = 0;
                _metrics.MaxMemoryUsage = 0;
                _metrics.CurrentCpuUsage = 0;
                _metrics.MaxCpuUsage = 0;
                _metrics.EndpointDurations.Clear();
                _metrics.QueryDurations.Clear();
                _stopwatch.Restart();
            }

            _logger.LogInformation("效能指標已重置");
        }
    }

    /// <summary>
    /// 效能指標資料模型
    /// </summary>
    public class PerformanceMetrics
    {
        public int TotalRequests { get; set; }
        public TimeSpan TotalRequestDuration { get; set; }
        public TimeSpan AverageRequestDuration { get; set; }
        public TimeSpan MaxRequestDuration { get; set; }
        public TimeSpan MinRequestDuration { get; set; }
        public int TotalDatabaseQueries { get; set; }
        public TimeSpan TotalDatabaseDuration { get; set; }
        public TimeSpan AverageDatabaseDuration { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public double CacheHitRate { get; set; }
        public long CurrentMemoryUsage { get; set; }
        public long MaxMemoryUsage { get; set; }
        public double CurrentCpuUsage { get; set; }
        public double MaxCpuUsage { get; set; }
        public TimeSpan Uptime { get; set; }
        public Dictionary<string, List<TimeSpan>> EndpointDurations { get; set; } = new();
        public Dictionary<string, List<TimeSpan>> QueryDurations { get; set; } = new();
    }
}