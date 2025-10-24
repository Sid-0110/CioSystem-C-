using CioSystem.Data.DependencyInjection;
using CioSystem.API.Services;
using CioSystem.Services;
using ValidationResult = CioSystem.Services.ValidationResult;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加資料存取層服務
builder.Services.AddDataLayer(builder.Configuration);

// 添加 API 服務
builder.Services.AddScoped<CioSystem.Services.IProductService, CioSystem.API.Services.ProductService>();
builder.Services.AddScoped<CioSystem.Services.IInventoryService, CioSystem.Services.InventoryService>();
builder.Services.AddScoped<CioSystem.Services.ISalesService, CioSystem.API.Services.SalesService>();
builder.Services.AddScoped<CioSystem.Services.IPurchasesService, CioSystem.API.Services.PurchasesService>();

// 添加資料庫種子資料
builder.Services.AddDatabaseSeed();

// 註冊 API 優化服務
builder.Services.AddScoped<IApiPerformanceService, ApiPerformanceService>();
builder.Services.AddScoped<IApiCacheService, ApiCacheService>();
builder.Services.AddScoped<IApiVersionService, ApiVersionService>();

// 添加記憶體快取
builder.Services.AddMemoryCache();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // 初始化資料庫和種子資料
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }
}

app.UseHttpsRedirection();

// 添加 API 優化中間件
app.UseApiPerformance();
app.UseApiCache();

app.UseAuthorization();

app.MapControllers();

app.Run();
