using CioSystem.Models;

namespace CioSystem.Services.Authentication
{
    /// <summary>
    /// 用戶服務接口
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 用戶登入
        /// </summary>
        Task<LoginResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// 用戶登出
        /// </summary>
        Task<bool> LogoutAsync(string sessionId);

        /// <summary>
        /// 驗證會話
        /// </summary>
        Task<User?> ValidateSessionAsync(string sessionId);

        /// <summary>
        /// 獲取所有用戶
        /// </summary>
        Task<IEnumerable<UserViewModel>> GetAllUsersAsync();

        /// <summary>
        /// 根據ID獲取用戶
        /// </summary>
        Task<UserViewModel?> GetUserByIdAsync(int id);

        /// <summary>
        /// 根據用戶名獲取用戶
        /// </summary>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// 創建新用戶
        /// </summary>
        Task<UserViewModel> CreateUserAsync(CreateUserRequest request);

        /// <summary>
        /// 更新用戶
        /// </summary>
        Task<UserViewModel> UpdateUserAsync(int id, UpdateUserRequest request);

        /// <summary>
        /// 刪除用戶
        /// </summary>
        Task<bool> DeleteUserAsync(int id);

        /// <summary>
        /// 更改密碼
        /// </summary>
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>
        /// 更新最後活動時間
        /// </summary>
        Task UpdateLastActivityAsync(string sessionId);

        /// <summary>
        /// 獲取在線用戶
        /// </summary>
        Task<IEnumerable<UserViewModel>> GetOnlineUsersAsync();

        /// <summary>
        /// 檢查用戶名是否可用
        /// </summary>
        Task<bool> IsUsernameAvailableAsync(string username);

        /// <summary>
        /// 檢查郵箱是否可用
        /// </summary>
        Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null);

        /// <summary>
        /// 獲取用戶統計信息
        /// </summary>
        Task<UserStatisticsViewModel> GetUserStatisticsAsync();

        /// <summary>
        /// 初始化預設管理員帳號
        /// </summary>
        Task InitializeDefaultAdminAsync();

        /// <summary>
        /// 登出所有活躍用戶
        /// </summary>
        Task LogoutAllUsersAsync();

        /// <summary>
        /// 登出過期的會話（超過指定時間未活動的會話）
        /// </summary>
        Task LogoutExpiredSessionsAsync(int timeoutMinutes = 30);
    }

    /// <summary>
    /// 用戶統計視圖模型
    /// </summary>
    public class UserStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int OnlineUsers { get; set; }
        public int AdminUsers { get; set; }
        public int ManagerUsers { get; set; }
        public int StaffUsers { get; set; }
        public int ViewerUsers { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public string MostActiveUser { get; set; } = string.Empty;
    }
}