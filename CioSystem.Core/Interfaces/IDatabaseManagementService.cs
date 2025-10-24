using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CioSystem.Core.Interfaces
{
    /// <summary>
    /// 資料庫管理服務接口
    /// 提供資料庫備份、還原、清理等功能
    /// </summary>
    public interface IDatabaseManagementService : IService
    {
        /// <summary>
        /// 創建資料庫備份
        /// </summary>
        /// <param name="backupName">備份名稱（可選）</param>
        /// <param name="includeData">是否包含數據</param>
        /// <param name="compress">是否壓縮</param>
        /// <returns>備份文件路徑</returns>
        Task<string> CreateBackupAsync(string? backupName = null, bool includeData = true, bool compress = true);

        /// <summary>
        /// 從備份還原資料庫
        /// </summary>
        /// <param name="backupFilePath">備份文件路徑</param>
        /// <param name="createBackupBeforeRestore">還原前是否創建備份</param>
        /// <returns>還原結果</returns>
        Task<DatabaseRestoreResult> RestoreFromBackupAsync(string backupFilePath, bool createBackupBeforeRestore = true);

        /// <summary>
        /// 清理資料庫
        /// </summary>
        /// <param name="cleanupOptions">清理選項</param>
        /// <returns>清理結果</returns>
        Task<DatabaseCleanupResult> CleanupDatabaseAsync(DatabaseCleanupOptions cleanupOptions);

        /// <summary>
        /// 獲取所有備份文件
        /// </summary>
        /// <returns>備份文件列表</returns>
        Task<IEnumerable<BackupFileInfo>> GetBackupFilesAsync();

        /// <summary>
        /// 刪除備份文件
        /// </summary>
        /// <param name="backupFilePath">備份文件路徑</param>
        /// <returns>刪除結果</returns>
        Task<bool> DeleteBackupAsync(string backupFilePath);

        /// <summary>
        /// 驗證備份文件完整性
        /// </summary>
        /// <param name="backupFilePath">備份文件路徑</param>
        /// <returns>驗證結果</returns>
        Task<BackupValidationResult> ValidateBackupAsync(string backupFilePath);

        /// <summary>
        /// 獲取資料庫統計信息
        /// </summary>
        /// <returns>資料庫統計信息</returns>
        Task<DatabaseStatistics> GetDatabaseStatisticsAsync();

        /// <summary>
        /// 優化資料庫
        /// </summary>
        /// <returns>優化結果</returns>
        Task<DatabaseOptimizationResult> OptimizeDatabaseAsync();
    }

    /// <summary>
    /// 資料庫還原結果
    /// </summary>
    public class DatabaseRestoreResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BackupFilePath { get; set; }
        public DateTime RestoreTime { get; set; }
        public long RestoredRecords { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// 資料庫清理選項
    /// </summary>
    public class DatabaseCleanupOptions
    {
        /// <summary>
        /// 清理舊的庫存移動記錄（保留天數）
        /// </summary>
        public int? KeepInventoryMovementsDays { get; set; }

        /// <summary>
        /// 清理舊的日誌記錄（保留天數）
        /// </summary>
        public int? KeepLogsDays { get; set; }

        /// <summary>
        /// 清理空的庫存記錄
        /// </summary>
        public bool CleanEmptyInventory { get; set; } = true;

        /// <summary>
        /// 清理無效的產品記錄
        /// </summary>
        public bool CleanInvalidProducts { get; set; } = true;

        /// <summary>
        /// 優化資料庫
        /// </summary>
        public bool OptimizeDatabase { get; set; } = true;

        /// <summary>
        /// 創建清理前備份
        /// </summary>
        public bool CreateBackupBeforeCleanup { get; set; } = true;
    }

    /// <summary>
    /// 資料庫清理結果
    /// </summary>
    public class DatabaseCleanupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CleanupTime { get; set; }
        public long DeletedRecords { get; set; }
        public long FreedSpaceBytes { get; set; }
        public List<string> CleanupActions { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// 備份文件信息
    /// </summary>
    public class BackupFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long FileSizeBytes { get; set; }
        public bool IsCompressed { get; set; }
        public bool IsValid { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// 備份驗證結果
    /// </summary>
    public class BackupValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ValidationTime { get; set; }
        public long FileSizeBytes { get; set; }
        public bool IsCompressed { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// 資料庫統計信息
    /// </summary>
    public class DatabaseStatistics
    {
        public DateTime GeneratedAt { get; set; }
        public long TotalProducts { get; set; }
        public long TotalInventory { get; set; }
        public long TotalInventoryMovements { get; set; }
        public long TotalPurchases { get; set; }
        public long TotalSales { get; set; }
        public long DatabaseSizeBytes { get; set; }
        public long FreeSpaceBytes { get; set; }
        public DateTime LastBackupTime { get; set; }
        public int BackupCount { get; set; }
    }

    /// <summary>
    /// 資料庫優化結果
    /// </summary>
    public class DatabaseOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime OptimizationTime { get; set; }
        public long FreedSpaceBytes { get; set; }
        public List<string> OptimizationActions { get; set; } = new List<string>();
    }
}