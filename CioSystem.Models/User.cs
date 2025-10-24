using System.ComponentModel.DataAnnotations;
using CioSystem.Core;

namespace CioSystem.Models
{
    /// <summary>
    /// 用戶模型
    /// </summary>
    public class User : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Staff";

        public bool IsActive { get; set; } = true;

        public DateTime? LastLogin { get; set; }

        public DateTime? LastActivity { get; set; }

        public string? SessionId { get; set; }

        public string? ConnectionId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        // 導航屬性
        public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    }

    /// <summary>
    /// 用戶會話模型
    /// </summary>
    public class UserSession : BaseEntity
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string SessionId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ConnectionId { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.Now;

        public DateTime? LastActivity { get; set; }

        public DateTime? LogoutTime { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        // 導航屬性
        public virtual User User { get; set; } = null!;
    }

    /// <summary>
    /// 用戶角色枚舉
    /// </summary>
    public enum UserRole
    {
        Admin = 1,      // 管理員
        Manager = 2,    // 經理
        Staff = 3,      // 員工
        Viewer = 4      // 查看者
    }

    /// <summary>
    /// 登入請求模型
    /// </summary>
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// 登入響應模型
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserViewModel? User { get; set; }
        public string? SessionId { get; set; }
        public string? Token { get; set; }
    }

    /// <summary>
    /// 用戶視圖模型
    /// </summary>
    public class UserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>
    /// 用戶創建請求模型
    /// </summary>
    public class CreateUserRequest
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "密碼確認不匹配")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [Required]
        public string Role { get; set; } = "Staff";

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 用戶更新請求模型
    /// </summary>
    public class UpdateUserRequest
    {
        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [MinLength(6)]
        public string? Password { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        public string? Role { get; set; }

        public bool IsActive { get; set; } = true;
    }
}