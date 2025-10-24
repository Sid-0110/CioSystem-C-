using CioSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CioSystem.Services.Health
{
    /// <summary>
    /// 服務健康檢查
    /// 檢查各個核心服務的健康狀態
    /// </summary>
    public class ServiceHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceHealthCheck> _logger;

        public ServiceHealthCheck(IServiceProvider serviceProvider, ILogger<ServiceHealthCheck> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceName = context.Registration.Name;
                var service = GetService(serviceName);

                if (service == null)
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"服務 {serviceName} 未找到");
                }

                var healthStatus = await service.HealthCheckAsync();

                if (healthStatus.IsHealthy)
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"服務 {serviceName} 運行正常", new Dictionary<string, object>
                    {
                        ["Status"] = healthStatus.Status,
                        ["Message"] = healthStatus.Message,
                        ["CheckedAt"] = healthStatus.CheckedAt,
                        ["ResponseTime"] = healthStatus.ResponseTime.TotalMilliseconds
                    });
                }
                else
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"服務 {serviceName} 運行異常: {healthStatus.Message}",
                        new Exception(healthStatus.Message),
                        new Dictionary<string, object>
                        {
                            ["Status"] = healthStatus.Status,
                            ["Message"] = healthStatus.Message,
                            ["CheckedAt"] = healthStatus.CheckedAt,
                            ["ResponseTime"] = healthStatus.ResponseTime.TotalMilliseconds
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "健康檢查失敗: {ServiceName}", context.Registration.Name);
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"健康檢查失敗: {ex.Message}", ex);
            }
        }

        private IService? GetService(string serviceName)
        {
            return serviceName switch
            {
                "cache_service" => _serviceProvider.GetService(typeof(ICacheService)) as IService,
                "logging_service" => _serviceProvider.GetService(typeof(ILoggingService)) as IService,
                "monitoring_service" => _serviceProvider.GetService(typeof(IMonitoringService)) as IService,
                _ => null
            };
        }
    }
}