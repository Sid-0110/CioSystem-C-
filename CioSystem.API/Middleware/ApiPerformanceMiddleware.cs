using CioSystem.API.Services;
using System.Text;

namespace CioSystem.API.Middleware
{
    /// <summary>
    /// API 效能中間件
    /// 自動追蹤 API 請求效能和響應
    /// </summary>
    public class ApiPerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiPerformanceMiddleware> _logger;
        private readonly IApiPerformanceService _performanceService;

        public ApiPerformanceMiddleware(
            RequestDelegate next,
            ILogger<ApiPerformanceMiddleware> logger,
            IApiPerformanceService performanceService)
        {
            _next = next;
            _logger = logger;
            _performanceService = performanceService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = Guid.NewGuid().ToString();
            var controllerName = GetControllerName(context);
            var actionName = GetActionName(context);

            // 開始追蹤請求
            using var tracker = _performanceService.StartRequestTracking(controllerName, actionName, requestId);

            // 包裝響應流以追蹤響應大小
            var originalResponseBody = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            finally
            {
                // 計算響應大小
                var responseSize = responseBody.Length;

                // 將響應寫回原始流
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;

                // 記錄請求完成
                await _performanceService.RecordRequestCompletionAsync(
                    requestId,
                    context.Response.StatusCode,
                    responseSize);

                // 記錄效能資訊
                LogPerformanceInfo(context, requestId, responseSize);
            }
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

        private void LogPerformanceInfo(HttpContext context, string requestId, long responseSize)
        {
            var method = context.Request.Method;
            var path = context.Request.Path;
            var statusCode = context.Response.StatusCode;
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var clientIp = GetClientIpAddress(context);

            _logger.LogInformation(
                "API 請求完成 - ID: {RequestId}, 方法: {Method}, 路徑: {Path}, 狀態: {StatusCode}, 大小: {Size}bytes, IP: {ClientIp}, UA: {UserAgent}",
                requestId, method, path, statusCode, responseSize, clientIp, userAgent);
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }

    /// <summary>
    /// API 效能中間件擴展方法
    /// </summary>
    public static class ApiPerformanceMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiPerformance(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiPerformanceMiddleware>();
        }
    }
}