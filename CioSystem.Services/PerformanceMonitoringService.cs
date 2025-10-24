using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CioSystem.Services
{
    /// <summary>
    /// 性能監控服務
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        /// <summary>
        /// 開始監控操作
        /// </summary>
        /// <param name="operationName">操作名稱</param>
        /// <returns>監控上下文</returns>
        IPerformanceContext StartMonitoring(string operationName);

        /// <summary>
        /// 記錄性能指標
        /// </summary>
        /// <param name="operationName">操作名稱</param>
        /// <param name="elapsedMilliseconds">執行時間（毫秒）</param>
        /// <param name="additionalInfo">額外信息</param>
        void LogPerformance(string operationName, long elapsedMilliseconds, string additionalInfo = "");

        /// <summary>
        /// 獲取性能統計
        /// </summary>
        /// <returns>性能統計</returns>
        PerformanceStatistics GetPerformanceStatistics();
    }

    /// <summary>
    /// 性能監控服務實現
    /// </summary>
    public class PerformanceMonitoringService : IPerformanceMonitoringService
    {
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly Dictionary<string, List<long>> _performanceMetrics;
        private readonly object _lock = new object();

        public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceMetrics = new Dictionary<string, List<long>>();
        }

        public IPerformanceContext StartMonitoring(string operationName)
        {
            return new PerformanceContext(operationName, this);
        }

        public void LogPerformance(string operationName, long elapsedMilliseconds, string additionalInfo = "")
        {
            lock (_lock)
            {
                if (!_performanceMetrics.ContainsKey(operationName))
                {
                    _performanceMetrics[operationName] = new List<long>();
                }
                _performanceMetrics[operationName].Add(elapsedMilliseconds);
            }

            if (elapsedMilliseconds > 1000) // 超過1秒記錄警告
            {
                _logger.LogWarning("性能警告: {OperationName} 執行時間 {ElapsedMs}ms {AdditionalInfo}",
                    operationName, elapsedMilliseconds, additionalInfo);
            }
            else
            {
                _logger.LogInformation("性能記錄: {OperationName} 執行時間 {ElapsedMs}ms {AdditionalInfo}",
                    operationName, elapsedMilliseconds, additionalInfo);
            }
        }

        public PerformanceStatistics GetPerformanceStatistics()
        {
            lock (_lock)
            {
                var statistics = new PerformanceStatistics();

                foreach (var kvp in _performanceMetrics)
                {
                    var operationName = kvp.Key;
                    var times = kvp.Value;

                    if (times.Any())
                    {
                        statistics.OperationStats[operationName] = new OperationStatistics
                        {
                            OperationName = operationName,
                            TotalCalls = times.Count,
                            AverageTime = times.Average(),
                            MinTime = times.Min(),
                            MaxTime = times.Max(),
                            TotalTime = times.Sum()
                        };
                    }
                }

                return statistics;
            }
        }
    }

    /// <summary>
    /// 性能監控上下文
    /// </summary>
    public interface IPerformanceContext : IDisposable
    {
        /// <summary>
        /// 操作名稱
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// 是否已停止
        /// </summary>
        bool IsStopped { get; }
    }

    /// <summary>
    /// 性能監控上下文實現
    /// </summary>
    public class PerformanceContext : IPerformanceContext
    {
        private readonly Stopwatch _stopwatch;
        private readonly IPerformanceMonitoringService _monitoringService;
        private bool _disposed = false;

        public string OperationName { get; }
        public bool IsStopped { get; private set; }

        public PerformanceContext(string operationName, IPerformanceMonitoringService monitoringService)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (!_disposed && !IsStopped)
            {
                _stopwatch.Stop();
                _monitoringService.LogPerformance(OperationName, _stopwatch.ElapsedMilliseconds);
                IsStopped = true;
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 性能統計
    /// </summary>
    public class PerformanceStatistics
    {
        public Dictionary<string, OperationStatistics> OperationStats { get; set; } = new Dictionary<string, OperationStatistics>();
    }

    /// <summary>
    /// 操作統計
    /// </summary>
    public class OperationStatistics
    {
        public string OperationName { get; set; } = string.Empty;
        public int TotalCalls { get; set; }
        public double AverageTime { get; set; }
        public long MinTime { get; set; }
        public long MaxTime { get; set; }
        public long TotalTime { get; set; }
    }
}