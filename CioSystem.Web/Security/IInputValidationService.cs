namespace CioSystem.Web.Security
{
    /// <summary>
    /// 輸入驗證服務介面
    /// 提供輸入驗證、清理和防護功能
    /// </summary>
    public interface IInputValidationService
    {
        /// <summary>
        /// 驗證和清理用戶輸入
        /// </summary>
        /// <param name="input">原始輸入</param>
        /// <param name="inputType">輸入類型</param>
        /// <returns>驗證結果</returns>
        Task<ValidationResult> ValidateAndSanitizeAsync(string input, InputType inputType);

        /// <summary>
        /// 檢查 SQL 注入攻擊
        /// </summary>
        /// <param name="input">輸入字串</param>
        /// <returns>是否包含 SQL 注入</returns>
        bool ContainsSqlInjection(string input);

        /// <summary>
        /// 檢查 XSS 攻擊
        /// </summary>
        /// <param name="input">輸入字串</param>
        /// <returns>是否包含 XSS</returns>
        bool ContainsXss(string input);

        /// <summary>
        /// 清理 HTML 內容
        /// </summary>
        /// <param name="html">HTML 內容</param>
        /// <returns>清理後的 HTML</returns>
        string SanitizeHtml(string html);

        /// <summary>
        /// 驗證檔案上傳
        /// </summary>
        /// <param name="fileName">檔案名稱</param>
        /// <param name="fileSize">檔案大小</param>
        /// <param name="contentType">內容類型</param>
        /// <returns>驗證結果</returns>
        Task<FileValidationResult> ValidateFileUploadAsync(string fileName, long fileSize, string contentType);

        /// <summary>
        /// 驗證電子郵件地址
        /// </summary>
        /// <param name="email">電子郵件</param>
        /// <returns>是否有效</returns>
        bool IsValidEmail(string email);

        /// <summary>
        /// 驗證 URL
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns>是否有效</returns>
        bool IsValidUrl(string url);

        /// <summary>
        /// 驗證電話號碼
        /// </summary>
        /// <param name="phone">電話號碼</param>
        /// <returns>是否有效</returns>
        bool IsValidPhone(string phone);

        /// <summary>
        /// 生成安全的檔案名稱
        /// </summary>
        /// <param name="originalFileName">原始檔案名稱</param>
        /// <returns>安全檔案名稱</returns>
        string GenerateSafeFileName(string originalFileName);

        /// <summary>
        /// 檢查路徑遍歷攻擊
        /// </summary>
        /// <param name="path">路徑</param>
        /// <returns>是否安全</returns>
        bool IsSafePath(string path);
    }

    /// <summary>
    /// 驗證結果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string SanitizedInput { get; set; } = string.Empty;
        public string[] Errors { get; set; } = Array.Empty<string>();
        public string[] Warnings { get; set; } = Array.Empty<string>();
        public SecurityThreat[] Threats { get; set; } = Array.Empty<SecurityThreat>();
    }

    /// <summary>
    /// 檔案驗證結果
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string SafeFileName { get; set; } = string.Empty;
        public string[] Errors { get; set; } = Array.Empty<string>();
        public string[] Warnings { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// 安全威脅
    /// </summary>
    public class SecurityThreat
    {
        public ThreatType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
    }

    /// <summary>
    /// 威脅類型
    /// </summary>
    public enum ThreatType
    {
        SqlInjection,
        Xss,
        PathTraversal,
        CommandInjection,
        LdapInjection,
        XPathInjection,
        XmlInjection,
        CssInjection,
        HtmlInjection,
        ScriptInjection
    }

    /// <summary>
    /// 輸入類型
    /// </summary>
    public enum InputType
    {
        Text,
        Html,
        Email,
        Url,
        Phone,
        Number,
        Date,
        FileName,
        Path,
        Search,
        Comment,
        Description
    }
}