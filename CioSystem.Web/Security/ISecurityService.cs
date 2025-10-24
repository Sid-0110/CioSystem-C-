using System.Security.Claims;

namespace CioSystem.Web.Security
{
    /// <summary>
    /// 安全性服務介面
    /// 提供身份驗證、授權、加密等安全功能
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// 驗證用戶身份
        /// </summary>
        /// <param name="username">用戶名</param>
        /// <param name="password">密碼</param>
        /// <returns>驗證結果</returns>
        Task<AuthenticationResult> AuthenticateAsync(string username, string password);

        /// <summary>
        /// 檢查用戶權限
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="resource">資源</param>
        /// <param name="action">操作</param>
        /// <returns>是否有權限</returns>
        Task<bool> HasPermissionAsync(int userId, string resource, string action);

        /// <summary>
        /// 加密敏感資料
        /// </summary>
        /// <param name="data">原始資料</param>
        /// <returns>加密後的資料</returns>
        string EncryptSensitiveData(string data);

        /// <summary>
        /// 解密敏感資料
        /// </summary>
        /// <param name="encryptedData">加密資料</param>
        /// <returns>解密後的資料</returns>
        string DecryptSensitiveData(string encryptedData);

        /// <summary>
        /// 生成安全令牌
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="claims">聲明</param>
        /// <returns>安全令牌</returns>
        string GenerateSecurityToken(int userId, IEnumerable<Claim> claims);

        /// <summary>
        /// 驗證安全令牌
        /// </summary>
        /// <param name="token">令牌</param>
        /// <returns>驗證結果</returns>
        Task<TokenValidationResult> ValidateTokenAsync(string token);

        /// <summary>
        /// 記錄安全事件
        /// </summary>
        /// <param name="eventType">事件類型</param>
        /// <param name="userId">用戶ID</param>
        /// <param name="details">詳細資訊</param>
        Task LogSecurityEventAsync(SecurityEventType eventType, int? userId, string details);

        /// <summary>
        /// 檢查密碼強度
        /// </summary>
        /// <param name="password">密碼</param>
        /// <returns>密碼強度</returns>
        PasswordStrength CheckPasswordStrength(string password);

        /// <summary>
        /// 生成安全密碼
        /// </summary>
        /// <param name="length">長度</param>
        /// <returns>安全密碼</returns>
        string GenerateSecurePassword(int length = 12);

        /// <summary>
        /// 檢查帳戶安全狀態
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <returns>安全狀態</returns>
        Task<AccountSecurityStatus> CheckAccountSecurityAsync(int userId);
    }

    /// <summary>
    /// 身份驗證結果
    /// </summary>
    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? Token { get; set; }
        public IEnumerable<Claim> Claims { get; set; } = new List<Claim>();
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// 令牌驗證結果
    /// </summary>
    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public IEnumerable<Claim> Claims { get; set; } = new List<Claim>();
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// 密碼強度
    /// </summary>
    public class PasswordStrength
    {
        public int Score { get; set; }
        public string Level { get; set; } = string.Empty;
        public string[] Suggestions { get; set; } = Array.Empty<string>();
        public bool IsStrong { get; set; }
    }

    /// <summary>
    /// 帳戶安全狀態
    /// </summary>
    public class AccountSecurityStatus
    {
        public bool IsSecure { get; set; }
        public string[] SecurityIssues { get; set; } = Array.Empty<string>();
        public DateTime? LastPasswordChange { get; set; }
        public DateTime? LastLogin { get; set; }
        public int FailedLoginAttempts { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LockedUntil { get; set; }
    }

    /// <summary>
    /// 安全事件類型
    /// </summary>
    public enum SecurityEventType
    {
        LoginSuccess,
        LoginFailed,
        PasswordChanged,
        AccountLocked,
        AccountUnlocked,
        PermissionDenied,
        DataAccess,
        DataModification,
        SystemAccess,
        SecurityViolation
    }
}