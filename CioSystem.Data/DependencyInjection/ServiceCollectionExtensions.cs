using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CioSystem.Core;
using CioSystem.Data.Repositories;

namespace CioSystem.Data.DependencyInjection
{
    /// <summary>
    /// 服務集合擴展方法
    /// 用於配置資料存取層的依賴注入
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加資料存取層服務（SQLite 預設）
        /// </summary>
        public static IServiceCollection AddDataLayer(
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

                options.UseSqlite(connectionString);

#if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
#endif
                // 忽略 PendingModelChangesWarning，避免啟動遷移時拋例外（開發用）
                options.ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }

        /// <summary>
        /// 添加 SQL Server 資料庫服務
        /// </summary>
        public static IServiceCollection AddSqlServerDataLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<CioSystemDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("SqlServerConnection")
                    ?? throw new InvalidOperationException("SQL Server 連接字串未設定");

                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });

#if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
#endif
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }

        /// <summary>
        /// 添加 PostgreSQL 資料庫服務（需安裝 Npgsql）
        /// </summary>
        public static IServiceCollection AddPostgreSqlDataLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<CioSystemDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("PostgreSqlConnection")
                    ?? throw new InvalidOperationException("PostgreSQL 連接字串未設定");

                // 需要安裝 Npgsql.EntityFrameworkCore.PostgreSQL 套件
                throw new NotImplementedException("需要安裝 Npgsql.EntityFrameworkCore.PostgreSQL 套件");
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }

        /// <summary>
        /// 添加記憶體資料庫服務（用於測試）
        /// </summary>
        public static IServiceCollection AddInMemoryDataLayer(this IServiceCollection services)
        {
            services.AddDbContext<CioSystemDbContext>(options =>
            {
                // 需要安裝 Microsoft.EntityFrameworkCore.InMemory 套件
                throw new NotImplementedException("需要安裝 Microsoft.EntityFrameworkCore.InMemory 套件");
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }

        /// <summary>
        /// 添加資料庫種子服務
        /// </summary>
        public static IServiceCollection AddDatabaseSeed(this IServiceCollection services)
        {
            services.AddScoped<DatabaseSeeder>();
            return services;
        }
    }

    /// <summary>
    /// 資料庫種子資料類別
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly CioSystemDbContext _context;

        public DatabaseSeeder(CioSystemDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 種子資料
        /// </summary>
        public async Task SeedAsync()
        {
            // 不呼叫 EnsureCreated，啟動由 Migrate() 處理

            if (_context.Products.Any())
                return;

            var products = new[]
            {
                new CioSystem.Models.Product
                {
                    Name = "學習筆記本",
                    Description = "高品質的學習筆記本，適合程式設計學習",
                    Price = 299.00m,
                    Category = "文具用品",
                    SKU = "NOTE-001",
                    Brand = "學習品牌",
                    Status = CioSystem.Models.ProductStatus.Active,
                    MinStockLevel = 10,
                    CreatedBy = "System"
                },
                new CioSystem.Models.Product
                {
                    Name = "程式設計書籍",
                    Description = "C# 程式設計入門書籍",
                    Price = 599.00m,
                    Category = "書籍",
                    SKU = "BOOK-001",
                    Brand = "技術出版社",
                    Status = CioSystem.Models.ProductStatus.Active,
                    MinStockLevel = 5,
                    CreatedBy = "System"
                },
                new CioSystem.Models.Product
                {
                    Name = "無線滑鼠",
                    Description = "高精度無線滑鼠，適合程式設計使用",
                    Price = 899.00m,
                    Category = "電腦周邊",
                    SKU = "MOUSE-001",
                    Brand = "科技品牌",
                    Status = CioSystem.Models.ProductStatus.Active,
                    MinStockLevel = 15,
                    CreatedBy = "System"
                }
            };

            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            var inventory = new[]
            {
                new CioSystem.Models.Inventory
                {
                    ProductId = 1,
                    Quantity = 50,
                    ProductSKU = "PROD-001",
                    SafetyStock = 10,
                    ReservedQuantity = 5,
                    Type = CioSystem.Models.InventoryType.Stock,
                    Status = CioSystem.Models.InventoryStatus.Normal,
                    CreatedBy = "System"
                },
                new CioSystem.Models.Inventory
                {
                    ProductId = 2,
                    Quantity = 20,
                    ProductSKU = "PROD-002",
                    SafetyStock = 15,
                    ReservedQuantity = 3,
                    Type = CioSystem.Models.InventoryType.Stock,
                    Status = CioSystem.Models.InventoryStatus.Normal,
                    CreatedBy = "System"
                },
                new CioSystem.Models.Inventory
                {
                    ProductId = 3,
                    Quantity = 30,
                    ProductSKU = "PROD-003",
                    SafetyStock = 20,
                    ReservedQuantity = 0,
                    Type = CioSystem.Models.InventoryType.Stock,
                    Status = CioSystem.Models.InventoryStatus.Normal,
                    CreatedBy = "System"
                }
            };

            await _context.Inventory.AddRangeAsync(inventory);
            await _context.SaveChangesAsync();
        }   
    }
}