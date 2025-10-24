using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace CioSystem.Web.Security
{
    /// <summary>
    /// 輸入驗證服務實現
    /// 提供輸入驗證、清理和防護功能
    /// </summary>
    public class InputValidationService : IInputValidationService
    {
        private readonly ILogger<InputValidationService> _logger;
        private readonly string[] _allowedFileExtensions;
        private readonly long _maxFileSize;
        private readonly string[] _dangerousPatterns;

        public InputValidationService(ILogger<InputValidationService> logger)
        {
            _logger = logger;
            _allowedFileExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
            _maxFileSize = 10 * 1024 * 1024; // 10MB
            _dangerousPatterns = new[]
            {
                @"<script[^>]*>.*?</script>",
                @"javascript:",
                @"vbscript:",
                @"onload\s*=",
                @"onerror\s*=",
                @"onclick\s*=",
                @"onmouseover\s*=",
                @"<iframe[^>]*>",
                @"<object[^>]*>",
                @"<embed[^>]*>",
                @"<link[^>]*>",
                @"<meta[^>]*>",
                @"<style[^>]*>",
                @"expression\s*\(",
                @"url\s*\(",
                @"@import",
                @"eval\s*\(",
                @"exec\s*\(",
                @"system\s*\(",
                @"shell_exec\s*\(",
                @"passthru\s*\(",
                @"proc_open\s*\(",
                @"popen\s*\(",
                @"file_get_contents\s*\(",
                @"file_put_contents\s*\(",
                @"fopen\s*\(",
                @"fwrite\s*\(",
                @"fread\s*\(",
                @"include\s*\(",
                @"require\s*\(",
                @"include_once\s*\(",
                @"require_once\s*\("
            };
        }

        public async Task<ValidationResult> ValidateAndSanitizeAsync(string input, InputType inputType)
        {
            try
            {
                if (string.IsNullOrEmpty(input))
                {
                    return new ValidationResult
                    {
                        IsValid = true,
                        SanitizedInput = string.Empty
                    };
                }

                var threats = new List<SecurityThreat>();
                var errors = new List<string>();
                var warnings = new List<string>();
                var sanitizedInput = input;

                // 檢查各種安全威脅
                if (ContainsSqlInjection(input))
                {
                    threats.Add(new SecurityThreat
                    {
                        Type = ThreatType.SqlInjection,
                        Description = "檢測到 SQL 注入攻擊",
                        Severity = "高",
                        Pattern = "SQL 注入模式"
                    });
                }

                if (ContainsXss(input))
                {
                    threats.Add(new SecurityThreat
                    {
                        Type = ThreatType.Xss,
                        Description = "檢測到 XSS 攻擊",
                        Severity = "高",
                        Pattern = "XSS 模式"
                    });
                }

                // 根據輸入類型進行特定驗證
                switch (inputType)
                {
                    case InputType.Email:
                        if (!IsValidEmail(input))
                        {
                            errors.Add("無效的電子郵件格式");
                        }
                        break;

                    case InputType.Url:
                        if (!IsValidUrl(input))
                        {
                            errors.Add("無效的 URL 格式");
                        }
                        break;

                    case InputType.Phone:
                        if (!IsValidPhone(input))
                        {
                            errors.Add("無效的電話號碼格式");
                        }
                        break;

                    case InputType.Html:
                        sanitizedInput = SanitizeHtml(input);
                        if (sanitizedInput != input)
                        {
                            warnings.Add("HTML 內容已被清理");
                        }
                        break;

                    case InputType.Path:
                        if (!IsSafePath(input))
                        {
                            errors.Add("不安全的檔案路徑");
                        }
                        break;
                }

                // 清理輸入
                if (threats.Count == 0)
                {
                    sanitizedInput = HttpUtility.HtmlEncode(sanitizedInput);
                }
                else
                {
                    // 如果檢測到威脅，進行更嚴格的清理
                    sanitizedInput = SanitizeInput(sanitizedInput);
                }

                // 記錄安全威脅
                if (threats.Count > 0)
                {
                    _logger.LogWarning("檢測到安全威脅: {Threats}", string.Join(", ", threats.Select(t => t.Type)));
                }

                return new ValidationResult
                {
                    IsValid = errors.Count == 0,
                    SanitizedInput = sanitizedInput,
                    Errors = errors.ToArray(),
                    Warnings = warnings.ToArray(),
                    Threats = threats.ToArray()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "輸入驗證時發生錯誤: {Input}", input);
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new[] { "輸入驗證時發生錯誤" }
                };
            }
        }

        public bool ContainsSqlInjection(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            var sqlPatterns = new[]
            {
                @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE)\b)",
                @"(\b(UNION|OR|AND)\b.*\b(SELECT|INSERT|UPDATE|DELETE)\b)",
                @"(\b(UNION|OR|AND)\b.*\b(SELECT|INSERT|UPDATE|DELETE)\b)",
                @"(--|#|\/\*|\*\/)",
                @"(\b(WAITFOR|DELAY|BENCHMARK)\b)",
                @"(\b(CHAR|ASCII|SUBSTRING|LENGTH)\b)",
                @"(\b(INFORMATION_SCHEMA|SYSOBJECTS|SYSCOLUMNS)\b)",
                @"(\b(CAST|CONVERT|ISNULL|COALESCE)\b)",
                @"(\b(SP_|XP_)\w+)",
                @"(\b(OPENROWSET|OPENDATASOURCE)\b)",
                @"(\b(BULK|BULKINSERT)\b)",
                @"(\b(LOAD_FILE|INTO\s+OUTFILE|INTO\s+DUMPFILE)\b)",
                @"(\b(GRANT|REVOKE|DENY)\b)",
                @"(\b(BACKUP|RESTORE)\b)",
                @"(\b(SHUTDOWN|RESTART)\b)"
            };

            return sqlPatterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        public bool ContainsXss(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            return _dangerousPatterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        public string SanitizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            // 移除危險標籤和屬性
            var dangerousTags = new[] { "script", "iframe", "object", "embed", "link", "meta", "style" };
            var dangerousAttributes = new[] { "onload", "onerror", "onclick", "onmouseover", "onfocus", "onblur" };

            var sanitized = html;

            // 移除危險標籤
            foreach (var tag in dangerousTags)
            {
                var pattern = $@"<{tag}[^>]*>.*?</{tag}>";
                sanitized = Regex.Replace(sanitized, pattern, "", RegexOptions.IgnoreCase);
            }

            // 移除危險屬性
            foreach (var attr in dangerousAttributes)
            {
                var pattern = $@"\s{attr}\s*=\s*[""'][^""']*[""']";
                sanitized = Regex.Replace(sanitized, pattern, "", RegexOptions.IgnoreCase);
            }

            // 移除 javascript: 和 vbscript: 協議
            sanitized = Regex.Replace(sanitized, @"javascript:", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"vbscript:", "", RegexOptions.IgnoreCase);

            return sanitized;
        }

        public async Task<FileValidationResult> ValidateFileUploadAsync(string fileName, long fileSize, string contentType)
        {
            try
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                // 檢查檔案名稱
                if (string.IsNullOrEmpty(fileName))
                {
                    errors.Add("檔案名稱不能為空");
                    return new FileValidationResult
                    {
                        IsValid = false,
                        Errors = errors.ToArray()
                    };
                }

                // 生成安全檔案名稱
                var safeFileName = GenerateSafeFileName(fileName);

                // 檢查檔案大小
                if (fileSize > _maxFileSize)
                {
                    errors.Add($"檔案大小超過限制 ({_maxFileSize / 1024 / 1024}MB)");
                }

                // 檢查檔案擴展名
                var extension = Path.GetExtension(fileName).ToLower();
                if (!_allowedFileExtensions.Contains(extension))
                {
                    errors.Add($"不支援的檔案類型: {extension}");
                }

                // 檢查檔案名稱中的危險字符
                if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                {
                    errors.Add("檔案名稱包含不安全的字符");
                }

                // 檢查內容類型
                var allowedContentTypes = new[]
                {
                    "image/jpeg", "image/png", "image/gif", "application/pdf",
                    "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                };

                if (!allowedContentTypes.Contains(contentType))
                {
                    warnings.Add($"未識別的內容類型: {contentType}");
                }

                return new FileValidationResult
                {
                    IsValid = errors.Count == 0,
                    SafeFileName = safeFileName,
                    Errors = errors.ToArray(),
                    Warnings = warnings.ToArray()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案驗證時發生錯誤: {FileName}", fileName);
                return new FileValidationResult
                {
                    IsValid = false,
                    Errors = new[] { "檔案驗證時發生錯誤" }
                };
            }
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;

            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, emailPattern);
        }

        public bool IsValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public bool IsValidPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return false;

            var phonePattern = @"^[\+]?[1-9][\d]{0,15}$";
            return Regex.IsMatch(phone.Replace(" ", "").Replace("-", ""), phonePattern);
        }

        public string GenerateSafeFileName(string originalFileName)
        {
            if (string.IsNullOrEmpty(originalFileName)) return "file";

            // 移除路徑分隔符和危險字符
            var safeName = Path.GetFileName(originalFileName);
            safeName = Regex.Replace(safeName, @"[^\w\-_\.]", "_");
            safeName = Regex.Replace(safeName, @"_{2,}", "_");

            // 確保檔案名稱不為空
            if (string.IsNullOrEmpty(safeName) || safeName == ".")
            {
                safeName = "file";
            }

            // 添加時間戳避免衝突
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var extension = Path.GetExtension(safeName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(safeName);

            return $"{nameWithoutExtension}_{timestamp}{extension}";
        }

        public bool IsSafePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            // 檢查路徑遍歷攻擊
            if (path.Contains("..") || path.Contains("~"))
            {
                return false;
            }

            // 檢查絕對路徑
            if (Path.IsPathRooted(path))
            {
                return false;
            }

            // 檢查危險字符
            var dangerousChars = new[] { "<", ">", ":", "\"", "|", "?", "*" };
            if (dangerousChars.Any(c => path.Contains(c)))
            {
                return false;
            }

            return true;
        }

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // 移除所有 HTML 標籤
            var sanitized = Regex.Replace(input, @"<[^>]*>", "");

            // 編碼特殊字符
            sanitized = HttpUtility.HtmlEncode(sanitized);

            // 移除多餘的空白字符
            sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();

            return sanitized;
        }
    }
}