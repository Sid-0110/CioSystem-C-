using CioSystem.Core.Interfaces;
using CioSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace CioSystem.Services.Database
{
    /// <summary>
    /// 資料庫管理服務實現
    /// 提供資料庫備份、還原、清理等功能
    /// </summary>
    public class DatabaseManagementService : IDatabaseManagementService
    {
        private readonly CioSystemDbContext _context;
        private readonly ILogger<DatabaseManagementService> _logger;
        private readonly string _backupDirectory;
        private readonly string _databasePath;

        public string ServiceName => "DatabaseManagementService";
        public string Version => "1.0.0";
        public bool IsAvailable => true;

        public DatabaseManagementService(
            CioSystemDbContext context,
            ILogger<DatabaseManagementService> logger)
        {
            _context = context;
            _logger = logger;
            _backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups");
            var connectionString = _context.Database.GetConnectionString();
            _logger.LogInformation("原始連接字串: {ConnectionString}", connectionString);
            
            if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Data Source="))
            {
                // 正確解析 Data Source 部分
                var dataSourcePart = connectionString.Split(';')
                    .FirstOrDefault(part => part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));
                
                if (!string.IsNullOrEmpty(dataSourcePart))
                {
                    _databasePath = dataSourcePart.Split('=').LastOrDefault()?.Trim() ?? "CioSystem.db";
                }
                else
                {
                    _databasePath = "CioSystem.db";
                }
            }
            else
            {
                _databasePath = "CioSystem.db";
            }

            // 確保資料庫路徑是絕對路徑
            if (!Path.IsPathRooted(_databasePath))
            {
                _databasePath = Path.Combine(Directory.GetCurrentDirectory(), _databasePath);
            }

            _logger.LogInformation("資料庫路徑設置為: {DatabasePath}", _databasePath);
            _logger.LogInformation("資料庫文件是否存在: {Exists}", File.Exists(_databasePath));

            // 確保備份目錄存在
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }
        }

        public Task<bool> InitializeAsync()
        {
            try
            {
                _logger.LogInformation("DatabaseManagementService 初始化完成");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DatabaseManagementService 初始化失敗");
                return Task.FromResult(false);
            }
        }

        public Task<bool> CleanupAsync()
        {
            try
            {
                _logger.LogInformation("DatabaseManagementService 正在清理");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DatabaseManagementService 清理失敗");
                return Task.FromResult(false);
            }
        }

        public async Task<ServiceHealthStatus> HealthCheckAsync()
        {
            try
            {
                // 檢查資料庫連接
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return new ServiceHealthStatus
                    {
                        IsHealthy = false,
                        Status = "Unhealthy",
                        Message = "無法連接到資料庫",
                        CheckedAt = DateTime.UtcNow,
                        ResponseTime = TimeSpan.Zero
                    };
                }

                // 檢查備份目錄
                if (!Directory.Exists(_backupDirectory))
                {
                    return new ServiceHealthStatus
                    {
                        IsHealthy = false,
                        Status = "Degraded",
                        Message = "備份目錄不存在",
                        CheckedAt = DateTime.UtcNow,
                        ResponseTime = TimeSpan.Zero
                    };
                }

                return new ServiceHealthStatus
                {
                    IsHealthy = true,
                    Status = "Healthy",
                    Message = "資料庫管理服務運行正常",
                    CheckedAt = DateTime.UtcNow,
                    ResponseTime = TimeSpan.Zero
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫管理服務健康檢查失敗");
                return new ServiceHealthStatus
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    Message = $"健康檢查失敗: {ex.Message}",
                    CheckedAt = DateTime.UtcNow,
                    ResponseTime = TimeSpan.Zero
                };
            }
        }

        public async Task<string> CreateBackupAsync(string? backupName = null, bool includeData = true, bool compress = true)
        {
            try
            {
                _logger.LogInformation("開始創建資料庫備份");

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = backupName ?? $"backup_{timestamp}";
                var extension = compress ? ".zip" : ".db";
                var backupFilePath = Path.Combine(_backupDirectory, $"{fileName}{extension}");

                if (compress)
                {
                    // 創建壓縮備份
                    using var zipStream = new FileStream(backupFilePath, FileMode.Create);
                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

                    // 添加資料庫文件
                    if (File.Exists(_databasePath))
                    {
                        var dbEntry = archive.CreateEntry(Path.GetFileName(_databasePath));
                        using var dbStream = dbEntry.Open();
                        using var dbFileStream = new FileStream(_databasePath, FileMode.Open, FileAccess.Read);
                        await dbFileStream.CopyToAsync(dbStream);
                    }

                    // 添加元數據
                    var metadataEntry = archive.CreateEntry("backup_metadata.json");
                    using var metadataStream = metadataEntry.Open();
                    using var writer = new StreamWriter(metadataStream);
                    var metadata = new
                    {
                        CreatedAt = DateTime.UtcNow,
                        DatabasePath = _databasePath,
                        IncludeData = includeData,
                        Compressed = compress,
                        Version = Version
                    };
                    await writer.WriteAsync(System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    // 創建直接備份
                    if (File.Exists(_databasePath))
                    {
                        File.Copy(_databasePath, backupFilePath, true);
                    }
                }

                _logger.LogInformation("資料庫備份創建成功: {BackupFilePath}", backupFilePath);
                return backupFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建資料庫備份失敗");
                throw;
            }
        }

        public async Task<DatabaseRestoreResult> RestoreFromBackupAsync(string backupFilePath, bool createBackupBeforeRestore = true)
        {
            var result = new DatabaseRestoreResult
            {
                RestoreTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("開始從備份還原資料庫: {BackupFilePath}", backupFilePath);

                if (!File.Exists(backupFilePath))
                {
                    result.Success = false;
                    result.Message = "備份文件不存在";
                    result.Errors.Add("備份文件不存在");
                    return result;
                }

                // 還原前創建備份
                if (createBackupBeforeRestore)
                {
                    var currentBackup = await CreateBackupAsync("before_restore", true, true);
                    result.BackupFilePath = currentBackup;
                    _logger.LogInformation("還原前備份已創建: {BackupFilePath}", currentBackup);
                }

                // 關閉資料庫連接
                await _context.Database.CloseConnectionAsync();

                // 還原資料庫文件
                if (backupFilePath.EndsWith(".zip"))
                {
                    // 從壓縮文件還原
                    using var zipStream = new FileStream(backupFilePath, FileMode.Open);
                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

                    var dbEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".db"));
                    if (dbEntry != null)
                    {
                        using var dbStream = dbEntry.Open();
                        using var dbFileStream = new FileStream(_databasePath, FileMode.Create, FileAccess.Write);
                        await dbStream.CopyToAsync(dbFileStream);
                    }
                }
                else
                {
                    // 直接還原
                    File.Copy(backupFilePath, _databasePath, true);
                }

                // 重新連接資料庫
                await _context.Database.OpenConnectionAsync();

                // 驗證還原結果
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    result.Success = false;
                    result.Message = "還原後無法連接到資料庫";
                    result.Errors.Add("還原後無法連接到資料庫");
                    return result;
                }

                // 統計還原的記錄數
                result.RestoredRecords = await GetTotalRecordCountAsync();

                result.Success = true;
                result.Message = "資料庫還原成功";
                _logger.LogInformation("資料庫還原成功，還原了 {RecordCount} 條記錄", result.RestoredRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫還原失敗");
                result.Success = false;
                result.Message = $"還原失敗: {ex.Message}";
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        public async Task<DatabaseCleanupResult> CleanupDatabaseAsync(DatabaseCleanupOptions cleanupOptions)
        {
            var result = new DatabaseCleanupResult
            {
                CleanupTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("開始清理資料庫");

                // 清理前創建備份
                if (cleanupOptions.CreateBackupBeforeCleanup)
                {
                    var backupPath = await CreateBackupAsync("before_cleanup", true, true);
                    result.CleanupActions.Add($"清理前備份已創建: {backupPath}");
                }

                var deletedRecords = 0L;

                // 清理舊的庫存移動記錄
                if (cleanupOptions.KeepInventoryMovementsDays.HasValue)
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-cleanupOptions.KeepInventoryMovementsDays.Value);
                    var oldMovements = await _context.InventoryMovements
                        .Where(m => m.CreatedAt < cutoffDate)
                        .ToListAsync();

                    if (oldMovements.Any())
                    {
                        _context.InventoryMovements.RemoveRange(oldMovements);
                        deletedRecords += oldMovements.Count;
                        result.CleanupActions.Add($"刪除了 {oldMovements.Count} 條舊的庫存移動記錄");
                    }
                }

                // 清理空的庫存記錄
                if (cleanupOptions.CleanEmptyInventory)
                {
                    var emptyInventory = await _context.Inventory
                        .Where(i => i.Quantity <= 0)
                        .ToListAsync();

                    if (emptyInventory.Any())
                    {
                        _context.Inventory.RemoveRange(emptyInventory);
                        deletedRecords += emptyInventory.Count;
                        result.CleanupActions.Add($"刪除了 {emptyInventory.Count} 條空的庫存記錄");
                    }
                }

                // 清理無效的產品記錄
                if (cleanupOptions.CleanInvalidProducts)
                {
                    var invalidProducts = await _context.Products
                        .Where(p => string.IsNullOrEmpty(p.Name) || string.IsNullOrEmpty(p.SKU))
                        .ToListAsync();

                    if (invalidProducts.Any())
                    {
                        _context.Products.RemoveRange(invalidProducts);
                        deletedRecords += invalidProducts.Count;
                        result.CleanupActions.Add($"刪除了 {invalidProducts.Count} 條無效的產品記錄");
                    }
                }

                // 保存更改
                await _context.SaveChangesAsync();

                // 優化資料庫
                if (cleanupOptions.OptimizeDatabase)
                {
                    var optimizationResult = await OptimizeDatabaseAsync();
                    if (optimizationResult.Success)
                    {
                        result.FreedSpaceBytes += optimizationResult.FreedSpaceBytes;
                        result.CleanupActions.AddRange(optimizationResult.OptimizationActions);
                    }
                }

                result.Success = true;
                result.Message = "資料庫清理完成";
                result.DeletedRecords = deletedRecords;
                _logger.LogInformation("資料庫清理完成，刪除了 {DeletedRecords} 條記錄", deletedRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫清理失敗");
                result.Success = false;
                result.Message = $"清理失敗: {ex.Message}";
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        public async Task<IEnumerable<BackupFileInfo>> GetBackupFilesAsync()
        {
            try
            {
                var backupFiles = new List<BackupFileInfo>();

                if (!Directory.Exists(_backupDirectory))
                {
                    return backupFiles;
                }

                var files = Directory.GetFiles(_backupDirectory, "backup_*")
                    .Concat(Directory.GetFiles(_backupDirectory, "*.zip"))
                    .Concat(Directory.GetFiles(_backupDirectory, "*.db"))
                    .Where(f => !f.Contains("before_") || f.Contains("before_restore") || f.Contains("before_cleanup"))
                    .OrderByDescending(f => File.GetCreationTime(f));

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    var backupInfo = new BackupFileInfo
                    {
                        FilePath = filePath,
                        FileName = fileInfo.Name,
                        CreatedAt = fileInfo.CreationTime,
                        FileSizeBytes = fileInfo.Length,
                        IsCompressed = filePath.EndsWith(".zip"),
                        IsValid = await ValidateBackupFileAsync(filePath)
                    };

                    // 嘗試從文件名提取描述
                    if (filePath.Contains("before_restore"))
                        backupInfo.Description = "還原前備份";
                    else if (filePath.Contains("before_cleanup"))
                        backupInfo.Description = "清理前備份";
                    else if (filePath.Contains("manual"))
                        backupInfo.Description = "手動備份";

                    backupFiles.Add(backupInfo);
                }

                return backupFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取備份文件列表失敗");
                return new List<BackupFileInfo>();
            }
        }

        public async Task<bool> DeleteBackupAsync(string backupFilePath)
        {
            try
            {
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                    _logger.LogInformation("備份文件已刪除: {BackupFilePath}", backupFilePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除備份文件失敗: {BackupFilePath}", backupFilePath);
                return false;
            }
        }

        public async Task<BackupValidationResult> ValidateBackupAsync(string backupFilePath)
        {
            var result = new BackupValidationResult
            {
                ValidationTime = DateTime.UtcNow
            };

            try
            {
                if (!File.Exists(backupFilePath))
                {
                    result.IsValid = false;
                    result.Message = "備份文件不存在";
                    result.Errors.Add("備份文件不存在");
                    return result;
                }

                var fileInfo = new FileInfo(backupFilePath);
                result.FileSizeBytes = fileInfo.Length;
                result.IsCompressed = backupFilePath.EndsWith(".zip");

                if (result.IsCompressed)
                {
                    // 驗證壓縮文件
                    using var zipStream = new FileStream(backupFilePath, FileMode.Open);
                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

                    var dbEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".db"));
                    if (dbEntry == null)
                    {
                        result.IsValid = false;
                        result.Message = "壓縮文件中未找到資料庫文件";
                        result.Errors.Add("壓縮文件中未找到資料庫文件");
                        return result;
                    }
                }
                else
                {
                    // 驗證直接備份文件
                    if (!backupFilePath.EndsWith(".db"))
                    {
                        result.IsValid = false;
                        result.Message = "無效的備份文件格式";
                        result.Errors.Add("無效的備份文件格式");
                        return result;
                    }
                }

                result.IsValid = true;
                result.Message = "備份文件驗證成功";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證備份文件失敗: {BackupFilePath}", backupFilePath);
                result.IsValid = false;
                result.Message = $"驗證失敗: {ex.Message}";
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync()
        {
            try
            {
                var statistics = new DatabaseStatistics
                {
                    GeneratedAt = DateTime.UtcNow
                };

                // 統計各表的記錄數
                statistics.TotalProducts = await _context.Products.CountAsync();
                statistics.TotalInventory = await _context.Inventory.CountAsync();
                statistics.TotalInventoryMovements = await _context.InventoryMovements.CountAsync();
                statistics.TotalPurchases = await _context.Purchases.CountAsync();
                statistics.TotalSales = await _context.Sales.CountAsync();

                // 獲取資料庫文件大小
                _logger.LogInformation("檢查資料庫文件: {DatabasePath}", _databasePath);
                if (File.Exists(_databasePath))
                {
                    var dbFileInfo = new FileInfo(_databasePath);
                    statistics.DatabaseSizeBytes = dbFileInfo.Length;
                    _logger.LogInformation("資料庫文件大小: {Size} bytes", statistics.DatabaseSizeBytes);
                }
                else
                {
                    _logger.LogWarning("資料庫文件不存在: {DatabasePath}", _databasePath);
                    statistics.DatabaseSizeBytes = 0;
                }

                // 獲取備份信息
                var backupFiles = await GetBackupFilesAsync();
                statistics.BackupCount = backupFiles.Count();
                statistics.LastBackupTime = backupFiles.Any() ? backupFiles.Max(b => b.CreatedAt) : DateTime.MinValue;

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取資料庫統計信息失敗");
                return new DatabaseStatistics { GeneratedAt = DateTime.UtcNow };
            }
        }

        public async Task<DatabaseOptimizationResult> OptimizeDatabaseAsync()
        {
            var result = new DatabaseOptimizationResult
            {
                OptimizationTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("開始優化資料庫");

                // 執行 VACUUM 命令來優化 SQLite 資料庫
                await _context.Database.ExecuteSqlRawAsync("VACUUM");
                result.OptimizationActions.Add("執行了 VACUUM 命令");

                // 執行 ANALYZE 命令來更新統計信息
                await _context.Database.ExecuteSqlRawAsync("ANALYZE");
                result.OptimizationActions.Add("執行了 ANALYZE 命令");

                // 獲取優化前後的文件大小
                var beforeSize = File.Exists(_databasePath) ? new FileInfo(_databasePath).Length : 0;

                // 重新創建索引
                await _context.Database.ExecuteSqlRawAsync("REINDEX");
                result.OptimizationActions.Add("重建了所有索引");

                var afterSize = File.Exists(_databasePath) ? new FileInfo(_databasePath).Length : 0;
                result.FreedSpaceBytes = Math.Max(0, beforeSize - afterSize);

                result.Success = true;
                result.Message = "資料庫優化完成";
                _logger.LogInformation("資料庫優化完成，釋放了 {FreedSpace} 字節空間", result.FreedSpaceBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫優化失敗");
                result.Success = false;
                result.Message = $"優化失敗: {ex.Message}";
            }

            return result;
        }

        private async Task<long> GetTotalRecordCountAsync()
        {
            try
            {
                return await _context.Products.CountAsync() +
                       await _context.Inventory.CountAsync() +
                       await _context.InventoryMovements.CountAsync() +
                       await _context.Purchases.CountAsync() +
                       await _context.Sales.CountAsync();
            }
            catch
            {
                return 0;
            }
        }

        private async Task<bool> ValidateBackupFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                if (filePath.EndsWith(".zip"))
                {
                    using var zipStream = new FileStream(filePath, FileMode.Open);
                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                    return archive.Entries.Any(e => e.Name.EndsWith(".db"));
                }
                else
                {
                    return filePath.EndsWith(".db") && new FileInfo(filePath).Length > 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}