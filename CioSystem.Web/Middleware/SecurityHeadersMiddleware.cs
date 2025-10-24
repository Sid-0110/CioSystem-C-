using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CioSystem.Web.Middleware
{
    /// <summary>
    /// 安全標頭中間件
    /// 添加各種安全標頭以保護應用程式
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;
        private readonly SecurityHeadersOptions _options;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            ILogger<SecurityHeadersMiddleware> logger,
            SecurityHeadersOptions options)
        {
            _next = next;
            _logger = logger;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // 添加安全標頭
                AddSecurityHeaders(context);

                // 添加內容安全策略
                AddContentSecurityPolicy(context);

                // 添加其他安全標頭
                AddAdditionalSecurityHeaders(context);

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全標頭中間件發生錯誤");
                throw;
            }
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var response = context.Response;

            // X-Content-Type-Options: 防止 MIME 類型嗅探
            if (_options.EnableContentTypeOptions)
            {
                response.Headers.Append("X-Content-Type-Options", "nosniff");
            }

            // X-Frame-Options: 防止點擊劫持
            if (_options.EnableFrameOptions)
            {
                response.Headers.Append("X-Frame-Options", _options.FrameOptions);
            }

            // X-XSS-Protection: XSS 防護
            if (_options.EnableXssProtection)
            {
                response.Headers.Append("X-XSS-Protection", "1; mode=block");
            }

            // Referrer-Policy: 控制引用資訊
            if (_options.EnableReferrerPolicy)
            {
                response.Headers.Append("Referrer-Policy", _options.ReferrerPolicy);
            }

            // Permissions-Policy: 控制瀏覽器功能
            if (_options.EnablePermissionsPolicy)
            {
                response.Headers.Append("Permissions-Policy", _options.PermissionsPolicy);
            }

            // Strict-Transport-Security: HTTPS 強制
            if (_options.EnableHsts && context.Request.IsHttps)
            {
                response.Headers.Append("Strict-Transport-Security",
                    $"max-age={_options.HstsMaxAge}; includeSubDomains");
            }
        }

        private void AddContentSecurityPolicy(HttpContext context)
        {
            // 完全禁用 CSP，不添加任何 CSP 標頭
            return;
        }

        private string BuildContentSecurityPolicy()
        {
            var directives = new List<string>();

            // 預設來源
            if (!string.IsNullOrEmpty(_options.CspDefaultSrc))
            {
                directives.Add($"default-src {_options.CspDefaultSrc}");
            }

            // 腳本來源
            if (!string.IsNullOrEmpty(_options.CspScriptSrc))
            {
                directives.Add($"script-src {_options.CspScriptSrc}");
                directives.Add($"script-src-elem {_options.CspScriptSrc}");
            }

            // 樣式來源
            if (!string.IsNullOrEmpty(_options.CspStyleSrc))
            {
                directives.Add($"style-src {_options.CspStyleSrc}");
                directives.Add($"style-src-elem {_options.CspStyleSrc}");
            }

            // 圖片來源
            if (!string.IsNullOrEmpty(_options.CspImgSrc))
            {
                directives.Add($"img-src {_options.CspImgSrc}");
            }

            // 字體來源
            if (!string.IsNullOrEmpty(_options.CspFontSrc))
            {
                directives.Add($"font-src {_options.CspFontSrc}");
            }

            // 連接來源
            if (!string.IsNullOrEmpty(_options.CspConnectSrc))
            {
                directives.Add($"connect-src {_options.CspConnectSrc}");
            }

            // 媒體來源
            if (!string.IsNullOrEmpty(_options.CspMediaSrc))
            {
                directives.Add($"media-src {_options.CspMediaSrc}");
            }

            // 物件來源
            if (!string.IsNullOrEmpty(_options.CspObjectSrc))
            {
                directives.Add($"object-src {_options.CspObjectSrc}");
            }

            // 子資源完整性
            if (_options.EnableSri)
            {
                directives.Add("require-sri-for script style");
            }

            return string.Join("; ", directives);
        }

        private void AddAdditionalSecurityHeaders(HttpContext context)
        {
            var response = context.Response;

            // Server: 隱藏伺服器資訊
            if (_options.HideServerHeader)
            {
                response.Headers.Remove("Server");
            }

            // X-Powered-By: 移除框架資訊
            if (_options.RemovePoweredByHeader)
            {
                response.Headers.Remove("X-Powered-By");
            }

            // Cache-Control: 控制快取
            if (!string.IsNullOrEmpty(_options.CacheControl))
            {
                response.Headers.Append("Cache-Control", _options.CacheControl);
            }

            // X-Download-Options: 防止檔案下載執行
            if (_options.EnableDownloadOptions)
            {
                response.Headers.Append("X-Download-Options", "noopen");
            }

            // X-DNS-Prefetch-Control: 控制 DNS 預取
            if (_options.EnableDnsPrefetchControl)
            {
                response.Headers.Append("X-DNS-Prefetch-Control", "off");
            }
        }
    }

    /// <summary>
    /// 安全標頭選項
    /// </summary>
    public class SecurityHeadersOptions
    {
        // 基本安全標頭
        public bool EnableContentTypeOptions { get; set; } = true;
        public bool EnableFrameOptions { get; set; } = true;
        public string FrameOptions { get; set; } = "DENY";
        public bool EnableXssProtection { get; set; } = true;
        public bool EnableReferrerPolicy { get; set; } = true;
        public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
        public bool EnablePermissionsPolicy { get; set; } = true;
        public string PermissionsPolicy { get; set; } = "geolocation=(), microphone=(), camera=()";
        public bool EnableHsts { get; set; } = true;
        public int HstsMaxAge { get; set; } = 31536000; // 1年

        // 內容安全策略
        public bool EnableCsp { get; set; } = true;
        public string CspDefaultSrc { get; set; } = "'self'";
        public string CspScriptSrc { get; set; } = "'self' 'unsafe-inline' 'unsafe-eval'";
        public string CspStyleSrc { get; set; } = "'self' 'unsafe-inline' https://fonts.googleapis.com";
        public string CspImgSrc { get; set; } = "'self' data: https:";
        public string CspFontSrc { get; set; } = "'self' https://fonts.gstatic.com";
        public string CspConnectSrc { get; set; } = "'self'";
        public string CspMediaSrc { get; set; } = "'self'";
        public string CspObjectSrc { get; set; } = "'none'";

        // 子資源完整性
        public bool EnableSri { get; set; } = false;

        // 其他標頭
        public bool HideServerHeader { get; set; } = true;
        public bool RemovePoweredByHeader { get; set; } = true;
        public string CacheControl { get; set; } = "no-cache, no-store, must-revalidate";
        public bool EnableDownloadOptions { get; set; } = true;
        public bool EnableDnsPrefetchControl { get; set; } = true;
    }

    /// <summary>
    /// 安全標頭中間件擴展方法
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseSecurityHeaders(new SecurityHeadersOptions());
        }

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder, SecurityHeadersOptions options)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>(options);
        }
    }
}