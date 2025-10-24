using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CioSystem.API.Services
{
    /// <summary>
    /// API 效能服務實現
    /// 提供 API 效能監控和優化功能
    /// </summary>
    public class ApiPerformanceService : IApiPerformanceService
    {
        private readonly ILogger<ApiPerformanceService> _logger;
        private readonly ConcurrentDictionary<string, RequestMetrics> _activeRequests;
        private readonly ConcurrentQueue<RequestMetrics> _completedRequests;
        private readonly object _statsLock = new object();
        private ApiPerformanceStats _stats;

        public ApiPerformanceService(ILogger<ApiPerformanceService> logger)
        {
            _logger = logger;
            _activeRequests = new ConcurrentDictionary<string, RequestMetrics>();
            _completedRequests = new ConcurrentQueue<RequestMetrics>();
            _stats = new ApiPerformanceStats();
        }

        public IDisposable StartRequestTracking(string controllerName, string actionName, string requestId)
        {
            var stopwatch = Stopwatch.StartNew();
            var metrics = new RequestMetrics
            {
                RequestId = requestId,
                ControllerName = controllerName,
                ActionName = actionName,
                StartTime = DateTime.UtcNow,
                Stopwatch = stopwatch
            };

            _activeRequests.TryAdd(requestId, metrics);
            _logger.LogInformation("開始追蹤 API 請求: {Controller}.{Action} (ID: {RequestId})",
                controllerName, actionName, requestId);

            return new RequestTracker(this, requestId);
        }

        public async Task RecordRequestCompletionAsync(string requestId, int statusCode, long responseSize)
        {
            if (_activeRequests.TryRemove(requestId, out var metrics))
            {
                metrics.Stopwatch.Stop();
                metrics.EndTime = DateTime.UtcNow;
                metrics.StatusCode = statusCode;
                metrics.ResponseSize = responseSize;
                metrics.Duration = metrics.Stopwatch.ElapsedMilliseconds;

                _completedRequests.Enqueue(metrics);

                // 保持最近 1000 個請求的記錄
                while (_completedRequests.Count > 1000)
                {
                    _completedRequests.TryDequeue(out _);
                }

                await UpdateStatsAsync();

                _logger.LogInformation("完成 API 請求: {Controller}.{Action} (ID: {RequestId}, 狀態: {StatusCode}, 耗時: {Duration}ms, 大小: {Size}bytes)",
                    metrics.ControllerName, metrics.ActionName, requestId, statusCode, metrics.Duration, responseSize);
            }
        }

        public async Task<ApiPerformanceStats> GetPerformanceStatsAsync()
        {
            await UpdateStatsAsync();
            return _stats;
        }

        public async Task<ApiHealthStatus> GetHealthStatusAsync()
        {
            var stats = await GetPerformanceStatsAsync();

            var isHealthy = stats.ErrorRate < 0.05 && // 錯誤率低於 5%
                           stats.AverageResponseTime < 1000 && // 平均響應時間低於 1 秒
                           stats.TotalRequests > 0; // 有請求記錄

            return new ApiHealthStatus
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "Healthy" : "Unhealthy",
                Message = isHealthy ? "API 運行正常" : "API 效能需要關注",
                CheckedAt = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["TotalRequests"] = stats.TotalRequests,
                    ["AverageResponseTime"] = stats.AverageResponseTime,
                    ["ErrorRate"] = stats.ErrorRate,
                    ["ActiveRequests"] = _activeRequests.Count
                }
            };
        }

        private async Task UpdateStatsAsync()
        {
            await Task.Run(() =>
            {
                lock (_statsLock)
                {
                    var completedRequests = _completedRequests.ToArray();

                    if (completedRequests.Length == 0)
                    {
                        _stats = new ApiPerformanceStats
                        {
                            LastUpdated = DateTime.UtcNow
                        };
                        return;
                    }

                    var responseTimes = completedRequests.Select(r => r.Duration).ToArray();
                    var errorCount = completedRequests.Count(r => r.StatusCode >= 400);
                    var totalResponseSize = completedRequests.Sum(r => r.ResponseSize);

                    _stats = new ApiPerformanceStats
                    {
                        TotalRequests = completedRequests.Length,
                        AverageResponseTime = responseTimes.Average(),
                        MaxResponseTime = responseTimes.Max(),
                        MinResponseTime = responseTimes.Min(),
                        ErrorCount = errorCount,
                        ErrorRate = (double)errorCount / completedRequests.Length,
                        TotalResponseSize = totalResponseSize,
                        AverageResponseSize = totalResponseSize / completedRequests.Length,
                        LastUpdated = DateTime.UtcNow
                    };
                }
            });
        }

        private class RequestMetrics
        {
            public string RequestId { get; set; } = string.Empty;
            public string ControllerName { get; set; } = string.Empty;
            public string ActionName { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public Stopwatch Stopwatch { get; set; } = new Stopwatch();
            public long Duration { get; set; }
            public int StatusCode { get; set; }
            public long ResponseSize { get; set; }
        }

        private class RequestTracker : IDisposable
        {
            private readonly ApiPerformanceService _service;
            private readonly string _requestId;
            private bool _disposed = false;

            public RequestTracker(ApiPerformanceService service, string requestId)
            {
                _service = service;
                _requestId = requestId;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    // 這裡不直接記錄完成，因為我們需要狀態碼和響應大小
                    // 這些資訊會在控制器中手動記錄
                    _disposed = true;
                }
            }
        }
    }
}