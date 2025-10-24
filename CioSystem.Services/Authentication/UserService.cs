using CioSystem.Models;
using CioSystem.Data;
using CioSystem.Services.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CioSystem.Services.Authentication
{
    public class UserService : IUserService
    {
        private readonly CioSystemDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly ISystemLogService _systemLogService;

        public UserService(
            CioSystemDbContext context,
            ILogger<UserService> logger,
            ISystemLogService systemLogService)
        {
            _context = context;
            _logger = logger;
            _systemLogService = systemLogService;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("用戶嘗試登入: {Username}", request.Username);

                // 查找用戶
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

                if (user == null)
                {
                    await _systemLogService.LogAsync("Warning", $"登入失敗：用戶不存在 {request.Username}", "System");
                    _logger.LogWarning("登入失敗：用戶不存在 {Username}", request.Username);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "用戶名或密碼錯誤"
                    };
                }

                // 驗證密碼
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    await _systemLogService.LogAsync("Warning", $"登入失敗：密碼錯誤 {request.Username}", "System");
                    _logger.LogWarning("登入失敗：密碼錯誤 {Username}", request.Username);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "用戶名或密碼錯誤"
                    };
                }

                // 創建會話
                var sessionId = Guid.NewGuid().ToString();
                var session = new UserSession
                {
                    UserId = user.Id,
                    SessionId = sessionId,
                    LoginTime = DateTime.Now,
                    LastActivity = DateTime.Now,
                    IsActive = true,
                    IpAddress = request.IpAddress,
                    UserAgent = request.UserAgent,
                    CreatedBy = user.Username,
                    UpdatedBy = user.Username,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserSessions.Add(session);
                await _context.SaveChangesAsync();

                // 更新用戶最後登入時間
                user.LastLogin = DateTime.Now;
                user.LastActivity = DateTime.Now;
                user.SessionId = sessionId;
                user.UpdatedBy = user.Username;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _systemLogService.LogAsync("Info", $"用戶登入成功: {user.Username} ({user.Role})", user.Username);
                _logger.LogInformation("用戶登入成功: {Username} ({Role})", user.Username, user.Role);

                return new LoginResponse
                {
                    Success = true,
                    Message = "登入成功",
                    User = new UserViewModel
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = user.Role,
                        IsActive = user.IsActive,
                        LastLogin = user.LastLogin,
                        CreatedDate = user.CreatedDate
                    },
                    SessionId = sessionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用戶登入時發生錯誤: {Username}", request.Username);
                await _systemLogService.LogAsync("Error", $"登入系統錯誤: {ex.Message}", "System");
                _logger.LogError(ex, "登入系統錯誤: {Message}", ex.Message);

                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
                var isDev = env.Equals("Development", StringComparison.OrdinalIgnoreCase);
                return new LoginResponse
                {
                    Success = false,
                    Message = isDev ? $"登入發生例外：{ex.Message}" : "登入時發生錯誤，請稍後再試"
                };
            }
        }

        public async Task<bool> LogoutAsync(string sessionId)
        {
            try
            {
                var session = await _context.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

                if (session != null)
                {
                    session.IsActive = false;
                    session.LogoutTime = DateTime.Now;
                    await _context.SaveChangesAsync();

                    await _systemLogService.LogAsync("Info", $"用戶登出: {session.User.Username}", session.User.Username);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用戶登出時發生錯誤: {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<User?> ValidateSessionAsync(string sessionId)
        {
            try
            {
                var session = await _context.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive && s.User.IsActive);

                if (session == null)
                    return null;

                // 更新最後活動時間
                session.LastActivity = DateTime.Now;
                await _context.SaveChangesAsync();

                return session.User;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證會話時發生錯誤: {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<IEnumerable<UserViewModel>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("開始查詢所有用戶");
                _logger.LogInformation("資料庫上下文: {ContextType}", _context?.GetType().Name ?? "null");
                
                // 先測試基本查詢
                var totalUsers = await _context.Users.CountAsync();
                _logger.LogInformation("資料庫中總用戶數: {TotalUsers}", totalUsers);
                
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                _logger.LogInformation("活躍用戶數: {ActiveUsers}", activeUsers);
                
                var users = await _context.Users
                    .Where(u => !u.IsDeleted)
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                _logger.LogInformation("查詢到 {Count} 個用戶", users.Count);
                
                if (users.Any())
                {
                    foreach (var user in users)
                    {
                        _logger.LogInformation("用戶: {Username}, 角色: {Role}, 活躍: {IsActive}", 
                            user.Username, user.Role, user.IsActive);
                    }
                }

                var result = users.Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email ?? string.Empty,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    LastLogin = u.LastLogin,
                    CreatedDate = u.CreatedDate
                }).ToList();

                _logger.LogInformation("轉換為 UserViewModel 完成，返回 {Count} 個用戶", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢所有用戶時發生錯誤: {Message}", ex.Message);
                _logger.LogError(ex, "堆疊追蹤: {StackTrace}", ex.StackTrace);
                _logger.LogError(ex, "內部異常: {InnerException}", ex.InnerException?.Message ?? "無");
                return new List<UserViewModel>();
            }
        }

        public async Task<UserViewModel?> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null || !user.IsActive)
                    return null;

                return new UserViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    LastLogin = user.LastLogin,
                    CreatedDate = user.CreatedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據ID獲取用戶時發生錯誤: {UserId}", id);
                return null;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據用戶名獲取用戶時發生錯誤: {Username}", username);
                return null;
            }
        }

        public async Task<UserViewModel> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                // 檢查用戶名是否已存在
                if (await IsUsernameAvailableAsync(request.Username) == false)
                {
                    throw new InvalidOperationException("用戶名已存在");
                }

                // 檢查郵箱是否已存在
                if (await IsEmailAvailableAsync(request.Email) == false)
                {
                    throw new InvalidOperationException("郵箱已被使用");
                }

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Role = request.Role,
                    IsActive = request.IsActive,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await _systemLogService.LogAsync("Info", $"創建新用戶: {user.Username} ({user.Role})", "System");

                return new UserViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    LastLogin = user.LastLogin,
                    CreatedDate = user.CreatedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建用戶時發生錯誤: {Username}", request.Username);
                throw;
            }
        }

        public async Task<UserViewModel> UpdateUserAsync(int id, UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    throw new InvalidOperationException("用戶不存在");
                }

                // 更新用戶信息
                if (!string.IsNullOrEmpty(request.Email))
                {
                    if (await IsEmailAvailableAsync(request.Email, id) == false)
                    {
                        throw new InvalidOperationException("郵箱已被其他用戶使用");
                    }
                    user.Email = request.Email;
                }

                if (!string.IsNullOrEmpty(request.Password))
                {
                    user.PasswordHash = HashPassword(request.Password);
                }

                if (!string.IsNullOrEmpty(request.FirstName))
                {
                    user.FirstName = request.FirstName;
                }

                if (!string.IsNullOrEmpty(request.LastName))
                {
                    user.LastName = request.LastName;
                }

                if (!string.IsNullOrEmpty(request.Role))
                {
                    user.Role = request.Role;
                }

                user.IsActive = request.IsActive;

                user.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                await _systemLogService.LogAsync("Info", $"更新用戶信息: {user.Username}", "System");

                return new UserViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    LastLogin = user.LastLogin,
                    CreatedDate = user.CreatedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用戶時發生錯誤: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return false;

                // 軟刪除：設置為已刪除狀態
                user.IsDeleted = true;
                user.IsActive = false;
                user.UpdatedDate = DateTime.Now;

                // 關閉所有會話
                var sessions = await _context.UserSessions
                    .Where(s => s.UserId == id && s.IsActive)
                    .ToListAsync();

                foreach (var session in sessions)
                {
                    session.IsActive = false;
                    session.LogoutTime = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                await _systemLogService.LogAsync("Info", $"刪除用戶: {user.Username}", "System");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除用戶時發生錯誤: {UserId}", id);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                // 驗證當前密碼
                if (!VerifyPassword(currentPassword, user.PasswordHash))
                    return false;

                // 更新密碼
                user.PasswordHash = HashPassword(newPassword);
                user.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                await _systemLogService.LogAsync("Info", $"用戶更改密碼: {user.Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更改密碼時發生錯誤: {UserId}", userId);
                return false;
            }
        }

        public async Task UpdateLastActivityAsync(string sessionId)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

                if (session != null)
                {
                    session.LastActivity = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新最後活動時間時發生錯誤: {SessionId}", sessionId);
            }
        }

        public async Task<IEnumerable<UserViewModel>> GetOnlineUsersAsync()
        {
            try
            {
                var cutoffTime = DateTime.Now.AddMinutes(-5); // 5分鐘內有活動的用戶視為在線

                var onlineSessions = await _context.UserSessions
                    .Include(s => s.User)
                    .Where(s => s.IsActive && s.LastActivity >= cutoffTime && s.User.IsActive)
                    .ToListAsync();

                return onlineSessions
                    .GroupBy(s => s.UserId)
                    .Select(g => g.First().User)
                    .Select(u => new UserViewModel
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Role = u.Role,
                        IsActive = u.IsActive,
                        LastLogin = u.LastLogin,
                        CreatedDate = u.CreatedDate
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取在線用戶時發生錯誤");
                return new List<UserViewModel>();
            }
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            try
            {
                return !await _context.Users.AnyAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查用戶名可用性時發生錯誤: {Username}", username);
                return false;
            }
        }

        public async Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null)
        {
            try
            {
                var query = _context.Users.Where(u => u.Email == email);

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeUserId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查郵箱可用性時發生錯誤: {Email}", email);
                return false;
            }
        }

        public async Task<UserStatisticsViewModel> GetUserStatisticsAsync()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync(u => u.IsActive && !u.IsDeleted);
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var onlineUsers = (await GetOnlineUsersAsync()).Count();

                var adminUsers = await _context.Users.CountAsync(u => u.Role == "管理員" && u.IsActive);
                var managerUsers = await _context.Users.CountAsync(u => u.Role == "經理" && u.IsActive);
                var staffUsers = await _context.Users.CountAsync(u => u.Role == "員工" && u.IsActive);
                var viewerUsers = await _context.Users.CountAsync(u => u.Role == "檢視者" && u.IsActive);

                var lastLoginTime = await _context.Users
                    .Where(u => u.LastLogin.HasValue)
                    .MaxAsync(u => u.LastLogin);

                var mostActiveUser = await _context.UserSessions
                    .Include(s => s.User)
                    .Where(s => s.IsActive)
                    .GroupBy(s => s.UserId)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.First().User.Username)
                    .FirstOrDefaultAsync();

                return new UserStatisticsViewModel
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    OnlineUsers = onlineUsers,
                    AdminUsers = adminUsers,
                    ManagerUsers = managerUsers,
                    StaffUsers = staffUsers,
                    ViewerUsers = viewerUsers,
                    LastLoginTime = lastLoginTime,
                    MostActiveUser = mostActiveUser ?? "無"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取用戶統計信息時發生錯誤");
                return new UserStatisticsViewModel();
            }
        }

        #region 私有方法

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }

        /// <summary>
        /// 初始化預設管理員帳號
        /// </summary>
        public async Task InitializeDefaultAdminAsync()
        {
            try
            {
                // 檢查是否已存在管理員帳號
                var existingAdmin = await _context.Users
                    .FirstOrDefaultAsync(u => u.Role == "管理員" && !u.IsDeleted);

                if (existingAdmin != null)
                {
                    // 確保可登入狀態，並重置密碼以利開發環境登入
                    existingAdmin.IsActive = true;
                    existingAdmin.PasswordHash = HashPassword("admin123");
                    existingAdmin.UpdatedAt = DateTime.Now;
                    existingAdmin.UpdatedBy = "System";
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("管理員帳號已存在，已重置密碼為 admin123");
                    return;
                }

                // 創建預設管理員帳號
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@ciosystem.com",
                    PasswordHash = HashPassword("admin123"),
                    Role = "管理員",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    IsDeleted = false
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("預設管理員帳號創建成功 - Username: admin, Password: admin123");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化預設管理員帳號時發生錯誤");
            }
        }

        /// <summary>
        /// 登出所有活躍用戶
        /// </summary>
        public async Task LogoutAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("開始登出所有活躍用戶");
                
                // 獲取所有活躍的會話
                var activeSessions = await _context.UserSessions
                    .Where(s => s.IsActive)
                    .ToListAsync();

                if (activeSessions.Any())
                {
                    _logger.LogInformation("找到 {Count} 個活躍會話，開始登出", activeSessions.Count);
                    
                    // 批量更新所有活躍會話為非活躍狀態
                    foreach (var session in activeSessions)
                    {
                        session.IsActive = false;
                        session.LogoutTime = DateTime.Now;
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("成功登出 {Count} 個用戶", activeSessions.Count);
                }
                else
                {
                    _logger.LogInformation("沒有活躍用戶需要登出");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出所有用戶時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 登出過期的會話（超過指定時間未活動的會話）
        /// </summary>
        public async Task LogoutExpiredSessionsAsync(int timeoutMinutes = 30)
        {
            try
            {
                _logger.LogInformation("開始清理過期會話，超時時間: {TimeoutMinutes} 分鐘", timeoutMinutes);
                
                var cutoffTime = DateTime.Now.AddMinutes(-timeoutMinutes);
                
                // 獲取過期的會話
                var expiredSessions = await _context.UserSessions
                    .Where(s => s.IsActive && s.LastActivity < cutoffTime)
                    .ToListAsync();

                if (expiredSessions.Any())
                {
                    _logger.LogInformation("找到 {Count} 個過期會話，開始清理", expiredSessions.Count);
                    
                    // 批量更新過期會話為非活躍狀態
                    foreach (var session in expiredSessions)
                    {
                        session.IsActive = false;
                        session.LogoutTime = DateTime.Now;
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("成功清理 {Count} 個過期會話", expiredSessions.Count);
                }
                else
                {
                    _logger.LogInformation("沒有過期會話需要清理");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理過期會話時發生錯誤");
                throw;
            }
        }

        #endregion
    }
}