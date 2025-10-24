using CioSystem.Services;
using CioSystem.Core.Interfaces;
using CioSystem.Services.Cache;
using CioSystem.Services.Logging;
using CioSystem.Services.Monitoring;
using CioSystem.Services.Health;
using CioSystem.Services.Background;
using CioSystem.Services.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CioSystem.Services
{
    /// <summary>
    /// 服務集合擴展方法
    /// 用於配置服務層的依賴注入
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加服務層服務
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服務集合</returns>
        public static IServiceCollection AddServicesLayer(this IServiceCollection services, IConfiguration? configuration = null)
        {
            // 註冊基礎服務
            services.AddMemoryCache();
            services.AddLogging();

            // 註冊配置類
            services.AddSingleton<MonitoringConfiguration>();
            services.AddSingleton<MonitoringConfiguration>();
            services.AddSingleton<CacheConfiguration>();

            // 註冊核心服務接口
            services.AddSingleton<ICacheService, MemoryCacheService>();
            services.AddSingleton<IMonitoringService, SystemMonitoringService>();
            services.AddScoped<IDatabaseManagementService, DatabaseManagementService>();
            services.AddScoped<ISystemLogService, SystemLogService>();
            services.AddScoped<LogInitializer>();

            // 註冊用戶認證服務
            services.AddScoped<CioSystem.Services.Authentication.IUserService, CioSystem.Services.Authentication.UserService>();

            // 暫時禁用 Redis 配置，使用記憶體快取
            // services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
            // {
            //     var configuration = provider.GetRequiredService<IConfiguration>();
            //     var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            //     return StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
            // });
            // services.AddStackExchangeRedisCache(options =>
            // {
            //     options.Configuration = configuration?.GetConnectionString("Redis") ?? "localhost:6379";
            // });
            // services.AddSingleton<CioSystem.Services.Cache.Redis.IRedisCacheService, CioSystem.Services.Cache.Redis.RedisCacheService>();

            // 暫時註解掉多層快取服務，因為依賴 Redis
            // services.AddSingleton<IMultiLayerCacheService, MultiLayerCacheService>();
            // services.AddSingleton<ISmartCacheStrategy, SmartCacheStrategy>();
            // 暫時註解掉快取服務以避免依賴問題
            //services.AddSingleton<ICacheWarmupService, CacheWarmupService>();
            //services.AddSingleton<ICacheInvalidationService, CacheInvalidationService>();

            // 註冊快取配置
            services.Configure<MemoryCacheOptions>(options =>
            {
                options.SizeLimit = 1000;
                options.CompactionPercentage = 0.25;
            });

            // 註冊日誌配置 - 使用預設配置

            // 註冊監控配置
            services.Configure<MonitoringConfiguration>(config =>
            {
                config.EnablePerformanceCounters = true;
                config.EnableAlerting = true;
                config.MetricsRetentionPeriod = TimeSpan.FromHours(24);
                config.HealthCheckInterval = TimeSpan.FromMinutes(1);
                config.MaxMetricsPerType = 1000;
            });

            // 暫時註解掉有循環依賴的服務
            // services.AddScoped<CacheDecorator<IInventoryService>>();
            // services.AddScoped<CacheDecorator<IProductService>>();
            // services.AddScoped<CacheDecorator<ISalesService>>();
            // services.AddScoped<CacheDecorator<IPurchasesService>>();

            // 暫時註解掉有循環依賴的快取服務
            // services.AddScoped<IProductCacheService, ProductCacheService>();
            // services.AddScoped<IStatisticsCacheService, StatisticsCacheService>();

            // 註冊業務服務
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<ISalesService, SalesService>();
            services.AddScoped<IPurchasesService, PurchasesService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IPerformanceMonitoringService, PerformanceMonitoringService>();

            return services;
        }

        /// <summary>
        /// 添加架構改進服務
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <returns>服務集合</returns>
        public static IServiceCollection AddArchitectureImprovements(this IServiceCollection services)
        {
            // 註冊背景服務
            services.AddHostedService<CacheCleanupService>();
            services.AddHostedService<MonitoringBackgroundService>();
            services.AddHostedService<StatisticsBackgroundService>();
            services.AddHostedService<CacheCleanupBackgroundService>();
            services.AddHostedService<DatabaseMaintenanceBackgroundService>();
            services.AddHostedService<CioSystem.Services.Background.DatabaseOptimizationBackgroundService>();

            // 註冊資料庫優化服務
            services.AddScoped<CioSystem.Services.Monitoring.IDatabaseQueryAnalyzer, CioSystem.Services.Monitoring.DatabaseQueryAnalyzer>();

            return services;
        }
    }
}