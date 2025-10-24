using CioSystem.Models;
using CioSystem.Services.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace CioSystem.Web.Controllers
{
    public class UserManagementController : BaseController
    {
        private readonly IUserService _userService;

        public UserManagementController(
            ILogger<UserManagementController> logger,
            IUserService userService) : base(logger)
        {
            _userService = userService;
        }

        /// <summary>
        /// 用戶管理首頁
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var statistics = await _userService.GetUserStatisticsAsync();
                var onlineUsers = await _userService.GetOnlineUsersAsync();

                ViewBag.Statistics = statistics;
                ViewBag.OnlineUsers = onlineUsers;

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入用戶管理頁面時發生錯誤");
                TempData["ErrorMessage"] = "載入用戶管理頁面時發生錯誤";
                return View(new List<UserViewModel>());
            }
        }

        /// <summary>
        /// 創建用戶頁面
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateUserRequest());
        }

        /// <summary>
        /// 創建用戶
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(request);
                }

                var user = await _userService.CreateUserAsync(request);
                TempData["SuccessMessage"] = $"用戶 {user.Username} 創建成功！";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建用戶時發生錯誤");
                ModelState.AddModelError("", "創建用戶時發生錯誤，請稍後再試");
                return View(request);
            }
        }

        /// <summary>
        /// 編輯用戶頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "用戶不存在";
                    return RedirectToAction("Index");
                }

                var request = new UpdateUserRequest
                {
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive
                };

                ViewBag.User = user;
                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入編輯用戶頁面時發生錯誤: {UserId}", id);
                TempData["ErrorMessage"] = "載入編輯用戶頁面時發生錯誤";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 更新用戶
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Edit(int id, UpdateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var currentUser = await _userService.GetUserByIdAsync(id);
                    ViewBag.User = currentUser;
                    return View(request);
                }

                var updatedUser = await _userService.UpdateUserAsync(id, request);
                TempData["SuccessMessage"] = $"用戶 {updatedUser.Username} 更新成功！";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var currentUser = await _userService.GetUserByIdAsync(id);
                ViewBag.User = currentUser;
                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用戶時發生錯誤: {UserId}", id);
                ModelState.AddModelError("", "更新用戶時發生錯誤，請稍後再試");
                var currentUser = await _userService.GetUserByIdAsync(id);
                ViewBag.User = currentUser;
                return View(request);
            }
        }

        /// <summary>
        /// 刪除用戶
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _userService.DeleteUserAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "用戶刪除成功！";
                }
                else
                {
                    TempData["ErrorMessage"] = "用戶不存在或刪除失敗";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除用戶時發生錯誤: {UserId}", id);
                TempData["ErrorMessage"] = "刪除用戶時發生錯誤";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// 檢查用戶名可用性
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckUsername(string username)
        {
            try
            {
                var isAvailable = await _userService.IsUsernameAvailableAsync(username);
                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查用戶名可用性時發生錯誤: {Username}", username);
                return Json(new { available = false });
            }
        }

        /// <summary>
        /// 檢查郵箱可用性
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            try
            {
                var isAvailable = await _userService.IsEmailAvailableAsync(email);
                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查郵箱可用性時發生錯誤: {Email}", email);
                return Json(new { available = false });
            }
        }

        /// <summary>
        /// 獲取用戶統計信息
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var statistics = await _userService.GetUserStatisticsAsync();
                return Json(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取用戶統計信息時發生錯誤");
                return Json(new { success = false, message = "獲取統計信息失敗" });
            }
        }

        /// <summary>
        /// 獲取在線用戶
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOnlineUsers()
        {
            try
            {
                var onlineUsers = await _userService.GetOnlineUsersAsync();
                return Json(new { success = true, users = onlineUsers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取在線用戶時發生錯誤");
                return Json(new { success = false, message = "獲取在線用戶失敗" });
            }
        }
    }
}