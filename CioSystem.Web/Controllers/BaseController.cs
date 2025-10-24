using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CioSystem.Services;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CioSystem.Web.Controllers
{
    /// <summary>
    /// 基礎控制器類
    /// 提供通用的錯誤處理、日誌記錄和響應方法
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected readonly ILogger _logger;

        protected BaseController(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 執行操作並處理異常
        /// </summary>
        /// <typeparam name="T">返回類型</typeparam>
        /// <param name="operation">要執行的操作</param>
        /// <param name="errorMessage">錯誤訊息</param>
        /// <param name="logContext">日誌上下文</param>
        /// <returns>操作結果</returns>
        protected async Task<IActionResult> ExecuteWithErrorHandling<T>(
            Func<Task<T>> operation,
            string errorMessage = "操作執行時發生錯誤",
            string logContext = "")
        {
            try
            {
                var result = await operation();
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "參數錯誤 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                return BadRequest(new { error = "請求參數無效", details = ex.Message ?? "未知錯誤" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "資源未找到 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                return NotFound(new { error = "找不到指定的資源", details = ex.Message ?? "未知錯誤" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "無效操作 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                return BadRequest(new { error = "操作無效", details = ex.Message ?? "未知錯誤" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "未預期的錯誤 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                return StatusCode(500, new { error = errorMessage, details = "系統發生錯誤，請稍後再試" });
            }
        }

        /// <summary>
        /// 執行操作並處理異常（無返回值）
        /// </summary>
        /// <param name="operation">要執行的操作</param>
        /// <param name="successMessage">成功訊息</param>
        /// <param name="errorMessage">錯誤訊息</param>
        /// <param name="logContext">日誌上下文</param>
        /// <returns>操作結果</returns>
        protected async Task<IActionResult> ExecuteWithErrorHandling(
            Func<Task> operation,
            string successMessage = "操作成功",
            string errorMessage = "操作執行時發生錯誤",
            string logContext = "")
        {
            try
            {
                await operation();
                TempData["SuccessMessage"] = successMessage;
                return Ok(new { message = successMessage });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "參數錯誤 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                TempData["ErrorMessage"] = $"請求參數無效: {ex.Message ?? "未知錯誤"}";
                return BadRequest(new { error = "請求參數無效", details = ex.Message ?? "未知錯誤" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "資源未找到 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                TempData["ErrorMessage"] = "找不到指定的資源";
                return NotFound(new { error = "找不到指定的資源", details = ex.Message ?? "未知錯誤" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "無效操作 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                TempData["ErrorMessage"] = $"操作無效: {ex.Message ?? "未知錯誤"}";
                return BadRequest(new { error = "操作無效", details = ex.Message ?? "未知錯誤" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "未預期的錯誤 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                TempData["ErrorMessage"] = errorMessage;
                return StatusCode(500, new { error = errorMessage, details = "系統發生錯誤，請稍後再試" });
            }
        }

        /// <summary>
        /// 驗證模型狀態
        /// </summary>
        /// <param name="logContext">日誌上下文</param>
        /// <returns>是否驗證通過</returns>
        protected bool ValidateModelState(string logContext = "")
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage ?? "驗證失敗").ToArray()
                    );

                _logger.LogWarning("模型驗證失敗 {LogContext}: {Errors}", logContext, string.Join(", ", errors.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}")));

                TempData["ErrorMessage"] = "表單驗證失敗，請檢查輸入的資料";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 記錄操作開始
        /// </summary>
        /// <param name="operation">操作名稱</param>
        /// <param name="parameters">參數</param>
        protected void LogOperationStart(string operation, params object[] parameters)
        {
            _logger.LogInformation("開始執行 {Operation} {Parameters}", operation, string.Join(", ", parameters));
        }

        /// <summary>
        /// 記錄操作完成
        /// </summary>
        /// <param name="operation">操作名稱</param>
        /// <param name="result">結果</param>
        protected void LogOperationComplete(string operation, object? result = null)
        {
            _logger.LogInformation("完成執行 {Operation} {Result}", operation, result?.ToString() ?? "");
        }

        /// <summary>
        /// 檢查用戶是否已登入 - 異步版本
        /// </summary>
        /// <returns>是否已登入</returns>
        protected async Task<bool> IsUserLoggedInAsync()
        {
            var sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId))
                return false;

            // 驗證會話是否在數據庫中存在且有效
            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<CioSystem.Services.Authentication.IUserService>();
                var user = await userService.ValidateSessionAsync(sessionId);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證會話時發生錯誤: {SessionId}", sessionId);
                return false;
            }
        }

        /// <summary>
        /// 檢查用戶是否已登入 - 同步版本
        /// </summary>
        /// <returns>是否已登入</returns>
        protected bool IsUserLoggedIn()
        {
            var sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId))
                return false;

            // 驗證會話是否在數據庫中存在且有效
            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<CioSystem.Services.Authentication.IUserService>();
                var user = userService.ValidateSessionAsync(sessionId).GetAwaiter().GetResult();
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證會話時發生錯誤: {SessionId}", sessionId);
                return false;
            }
        }

        /// <summary>
        /// 獲取當前用戶ID
        /// </summary>
        /// <returns>用戶ID</returns>
        protected int GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out int userId) ? userId : 0;
        }

        /// <summary>
        /// 獲取當前用戶名
        /// </summary>
        /// <returns>用戶名</returns>
        protected string GetCurrentUsername()
        {
            return HttpContext.Session.GetString("Username") ?? string.Empty;
        }

        /// <summary>
        /// 獲取當前用戶角色
        /// </summary>
        /// <returns>用戶角色</returns>
        protected string GetCurrentUserRole()
        {
            return HttpContext.Session.GetString("Role") ?? string.Empty;
        }

        /// <summary>
        /// 重定向到登入頁面（如果未登入）
        /// </summary>
        /// <returns>重定向結果或null</returns>
        protected async Task<IActionResult?> RequireLoginAsync()
        {
            if (!await IsUserLoggedInAsync())
            {
                TempData["ErrorMessage"] = "請先登入系統";
                return RedirectToAction("Login", "Auth");
            }
            return null;
        }

        /// <summary>
        /// 重定向到登入頁面（如果未登入）- 同步版本
        /// </summary>
        /// <returns>重定向結果或null</returns>
        protected IActionResult? RequireLogin()
        {
            if (!IsUserLoggedIn())
            {
                TempData["ErrorMessage"] = "請先登入系統";
                return RedirectToAction("Login", "Auth");
            }
            return null;
        }

        /// <summary>
        /// 創建分頁信息
        /// </summary>
        /// <param name="page">當前頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="totalCount">總數量</param>
        protected void SetPaginationInfo(int page, int pageSize, int totalCount)
        {
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        }

        /// <summary>
        /// 設置成功訊息
        /// </summary>
        /// <param name="message">訊息內容</param>
        protected void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        /// <summary>
        /// 設置錯誤訊息
        /// </summary>
        /// <param name="message">訊息內容</param>
        protected void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        /// <summary>
        /// 執行 Web 操作並處理異常（返回視圖）
        /// </summary>
        /// <typeparam name="T">返回類型</typeparam>
        /// <param name="operation">要執行的操作</param>
        /// <param name="viewName">視圖名稱</param>
        /// <param name="errorMessage">錯誤訊息</param>
        /// <param name="logContext">日誌上下文</param>
        /// <returns>操作結果</returns>
        protected async Task<IActionResult> ExecuteWebOperation<T>(
            Func<Task<T>> operation,
            string viewName,
            string errorMessage = "操作執行時發生錯誤",
            string logContext = "")
        {
            try
            {
                var result = await operation();
                return View(viewName, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "參數錯誤 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                SetErrorMessage($"請求參數無效: {ex.Message ?? "未知錯誤"}");
                return View(viewName);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "資源未找到 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                SetErrorMessage("找不到指定的資源");
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "無效操作 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                SetErrorMessage($"操作無效: {ex.Message ?? "未知錯誤"}");
                return View(viewName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "未預期的錯誤 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                SetErrorMessage(errorMessage);
                return View(viewName);
            }
        }

        /// <summary>
        /// 執行 Web 操作並處理異常（重定向）
        /// </summary>
        /// <param name="operation">要執行的操作</param>
        /// <param name="successAction">成功後的重定向動作</param>
        /// <param name="successMessage">成功訊息</param>
        /// <param name="errorMessage">錯誤訊息</param>
        /// <param name="logContext">日誌上下文</param>
        /// <returns>操作結果</returns>
        protected async Task<IActionResult> ExecuteWebOperationWithRedirect(
            Func<Task> operation,
            string successAction,
            string successMessage = "操作成功",
            string errorMessage = "操作執行時發生錯誤",
            string logContext = "")
        {
            try
            {
                await operation();
                SetSuccessMessage(successMessage);
                return RedirectToAction(successAction);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "參數錯誤 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                SetErrorMessage($"請求參數無效: {ex.Message ?? "未知錯誤"}");
                return RedirectToAction(successAction);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "資源未找到 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                SetErrorMessage("找不到指定的資源");
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "無效操作 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                SetErrorMessage($"操作無效: {ex.Message ?? "未知錯誤"}");
                return RedirectToAction(successAction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "未預期的錯誤 {LogContext}: {Message}", logContext, ex.Message ?? "未知錯誤");
                SetErrorMessage(errorMessage);
                return RedirectToAction(successAction);
            }
        }

        /// <summary>
        /// 驗證並執行模型綁定
        /// </summary>
        /// <typeparam name="T">模型類型</typeparam>
        /// <param name="model">要驗證的模型</param>
        /// <param name="logContext">日誌上下文</param>
        /// <returns>驗證結果</returns>
        protected CioSystem.Services.ValidationResult ValidateModel<T>(T model, string logContext = "") where T : class
        {
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var validationContext = new ValidationContext(model);

            if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "驗證失敗").ToList();
                _logger.LogWarning("模型驗證失敗 {LogContext}: {Errors}", logContext, string.Join(", ", errors));

                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }

                var result = new CioSystem.Services.ValidationResult();
                result.AddErrors(errors);
                return result;
            }

            return new CioSystem.Services.ValidationResult();
        }

        /// <summary>
        /// 創建分頁響應
        /// </summary>
        /// <typeparam name="T">數據類型</typeparam>
        /// <param name="data">數據</param>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="totalCount">總數量</param>
        /// <returns>分頁響應</returns>
        protected PagedResult<T> CreatePagedResult<T>(IEnumerable<T> data, int page, int pageSize, int totalCount)
        {
            return new PagedResult<T>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }

        /// <summary>
        /// 記錄性能指標
        /// </summary>
        /// <param name="operationName">操作名稱</param>
        /// <param name="elapsedMilliseconds">執行時間（毫秒）</param>
        /// <param name="additionalInfo">額外信息</param>
        protected void LogPerformance(string operationName, long elapsedMilliseconds, string additionalInfo = "")
        {
            if (elapsedMilliseconds > 1000) // 超過1秒記錄警告
            {
                _logger.LogWarning("性能警告: {OperationName} 執行時間 {ElapsedMs}ms {AdditionalInfo}",
                    operationName, elapsedMilliseconds, additionalInfo);
            }
            else
            {
                _logger.LogInformation("性能記錄: {OperationName} 執行時間 {ElapsedMs}ms {AdditionalInfo}",
                    operationName, elapsedMilliseconds, additionalInfo);
            }
        }
    }


    /// <summary>
    /// 分頁結果
    /// </summary>
    /// <typeparam name="T">數據類型</typeparam>
    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}