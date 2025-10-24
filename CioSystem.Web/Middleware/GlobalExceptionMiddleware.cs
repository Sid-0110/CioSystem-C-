using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CioSystem.Web.Middleware
{
    /// <summary>
    /// 全域異常處理中間件
    /// 統一處理應用程式中的未處理異常
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "未處理的異常: {RequestPath}", context.Request.Path);

            var response = context.Response;
            response.ContentType = "application/json";

            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "系統發生錯誤",
                Status = GetStatusCode(exception),
                Instance = context.Request.Path,
                Detail = GetErrorMessage(exception)
            };

            // 在開發環境中提供更多詳細信息
            if (_environment.IsDevelopment())
            {
                problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
                problemDetails.Extensions["exception"] = exception.ToString();
            }

            response.StatusCode = problemDetails.Status ?? 500;

            var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await response.WriteAsync(jsonResponse);
        }

        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => 400,
                ArgumentException => 400,
                InvalidOperationException => 400,
                KeyNotFoundException => 404,
                UnauthorizedAccessException => 401,
                NotImplementedException => 501,
                _ => 500
            };
        }

        private static string GetErrorMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => "必要參數不能為空",
                ArgumentException => "請求參數無效",
                InvalidOperationException => "操作無效",
                KeyNotFoundException => "找不到指定的資源",
                UnauthorizedAccessException => "未授權的訪問",
                NotImplementedException => "功能尚未實現",
                _ => "系統發生錯誤，請稍後再試"
            };
        }
    }

    /// <summary>
    /// 全域異常處理中間件擴展方法
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}