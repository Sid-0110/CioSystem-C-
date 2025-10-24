using CioSystem.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CioSystem.Services.Background
{
    /// <summary>
    /// 監控背景服務
    /// 定期收集系統指標和檢查警報
    /// </summary>
    public class MonitoringBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonitoringBackgroundService> _logger;
        private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(1);

        public MonitoringBackgroundService(IServiceProvider serviceProvider, ILogger<MonitoringBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("監控背景服務已啟動");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformMonitoringAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "監控過程中發生錯誤");
                }

                await Task.Delay(_monitoringInterval, stoppingToken);
            }

            _logger.LogInformation("監控背景服務已停止");
        }

        private async Task PerformMonitoringAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var monitoringService = scope.ServiceProvider.GetService<IMonitoringService>();
            var loggingService = scope.ServiceProvider.GetService<ILoggingService>();

            if (monitoringService == null || loggingService == null)
            {
                _logger.LogWarning("無法取得監控服務或日誌服務");
                return;
            }

            try
            {
                // 收集系統資源使用情況
                var resourceUsage = await monitoringService.GetResourceUsageAsync();

                // 記錄系統指標
                await monitoringService.RecordMetricAsync("system.cpu_usage", resourceUsage.CpuUsage);
                await monitoringService.RecordMetricAsync("system.memory_usage", resourceUsage.MemoryUsage);
                await monitoringService.RecordMetricAsync("system.thread_count", resourceUsage.ThreadCount);

                // 檢查警報
                var alerts = await monitoringService.CheckAlertsAsync();
                foreach (var alert in alerts)
                {
                    _logger.LogWarning("警報觸發: {AlertMessage}", alert.Message);
                    await loggingService.LogWarningAsync($"警報觸發: {alert.Message}",
                        properties: new Dictionary<string, object>
                        {
                            ["AlertId"] = alert.Id,
                            ["RuleId"] = alert.RuleId,
                            ["Severity"] = alert.Severity.ToString(),
                            ["TriggeredAt"] = alert.TriggeredAt
                        });
                }

                // 記錄監控性能
                await loggingService.LogPerformanceAsync("SystemMonitoring", TimeSpan.FromMilliseconds(50),
                    new Dictionary<string, object>
                    {
                        ["CpuUsage"] = resourceUsage.CpuUsage,
                        ["MemoryUsage"] = resourceUsage.MemoryUsage,
                        ["ThreadCount"] = resourceUsage.ThreadCount,
                        ["ActiveAlerts"] = alerts.Count()
                    });

                _logger.LogDebug("系統監控完成 - CPU: {CpuUsage:F1}%, 記憶體: {MemoryUsage}MB, 執行緒: {ThreadCount}, 警報: {AlertCount}",
                    resourceUsage.CpuUsage, resourceUsage.MemoryUsage, resourceUsage.ThreadCount, alerts.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行系統監控時發生錯誤");
            }
        }
    }
}