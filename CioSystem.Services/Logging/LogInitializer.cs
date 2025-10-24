using CioSystem.Models;

namespace CioSystem.Services.Logging
{
    /// <summary>
    /// 日誌初始化器，用於創建示例日誌數據
    /// </summary>
    public class LogInitializer
    {
        private readonly ISystemLogService _logService;

        public LogInitializer(ISystemLogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// 初始化示例日誌數據
        /// </summary>
        public async Task InitializeSampleLogsAsync()
        {
            try
            {
                // 檢查是否已經有日誌數據
                var (existingLogs, _) = await _logService.GetLogsAsync(pageSize: 1);
                if (existingLogs.Any())
                {
                    return; // 已經有數據，不需要初始化
                }

                // 創建示例日誌數據
                var sampleLogs = new[]
                {
                    new { Level = "Info", Message = "系統啟動成功", User = "System" },
                    new { Level = "Info", Message = "用戶 admin 登入成功", User = "admin" },
                    new { Level = "Info", Message = "產品資料同步完成", User = "System" },
                    new { Level = "Warning", Message = "資料庫連線延遲 500ms", User = "System" },
                    new { Level = "Info", Message = "庫存資料更新完成", User = "admin" },
                    new { Level = "Error", Message = "用戶 manager 登入失敗：密碼錯誤", User = "manager" },
                    new { Level = "Info", Message = "銷售記錄匯出完成", User = "admin" },
                    new { Level = "Warning", Message = "記憶體使用率達到 80%", User = "System" },
                    new { Level = "Info", Message = "備份任務執行完成", User = "System" },
                    new { Level = "Error", Message = "資料庫連接超時", User = "System" },
                    new { Level = "Info", Message = "用戶 staff 登入成功", User = "staff" },
                    new { Level = "Info", Message = "庫存警告：產品 A001 庫存不足", User = "System" },
                    new { Level = "Warning", Message = "磁碟空間使用率達到 85%", User = "System" },
                    new { Level = "Info", Message = "報表生成完成", User = "admin" },
                    new { Level = "Error", Message = "檔案上傳失敗：檔案大小超過限制", User = "staff" },
                    new { Level = "Info", Message = "系統設定更新完成", User = "admin" },
                    new { Level = "Warning", Message = "API 請求回應時間過長", User = "System" },
                    new { Level = "Info", Message = "用戶登出成功", User = "staff" },
                    new { Level = "Error", Message = "資料驗證失敗：無效的產品編號", User = "admin" },
                    new { Level = "Info", Message = "系統維護模式啟動", User = "admin" }
                };

                // 記錄示例日誌
                foreach (var log in sampleLogs)
                {
                    await _logService.LogAsync(log.Level, log.Message, log.User);
                    await Task.Delay(100); // 確保時間戳不同
                }
            }
            catch (Exception ex)
            {
                // 記錄初始化錯誤，但不拋出異常
                await _logService.LogAsync("Error", $"日誌初始化失敗: {ex.Message}", "System", ex);
            }
        }
    }
}