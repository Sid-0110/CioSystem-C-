using CioSystem.Models;
using CioSystem.Services.Authentication;
using CioSystem.Services.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CioSystem.Web.Hubs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CioSystem.Web.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IUserService _userService;
        private readonly ISystemLogService _systemLogService;
        // SignalR 已禁用
        // private readonly IHubContext<DashboardHub> _hubContext;

        public AuthController(
            ILogger<AuthController> logger,
            IUserService userService,
            ISystemLogService systemLogService) : base(logger)
        {
            _userService = userService;
            _systemLogService = systemLogService;
            // SignalR 已禁用
            // _hubContext = hubContext;
        }

        /// <summary>
        /// 顯示登入頁面
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            // 如果已經登入，重定向到首頁
            if (IsUserLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        /// <summary>
        /// 處理登入請求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(CioSystem.Models.LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "請檢查輸入的資料";
                    return View();
                }

                var result = await _userService.LoginAsync(request);
                
                if (result.Success)
                {
                    // 設置會話
                    HttpContext.Session.SetString("SessionId", result.SessionId!);
                    HttpContext.Session.SetString("UserId", result.User!.Id.ToString());
                    HttpContext.Session.SetString("Username", result.User.Username);
                    HttpContext.Session.SetString("Role", result.User.Role);

                    // 設置認證 Cookie
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, result.User.Username),
                        new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
                        new Claim(ClaimTypes.Role, result.User.Role),
                        new Claim("SessionId", result.SessionId!)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    // 記錄登入日誌
                    await _systemLogService.LogAsync("Info", $"用戶登入成功: {result.User.Username} ({result.User.Role})", result.User.Username);

                    TempData["SuccessMessage"] = "登入成功！";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登入時發生錯誤");
                await _systemLogService.LogAsync("Error", $"登入系統錯誤: {ex.Message}", "System");
                TempData["ErrorMessage"] = "登入時發生錯誤，請稍後再試";
                return View();
            }
        }

        /// <summary>
        /// 處理登出請求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var sessionId = HttpContext.Session.GetString("SessionId");
                var username = HttpContext.Session.GetString("Username");

                if (!string.IsNullOrEmpty(sessionId))
                {
                    await _userService.LogoutAsync(sessionId);
                    if (!string.IsNullOrEmpty(username))
                    {
                        await _systemLogService.LogAsync("Info", $"用戶登出: {username}", username);
                    }
                }

                // 清除會話
                HttpContext.Session.Clear();

                // 清除認證 Cookie
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                TempData["SuccessMessage"] = "已成功登出";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出時發生錯誤");
                TempData["ErrorMessage"] = "登出時發生錯誤";
                return RedirectToAction("Login");
            }
        }

        /// <summary>
        /// 處理註冊請求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "請檢查輸入的資料";
                    return View("Login");
                }

                // 檢查密碼確認
                if (request.Password != request.ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "密碼與確認密碼不一致";
                    return View("Login");
                }

                // 創建用戶
                var createRequest = new CreateUserRequest
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = request.Password,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Role = request.Role,
                    IsActive = true
                };

                var result = await _userService.CreateUserAsync(createRequest);
                
                if (result != null)
                {
                    // 記錄註冊日誌
                    await _systemLogService.LogAsync("Info", $"新用戶註冊成功: {result.Username} ({result.Role})", "System");
                    
                    TempData["SuccessMessage"] = "註冊成功！請使用新帳號登入";
                    return RedirectToAction("Login");
                }
                else
                {
                    TempData["ErrorMessage"] = "註冊失敗，請稍後再試";
                    return View("Login");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用戶註冊時發生錯誤");
                await _systemLogService.LogAsync("Error", $"用戶註冊失敗: {ex.Message}", "System");
                TempData["ErrorMessage"] = "註冊時發生錯誤，請稍後再試";
                return View("Login");
            }
        }

        #region 私有方法

        // 使用 BaseController 的登入驗證與使用者資訊取得，避免覆蓋造成判斷不一致

        #endregion
    }

    /// <summary>
    /// 變更密碼請求模型
    /// </summary>
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 註冊請求模型
    /// </summary>
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}