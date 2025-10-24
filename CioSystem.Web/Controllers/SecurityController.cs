using Microsoft.AspNetCore.Mvc;
using CioSystem.Web.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CioSystem.Web.Controllers
{
    /// <summary>
    /// 安全控制器
    /// 提供安全相關的 API 端點
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 需要身份驗證
    public class SecurityController : ControllerBase
    {
        private readonly ISecurityService _securityService;
        private readonly IInputValidationService _inputValidationService;
        private readonly ISecurityLogService _securityLogService;
        private readonly ILogger<SecurityController> _logger;

        public SecurityController(
            ISecurityService securityService,
            IInputValidationService inputValidationService,
            ISecurityLogService securityLogService,
            ILogger<SecurityController> logger)
        {
            _securityService = securityService;
            _inputValidationService = inputValidationService;
            _securityLogService = securityLogService;
            _logger = logger;
        }

        /// <summary>
        /// 用戶登入
        /// </summary>
        /// <param name="request">登入請求</param>
        /// <returns>登入結果</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthenticationResult>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // 驗證輸入
                var validationResult = await _inputValidationService.ValidateAndSanitizeAsync(request.Username, InputType.Text);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult.Errors);
                }

                // 記錄登入嘗試
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers.UserAgent.ToString();

                // 執行身份驗證
                var authResult = await _securityService.AuthenticateAsync(validationResult.SanitizedInput, request.Password);

                // 記錄登入事件
                await _securityLogService.LogLoginEventAsync(
                    authResult.UserId,
                    ipAddress,
                    userAgent,
                    authResult.IsSuccess,
                    authResult.IsSuccess ? null : authResult.Message
                );

                if (authResult.IsSuccess)
                {
                    return Ok(authResult);
                }

                return Unauthorized(authResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登入時發生錯誤");
                return StatusCode(500, "登入時發生內部錯誤");
            }
        }

        /// <summary>
        /// 檢查權限
        /// </summary>
        /// <param name="resource">資源</param>
        /// <param name="action">操作</param>
        /// <returns>權限檢查結果</returns>
        [HttpGet("permission")]
        public async Task<ActionResult<bool>> CheckPermission([FromQuery] string resource, [FromQuery] string action)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized("未登入");
                }

                var hasPermission = await _securityService.HasPermissionAsync(userId.Value, resource, action);
                return Ok(hasPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "權限檢查時發生錯誤");
                return StatusCode(500, "權限檢查時發生內部錯誤");
            }
        }

        /// <summary>
        /// 驗證輸入
        /// </summary>
        /// <param name="request">驗證請求</param>
        /// <returns>驗證結果</returns>
        [HttpPost("validate-input")]
        public async Task<ActionResult<ValidationResult>> ValidateInput([FromBody] InputValidationRequest request)
        {
            try
            {
                var result = await _inputValidationService.ValidateAndSanitizeAsync(request.Input, request.InputType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "輸入驗證時發生錯誤");
                return StatusCode(500, "輸入驗證時發生內部錯誤");
            }
        }

        /// <summary>
        /// 檢查密碼強度
        /// </summary>
        /// <param name="password">密碼</param>
        /// <returns>密碼強度</returns>
        [HttpPost("check-password-strength")]
        public async Task<ActionResult<PasswordStrength>> CheckPasswordStrength([FromBody] string password)
        {
            try
            {
                var strength = _securityService.CheckPasswordStrength(password);
                return Ok(strength);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密碼強度檢查時發生錯誤");
                return StatusCode(500, "密碼強度檢查時發生內部錯誤");
            }
        }

        /// <summary>
        /// 生成安全密碼
        /// </summary>
        /// <param name="length">長度</param>
        /// <returns>安全密碼</returns>
        [HttpGet("generate-password")]
        public async Task<ActionResult<string>> GeneratePassword([FromQuery] int length = 12)
        {
            try
            {
                var password = _securityService.GenerateSecurePassword(length);
                return Ok(password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成密碼時發生錯誤");
                return StatusCode(500, "生成密碼時發生內部錯誤");
            }
        }

        /// <summary>
        /// 檢查帳戶安全狀態
        /// </summary>
        /// <returns>安全狀態</returns>
        [HttpGet("account-security")]
        public async Task<ActionResult<AccountSecurityStatus>> GetAccountSecurity()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized("未登入");
                }

                var status = await _securityService.CheckAccountSecurityAsync(userId.Value);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查帳戶安全狀態時發生錯誤");
                return StatusCode(500, "檢查帳戶安全狀態時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得安全日誌
        /// </summary>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <param name="eventType">事件類型</param>
        /// <param name="userId">用戶ID</param>
        /// <returns>安全日誌</returns>
        [HttpGet("logs")]
        public async Task<ActionResult<IEnumerable<SecurityLogEvent>>> GetSecurityLogs(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] SecurityEventType? eventType = null,
            [FromQuery] int? userId = null)
        {
            try
            {
                var logs = await _securityLogService.GetSecurityLogsAsync(startDate, endDate, eventType, userId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得安全日誌時發生錯誤");
                return StatusCode(500, "取得安全日誌時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得安全統計
        /// </summary>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <returns>安全統計</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<SecurityStatistics>> GetSecurityStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var statistics = await _securityLogService.GetSecurityStatisticsAsync(startDate, endDate);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得安全統計時發生錯誤");
                return StatusCode(500, "取得安全統計時發生內部錯誤");
            }
        }

        /// <summary>
        /// 檢查異常活動
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="ipAddress">IP地址</param>
        /// <returns>異常活動報告</returns>
        [HttpGet("anomalies")]
        public async Task<ActionResult<AnomalyReport>> CheckAnomalies(
            [FromQuery] int? userId = null,
            [FromQuery] string? ipAddress = null)
        {
            try
            {
                var report = await _securityLogService.CheckAnomalousActivityAsync(userId, ipAddress);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查異常活動時發生錯誤");
                return StatusCode(500, "檢查異常活動時發生內部錯誤");
            }
        }

        /// <summary>
        /// 記錄安全事件
        /// </summary>
        /// <param name="event">安全事件</param>
        /// <returns>操作結果</returns>
        [HttpPost("log-event")]
        public async Task<ActionResult> LogSecurityEvent([FromBody] SecurityLogEvent @event)
        {
            try
            {
                await _securityLogService.LogSecurityEventAsync(@event);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄安全事件時發生錯誤");
                return StatusCode(500, "記錄安全事件時發生內部錯誤");
            }
        }

        // 私有輔助方法
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
        }

        private string GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }

    // 請求模型
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class InputValidationRequest
    {
        public string Input { get; set; } = string.Empty;
        public InputType InputType { get; set; }
    }
}