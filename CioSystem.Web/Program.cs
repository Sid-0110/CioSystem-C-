using CioSystem.Data.DependencyInjection;
using CioSystem.Services;
using CioSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Data;
using CioSystem.Web.Security;
using CioSystem.Web.Middleware;
using CioSystem.Services.Monitoring;
using CioSystem.Services.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 設定 SQLite 絕對路徑（確保只使用當前 Web 專案下的 DB）
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "CioSystem.db");
builder.Configuration["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared";

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

// 添加會話支援
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".CioSystem.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 添加認證和授權服務
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = ".CioSystem.Auth";
    });

builder.Services.AddAuthorization();
// 註冊資料層和服務層（使用優化版本）
builder.Services.AddOptimizedSqliteDataLayer(builder.Configuration);
builder.Services.AddServicesLayer(builder.Configuration);

// 註冊 Web 服務（依賴於基礎服務）
builder.Services.AddScoped<CioSystem.Web.Services.IMetricsService, CioSystem.Web.Services.MetricsService>();

// 註冊健康檢查
builder.Services.AddHealthChecks()
    .AddCheck<CioSystem.Services.Health.DatabaseHealthCheck>("database")
    .AddCheck<CioSystem.Services.Health.CacheHealthCheck>("cache")
    .AddCheck<CioSystem.Services.Health.SystemResourcesHealthCheck>("system_resources");

// 註冊系統日誌服務
builder.Services.AddScoped<CioSystem.Services.Logging.ISystemLogService, CioSystem.Services.Logging.SystemLogService>();

// 註冊安全服務
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<IInputValidationService, InputValidationService>();
builder.Services.AddScoped<ISecurityLogService, SecurityLogService>();

// 註冊監控和日誌服務
builder.Services.AddScoped<IAdvancedMonitoringService, AdvancedMonitoringService>();
builder.Services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();

// 配置安全標頭
builder.Services.Configure<SecurityHeadersOptions>(options =>
{
    options.EnableContentTypeOptions = true;
    options.EnableFrameOptions = true;
    options.FrameOptions = "DENY";
    options.EnableXssProtection = true;
    options.EnableReferrerPolicy = true;
    options.ReferrerPolicy = "strict-origin-when-cross-origin";
    options.EnablePermissionsPolicy = true;
    options.PermissionsPolicy = "geolocation=(), microphone=(), camera=()";
    options.EnableHsts = true;
    options.HstsMaxAge = 31536000;
    options.EnableCsp = false;
    options.CspDefaultSrc = "'self'";
    options.CspScriptSrc = "'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://unpkg.com";
    options.CspStyleSrc = "'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com https://unpkg.com";
    options.CspImgSrc = "'self' data: https: blob:";
    options.CspFontSrc = "'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://unpkg.com";
    options.CspConnectSrc = "'self' https:";
    options.CspMediaSrc = "'self'";
    options.CspObjectSrc = "'none'";
    options.HideServerHeader = true;
    options.RemovePoweredByHeader = true;
    options.CacheControl = "no-cache, no-store, must-revalidate";
    options.EnableDownloadOptions = true;
    options.EnableDnsPrefetchControl = true;
});

var app = builder.Build();

// 確保資料庫已創建和遷移
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CioSystemDbContext>();

    // 印出實際連線字串
    var connStr = context.Database.GetDbConnection().ConnectionString;
    Console.WriteLine($"[Startup] Using SQLite: {connStr}");

    bool needReset = false;
    bool usedEnsureCreated = false;
    bool skipMigrateDueToExistingSchema = false;
    try
    {
        await context.Database.OpenConnectionAsync();

        // 檢查是否存在遷移歷史表
        using var cmdHasHistoryTable = context.Database.GetDbConnection().CreateCommand();
        cmdHasHistoryTable.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
        var hasHistoryTable = Convert.ToInt32(await cmdHasHistoryTable.ExecuteScalarAsync()) > 0;

        // 檢查是否已有 Products 表
        using var cmdHasProducts = context.Database.GetDbConnection().CreateCommand();
        cmdHasProducts.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Products'";
        var hasProductsTable = Convert.ToInt32(await cmdHasProducts.ExecuteScalarAsync()) > 0;

        // 檢查是否已有 Users 表
        using var cmdHasUsers = context.Database.GetDbConnection().CreateCommand();
        cmdHasUsers.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Users'";
        var hasUsersTable = Convert.ToInt32(await cmdHasUsers.ExecuteScalarAsync()) > 0;

        // 若有遷移歷史表，檢查其是否為空
        bool historyIsEmpty = false;
        if (hasHistoryTable)
        {
            using var cmdHistoryRows = context.Database.GetDbConnection().CreateCommand();
            cmdHistoryRows.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory";
            historyIsEmpty = Convert.ToInt32(await cmdHistoryRows.ExecuteScalarAsync()) == 0;
        }

        // 額外檢查 Products 是否缺少 CostPrice 欄位
        bool missingCostPrice = false;
        if (hasProductsTable)
        {
            using var cmdCheckColumn = context.Database.GetDbConnection().CreateCommand();
            cmdCheckColumn.CommandText = "PRAGMA table_info(Products)";
            using var reader = await cmdCheckColumn.ExecuteReaderAsync();
            bool hasCostPrice = false;
            while (await reader.ReadAsync())
            {
                var colName = reader[1]?.ToString();
                if (string.Equals(colName, "CostPrice", StringComparison.OrdinalIgnoreCase))
                {
                    hasCostPrice = true;
                    break;
                }
            }
            missingCostPrice = !hasCostPrice;
        }

        // 若缺少 CostPrice，直接以 DDL 修復（SQLite 支援 ADD COLUMN）
        if (hasProductsTable && missingCostPrice)
        {
            Console.WriteLine("[Startup] Missing column Products.CostPrice detected. Applying quick DDL fix...");
            using var cmdAddCol = context.Database.GetDbConnection().CreateCommand();
            // 與先前遷移一致，使用 TEXT NOT NULL DEFAULT '0.0'
            cmdAddCol.CommandText = "ALTER TABLE Products ADD COLUMN CostPrice TEXT NOT NULL DEFAULT '0.0'";
            await cmdAddCol.ExecuteNonQueryAsync();
            Console.WriteLine("[Startup] Column Products.CostPrice added.");
        }

        // 不再使用 EnsureCreated 建結構，統一交由遷移負責，避免造成遷移歷史不一致
        // if (!hasUsersTable)
        // {
        //     Console.WriteLine("[Startup] Missing Users table. Running EnsureCreated to create schema...");
        //     await context.Database.EnsureCreatedAsync();
        //     Console.WriteLine("[Startup] EnsureCreated completed.");
        //     usedEnsureCreated = true;
        // }

        // 條件：開發環境 +（沒有歷史表或歷史表為空）+ 已有 Products 表 => 視為不一致，需重建
        var allowAutoReset = builder.Configuration.GetValue<bool>("Database:AllowAutoReset");
        // 額外要求環境變數同意（避免誤刪資料庫）
        var envConsent = Environment.GetEnvironmentVariable("CIO_AUTO_RESET");
        var consented = string.Equals(envConsent, "1", StringComparison.Ordinal);
        if (allowAutoReset && consented && app.Environment.IsDevelopment() && hasProductsTable && (!hasHistoryTable || historyIsEmpty))
        {
            Console.WriteLine("[Startup] Detected schema without migrations history (or empty history) in Development. Resetting SQLite DB (AllowAutoReset=true)...");
            needReset = true;
        }
        else if (hasProductsTable && (!hasHistoryTable || historyIsEmpty))
        {
            // 若已有實體資料表但缺少遷移歷史，避免執行 Migrate 造成 'table already exists' 錯誤
            Console.WriteLine("[Startup] Detected existing schema without migrations history. Skipping EF migrations to avoid conflicts.");
            skipMigrateDueToExistingSchema = true;
        }
    }
    finally
    {
        await context.Database.CloseConnectionAsync();
    }

    if (needReset)
    {
        await context.DisposeAsync();
        if (File.Exists(dbPath))
        {
            try
            {
                // 先備份後刪除
                var backupDir = Path.Combine(builder.Environment.ContentRootPath, "backups");
                Directory.CreateDirectory(backupDir);
                var backupPath = Path.Combine(backupDir, $"CioSystem_{DateTime.Now:yyyyMMdd_HHmmss}.db.bak");
                File.Copy(dbPath, backupPath, true);
                Console.WriteLine($"[Startup] Database backed up to: {backupPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Startup] Database backup before reset failed: {ex.Message}");
            }

            File.Delete(dbPath);
        }

        using var scope2 = app.Services.CreateScope();
        var ctx2 = scope2.ServiceProvider.GetRequiredService<CioSystemDbContext>();
        await ctx2.Database.MigrateAsync();
        Console.WriteLine("[Startup] Database migration completed after reset.");
        // 禁用種子資料，使用資料庫中的實際資料
        // var seeder2 = new CioSystem.Data.DependencyInjection.DatabaseSeeder(ctx2);
        // await seeder2.SeedAsync();
        Console.WriteLine("[Startup] Database seeding disabled after reset - using existing data.");
    }
    else
    {
        if (!usedEnsureCreated && !skipMigrateDueToExistingSchema)
        {
            await context.Database.MigrateAsync();
            Console.WriteLine("[Startup] Database migration completed.");
        }
        // 確保 Products 有 CostPrice 欄位（為舊 DB 提供快速修復）
        try
        {
            await context.Database.OpenConnectionAsync();
            using var cmdCheckColumn2 = context.Database.GetDbConnection().CreateCommand();
            cmdCheckColumn2.CommandText = "PRAGMA table_info(Products)";
            using var reader2 = await cmdCheckColumn2.ExecuteReaderAsync();
            bool hasCostPrice2 = false;
            while (await reader2.ReadAsync())
            {
                var colName = reader2[1]?.ToString();
                if (string.Equals(colName, "CostPrice", StringComparison.OrdinalIgnoreCase))
                {
                    hasCostPrice2 = true;
                    break;
                }
            }
            if (!hasCostPrice2)
            {
                Console.WriteLine("[Startup] Quick DDL: Adding missing Products.CostPrice column...");
                using var cmdAddCol2 = context.Database.GetDbConnection().CreateCommand();
                // 與先前一致，TEXT NOT NULL DEFAULT '0.0'（舊資料相容）
                cmdAddCol2.CommandText = "ALTER TABLE Products ADD COLUMN CostPrice TEXT NOT NULL DEFAULT '0.0'";
                await cmdAddCol2.ExecuteNonQueryAsync();
                Console.WriteLine("[Startup] Quick DDL: Products.CostPrice added.");
            }

            // 確保 Inventory 有 EmployeeRetention 欄位（舊 DB 兼容）
            using var cmdCheckInv = context.Database.GetDbConnection().CreateCommand();
            cmdCheckInv.CommandText = "PRAGMA table_info(Inventory)";
            using var readerInv = await cmdCheckInv.ExecuteReaderAsync();
            bool hasEmployeeRetention = false;
            while (await readerInv.ReadAsync())
            {
                var colName = readerInv[1]?.ToString();
                if (string.Equals(colName, "EmployeeRetention", StringComparison.OrdinalIgnoreCase))
                {
                    hasEmployeeRetention = true;
                    break;
                }
            }
            if (!hasEmployeeRetention)
            {
                Console.WriteLine("[Startup] Quick DDL: Adding missing Inventory.EmployeeRetention column...");
                using var cmdAddInvCol = context.Database.GetDbConnection().CreateCommand();
                cmdAddInvCol.CommandText = "ALTER TABLE Inventory ADD COLUMN EmployeeRetention INTEGER NOT NULL DEFAULT 0";
                await cmdAddInvCol.ExecuteNonQueryAsync();
                Console.WriteLine("[Startup] Quick DDL: Inventory.EmployeeRetention added.")
;
            }

            // 跳過 EmployeeRetention 相關的資料處理，因為該欄位不存在
            Console.WriteLine("[Startup] Skipping EmployeeRetention processing - column does not exist in current schema.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Startup] Quick DDL check failed: {ex.Message}");
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        // 禁用種子資料，使用資料庫中的實際資料
        // var seeder = new CioSystem.Data.DependencyInjection.DatabaseSeeder(context);
        // await seeder.SeedAsync();
        Console.WriteLine("[Startup] Database seeding disabled - using existing data.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 開發環境不強制 HTTPS，避免 SignalR 客戶端被導向不存在的 https 埠
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

// 添加安全標頭中間件
app.UseSecurityHeaders();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// API 控制器路由
app.MapControllers();

// MVC 控制器路由 - 暫時取消登入，直接導向首頁
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 添加資料庫管理路由
app.MapControllerRoute(
    name: "database-management",
    pattern: "DatabaseManagement/{action=DatabaseManagement}/{id?}",
    defaults: new { controller = "SystemSettings" });

// 添加 SystemSettings 資料庫管理路由
app.MapControllerRoute(
    name: "system-settings-database",
    pattern: "SystemSettings/DatabaseManagement/{action=DatabaseManagement}/{id?}",
    defaults: new { controller = "SystemSettings" });

// SignalR hubs
app.MapHub<CioSystem.Web.Hubs.DashboardHub>("/hubs/dashboard");

// 健康檢查端點
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                data = entry.Value.Data,
                duration = entry.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// 初始化預設管理員帳號
// 移除自動初始化管理員帳號 - 用戶需要手動註冊
// using (var scope = app.Services.CreateScope())
// {
//     try
//     {
//         var userService = scope.ServiceProvider.GetRequiredService<CioSystem.Services.Authentication.IUserService>();
//         await userService.InitializeDefaultAdminAsync();
//     }
//     catch (Exception ex)
//     {
//         var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
//         logger.LogError(ex, "初始化預設管理員帳號時發生錯誤");
//     }
// }

// 添加應用程式關閉時的清理處理
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(async () =>
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<CioSystem.Services.Authentication.IUserService>();
            var systemLogService = scope.ServiceProvider.GetRequiredService<CioSystem.Services.Logging.ISystemLogService>();
            
            // 程式結束時登出所有活躍用戶
            await userService.LogoutAllUsersAsync();
            
            // 記錄系統關閉日誌
            await systemLogService.LogAsync("Info", "系統正在關閉，已登出所有用戶", "System");
        }
    }
    catch (Exception ex)
    {
        // 記錄錯誤但不阻止關閉
        Console.WriteLine($"應用程式關閉時發生錯誤: {ex.Message}");
    }
});

app.Run();
