using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;

namespace CioSystem.Web.Security
{
    /// <summary>
    /// 安全性服務實現
    /// 提供身份驗證、授權、加密等安全功能
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private readonly ILogger<SecurityService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _encryptionKey;
        private readonly string _jwtSecret;
        private readonly int _jwtExpirationMinutes;

        public SecurityService(ILogger<SecurityService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _encryptionKey = _configuration["Security:EncryptionKey"] ?? GenerateEncryptionKey();
            _jwtSecret = _configuration["Security:JwtSecret"] ?? GenerateJwtSecret();
            _jwtExpirationMinutes = int.Parse(_configuration["Security:JwtExpirationMinutes"] ?? "60");
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            try
            {
                // 記錄登入嘗試
                await LogSecurityEventAsync(SecurityEventType.LoginSuccess, null, $"登入嘗試: {username}");

                // 這裡應該與實際的用戶資料庫進行驗證
                // 目前使用模擬驗證
                if (IsValidUser(username, password))
                {
                    var userId = GetUserIdByUsername(username);
                    var claims = GetUserClaims(userId);
                    var token = GenerateSecurityToken(userId, claims);

                    return new AuthenticationResult
                    {
                        IsSuccess = true,
                        Message = "登入成功",
                        UserId = userId,
                        Token = token,
                        Claims = claims,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes)
                    };
                }

                await LogSecurityEventAsync(SecurityEventType.LoginFailed, null, $"登入失敗: {username}");
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "用戶名或密碼錯誤"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "身份驗證時發生錯誤: {Username}", username);
                await LogSecurityEventAsync(SecurityEventType.SecurityViolation, null, $"身份驗證錯誤: {ex.Message}");

                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "身份驗證時發生錯誤"
                };
            }
        }

        public async Task<bool> HasPermissionAsync(int userId, string resource, string action)
        {
            try
            {
                // 這裡應該檢查用戶的實際權限
                // 目前使用模擬權限檢查
                var userRole = GetUserRole(userId);

                // 管理員擁有所有權限
                if (userRole == "Admin")
                {
                    return true;
                }

                // 根據角色和資源檢查權限
                var hasPermission = CheckResourcePermission(userRole, resource, action);

                if (!hasPermission)
                {
                    await LogSecurityEventAsync(SecurityEventType.PermissionDenied, userId, $"權限拒絕: {resource}/{action}");
                }

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "權限檢查時發生錯誤: UserId={UserId}, Resource={Resource}, Action={Action}",
                    userId, resource, action);
                return false;
            }
        }

        public string EncryptSensitiveData(string data)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16]; // 在實際應用中應該使用隨機 IV

                using var encryptor = aes.CreateEncryptor();
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料加密時發生錯誤");
                throw new InvalidOperationException("資料加密失敗", ex);
            }
        }

        public string DecryptSensitiveData(string encryptedData)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16]; // 在實際應用中應該使用隨機 IV

                using var decryptor = aes.CreateDecryptor();
                var encryptedBytes = Convert.FromBase64String(encryptedData);
                var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料解密時發生錯誤");
                throw new InvalidOperationException("資料解密失敗", ex);
            }
        }

        public string GenerateSecurityToken(int userId, IEnumerable<Claim> claims)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: "CioSystem",
                    audience: "CioSystemUsers",
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成安全令牌時發生錯誤: UserId={UserId}", userId);
                throw new InvalidOperationException("令牌生成失敗", ex);
            }
        }

        public async Task<TokenValidationResult> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "CioSystem",
                    ValidateAudience = true,
                    ValidAudience = "CioSystemUsers",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var jwtToken = (JwtSecurityToken)validatedToken;

                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                var userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;

                return new TokenValidationResult
                {
                    IsValid = true,
                    Message = "令牌有效",
                    UserId = userId,
                    Claims = principal.Claims,
                    ExpiresAt = jwtToken.ValidTo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "令牌驗證時發生錯誤");
                return new TokenValidationResult
                {
                    IsValid = false,
                    Message = "令牌無效或已過期"
                };
            }
        }

        public async Task LogSecurityEventAsync(SecurityEventType eventType, int? userId, string details)
        {
            try
            {
                var logMessage = $"安全事件: {eventType}, 用戶: {userId}, 詳情: {details}, 時間: {DateTime.UtcNow}";
                _logger.LogInformation(logMessage);

                // 這裡可以將安全事件記錄到專門的安全日誌系統
                // 例如：資料庫、檔案系統、外部安全系統等
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄安全事件時發生錯誤: {EventType}, {UserId}, {Details}",
                    eventType, userId, details);
            }
        }

        public PasswordStrength CheckPasswordStrength(string password)
        {
            var score = 0;
            var suggestions = new List<string>();

            // 長度檢查
            if (password.Length >= 8) score += 1;
            else suggestions.Add("密碼長度至少 8 個字符");

            if (password.Length >= 12) score += 1;

            // 字符類型檢查
            if (Regex.IsMatch(password, @"[a-z]")) score += 1;
            else suggestions.Add("包含小寫字母");

            if (Regex.IsMatch(password, @"[A-Z]")) score += 1;
            else suggestions.Add("包含大寫字母");

            if (Regex.IsMatch(password, @"[0-9]")) score += 1;
            else suggestions.Add("包含數字");

            if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) score += 1;
            else suggestions.Add("包含特殊字符");

            // 常見密碼檢查
            var commonPasswords = new[] { "password", "123456", "admin", "qwerty", "letmein" };
            if (commonPasswords.Contains(password.ToLower()))
            {
                score = Math.Max(0, score - 2);
                suggestions.Add("避免使用常見密碼");
            }

            var level = score switch
            {
                >= 5 => "強",
                >= 3 => "中等",
                >= 1 => "弱",
                _ => "極弱"
            };

            return new PasswordStrength
            {
                Score = score,
                Level = level,
                Suggestions = suggestions.ToArray(),
                IsStrong = score >= 4
            };
        }

        public string GenerateSecurePassword(int length = 12)
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";
            const string allChars = lowercase + uppercase + digits + symbols;

            using var rng = RandomNumberGenerator.Create();
            var password = new StringBuilder();

            // 確保至少包含每種類型的字符
            password.Append(GetRandomChar(lowercase, rng));
            password.Append(GetRandomChar(uppercase, rng));
            password.Append(GetRandomChar(digits, rng));
            password.Append(GetRandomChar(symbols, rng));

            // 填充剩餘長度
            for (int i = 4; i < length; i++)
            {
                password.Append(GetRandomChar(allChars, rng));
            }

            // 打亂字符順序
            var passwordArray = password.ToString().ToCharArray();
            for (int i = passwordArray.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(0, i + 1);
                (passwordArray[i], passwordArray[j]) = (passwordArray[j], passwordArray[i]);
            }

            return new string(passwordArray);
        }

        public async Task<AccountSecurityStatus> CheckAccountSecurityAsync(int userId)
        {
            try
            {
                // 這裡應該從實際的用戶資料庫獲取安全資訊
                // 目前使用模擬資料
                var lastPasswordChange = DateTime.UtcNow.AddDays(-30); // 模擬 30 天前
                var lastLogin = DateTime.UtcNow.AddHours(-2); // 模擬 2 小時前
                var failedAttempts = 0; // 模擬無失敗嘗試
                var isLocked = false;

                var issues = new List<string>();

                // 檢查密碼年齡
                if (lastPasswordChange < DateTime.UtcNow.AddDays(-90))
                {
                    issues.Add("密碼已超過 90 天未更換");
                }

                // 檢查登入頻率
                if (lastLogin < DateTime.UtcNow.AddDays(-30))
                {
                    issues.Add("帳戶已超過 30 天未登入");
                }

                // 檢查失敗嘗試
                if (failedAttempts >= 5)
                {
                    issues.Add("帳戶因多次登入失敗被鎖定");
                    isLocked = true;
                }

                return new AccountSecurityStatus
                {
                    IsSecure = issues.Count == 0,
                    SecurityIssues = issues.ToArray(),
                    LastPasswordChange = lastPasswordChange,
                    LastLogin = lastLogin,
                    FailedLoginAttempts = failedAttempts,
                    IsLocked = isLocked
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查帳戶安全狀態時發生錯誤: UserId={UserId}", userId);
                return new AccountSecurityStatus
                {
                    IsSecure = false,
                    SecurityIssues = new[] { "無法檢查安全狀態" }
                };
            }
        }

        // 私有輔助方法
        private bool IsValidUser(string username, string password)
        {
            // 模擬用戶驗證 - 在實際應用中應該查詢資料庫
            return username == "admin" && password == "admin123";
        }

        private int GetUserIdByUsername(string username)
        {
            // 模擬用戶 ID 查詢
            return 1;
        }

        private IEnumerable<Claim> GetUserClaims(int userId)
        {
            return new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
        }

        private string GetUserRole(int userId)
        {
            // 模擬角色查詢
            return "Admin";
        }

        private bool CheckResourcePermission(string role, string resource, string action)
        {
            // 模擬權限檢查
            return role == "Admin" || (role == "User" && action == "Read");
        }

        private char GetRandomChar(string chars, RandomNumberGenerator rng)
        {
            var index = RandomNumberGenerator.GetInt32(0, chars.Length);
            return chars[index];
        }

        private string GenerateEncryptionKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var keyBytes = new byte[32];
            rng.GetBytes(keyBytes);
            return Convert.ToBase64String(keyBytes);
        }

        private string GenerateJwtSecret()
        {
            using var rng = RandomNumberGenerator.Create();
            var secretBytes = new byte[64];
            rng.GetBytes(secretBytes);
            return Convert.ToBase64String(secretBytes);
        }
    }
}