using CioSystem.API.Services;
using System.Text.Json;

namespace CioSystem.API.Middleware
{
    /// <summary>
    /// API 快取中間件
    /// 自動處理 API 響應快取
    /// </summary>
    public class ApiCacheMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiCacheMiddleware> _logger;
        private readonly IApiCacheService _cacheService;

        public ApiCacheMiddleware(
            RequestDelegate next,
            ILogger<ApiCacheMiddleware> logger,
            IApiCacheService cacheService)
        {
            _next = next;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 只對 GET 請求進行快取
            if (context.Request.Method != "GET")
            {
                await _next(context);
                return;
            }

            // 檢查是否應該快取此請求
            if (!ShouldCacheRequest(context))
            {
                await _next(context);
                return;
            }

            var cacheKey = GenerateCacheKey(context);

            try
            {
                // 嘗試從快取取得響應
                var cachedResponse = await _cacheService.GetCachedResponseAsync<object>(cacheKey);
                if (cachedResponse != null)
                {
                    _logger.LogDebug("返回快取響應: {CacheKey}", cacheKey);
                    await WriteCachedResponseAsync(context, cachedResponse);
                    return;
                }

                // 快取未命中，執行原始請求
                var originalResponseBody = context.Response.Body;
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await _next(context);

                // 檢查響應是否成功
                if (context.Response.StatusCode == 200)
                {
                    // 讀取響應內容
                    responseBody.Seek(0, SeekOrigin.Begin);
                    var responseContent = await new StreamReader(responseBody).ReadToEndAsync();

                    // 將響應寫回原始流
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalResponseBody);
                    context.Response.Body = originalResponseBody;

                    // 快取響應
                    var expiration = GetCacheExpiration(context);
                    await _cacheService.SetCachedResponseAsync(cacheKey, responseContent, expiration);

                    _logger.LogDebug("快取新響應: {CacheKey}, 過期時間: {Expiration}", cacheKey, expiration);
                }
                else
                {
                    // 響應不成功，不進行快取
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalResponseBody);
                    context.Response.Body = originalResponseBody;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API 快取中間件發生錯誤: {CacheKey}", cacheKey);
                await _next(context);
            }
        }

        private bool ShouldCacheRequest(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // 只快取 API 端點
            if (!path.StartsWith("/api/"))
            {
                return false;
            }

            // 排除某些端點
            var excludedPaths = new[]
            {
                "/api/health",
                "/api/version",
                "/api/performance",
                "/api/cache"
            };

            return !excludedPaths.Any(excluded => path.StartsWith(excluded));
        }

        private string GenerateCacheKey(HttpContext context)
        {
            var controller = GetControllerName(context);
            var action = GetActionName(context);
            var parameters = GetRequestParameters(context);

            return _cacheService.GenerateCacheKey(controller, action, parameters);
        }

        private string GetControllerName(HttpContext context)
        {
            var routeData = context.Request.RouteValues;
            return routeData.TryGetValue("controller", out var controller)
                ? controller?.ToString() ?? "Unknown"
                : "Unknown";
        }

        private string GetActionName(HttpContext context)
        {
            var routeData = context.Request.RouteValues;
            return routeData.TryGetValue("action", out var action)
                ? action?.ToString() ?? "Unknown"
                : "Unknown";
        }

        private Dictionary<string, object> GetRequestParameters(HttpContext context)
        {
            var parameters = new Dictionary<string, object>();

            // 從查詢字串取得參數
            foreach (var query in context.Request.Query)
            {
                parameters[query.Key] = query.Value.ToString();
            }

            // 從路由值取得參數
            foreach (var routeValue in context.Request.RouteValues)
            {
                if (routeValue.Key != "controller" && routeValue.Key != "action")
                {
                    parameters[routeValue.Key] = routeValue.Value?.ToString() ?? "";
                }
            }

            return parameters;
        }

        private TimeSpan GetCacheExpiration(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // 根據端點類型設定不同的快取時間
            return path switch
            {
                var p when p.Contains("products") => TimeSpan.FromMinutes(30),
                var p when p.Contains("inventory") => TimeSpan.FromMinutes(15),
                var p when p.Contains("sales") => TimeSpan.FromMinutes(10),
                var p when p.Contains("purchases") => TimeSpan.FromMinutes(10),
                _ => TimeSpan.FromMinutes(5)
            };
        }

        private async Task WriteCachedResponseAsync(HttpContext context, object cachedResponse)
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(cachedResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    /// <summary>
    /// API 快取中間件擴展方法
    /// </summary>
    public static class ApiCacheMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiCache(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiCacheMiddleware>();
        }
    }
}