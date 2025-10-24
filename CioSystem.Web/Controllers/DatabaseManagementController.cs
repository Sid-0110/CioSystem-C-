using CioSystem.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CioSystem.Web.Controllers
{
    /// <summary>
    /// 資料庫管理控制器
    /// 提供資料庫備份、還原、清理的 Web API 接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseManagementController : ControllerBase
    {
        private readonly IDatabaseManagementService _databaseManagementService;
        private readonly ILogger<DatabaseManagementController> _logger;

        public DatabaseManagementController(
            IDatabaseManagementService databaseManagementService,
            ILogger<DatabaseManagementController> logger)
        {
            _databaseManagementService = databaseManagementService;
            _logger = logger;
        }

        /// <summary>
        /// 創建資料庫備份
        /// </summary>
        /// <param name="backupName">備份名稱（可選）</param>
        /// <param name="includeData">是否包含數據</param>
        /// <param name="compress">是否壓縮</param>
        /// <returns>備份結果</returns>
        [HttpPost("backup")]
        public async Task<IActionResult> CreateBackup(
            [FromQuery] string? backupName = null,
            [FromQuery] bool includeData = true,
            [FromQuery] bool compress = true)
        {
            try
            {
                _logger.LogInformation("開始創建資料庫備份，備份名稱: {BackupName}", backupName);

                var backupFilePath = await _databaseManagementService.CreateBackupAsync(backupName, includeData, compress);

                var result = new
                {
                    Success = true,
                    Message = "備份創建成功",
                    BackupFilePath = backupFilePath,
                    FileName = Path.GetFileName(backupFilePath),
                    CreatedAt = DateTime.UtcNow,
                    Compressed = compress
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建資料庫備份失敗");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "備份創建失敗",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 從備份還原資料庫
        /// </summary>
        /// <param name="backupFilePath">備份文件路徑</param>
        /// <param name="createBackupBeforeRestore">還原前是否創建備份</param>
        /// <returns>還原結果</returns>
        [HttpPost("restore")]
        public async Task<IActionResult> RestoreFromBackup(
            [FromBody] RestoreRequest request)
        {
            try
            {
                _logger.LogInformation("開始從備份還原資料庫: {BackupFilePath}", request.BackupFilePath);

                var result = await _databaseManagementService.RestoreFromBackupAsync(
                    request.BackupFilePath,
                    request.CreateBackupBeforeRestore);

                if (result.Success)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = result.Message,
                        RestoreTime = result.RestoreTime,
                        RestoredRecords = result.RestoredRecords,
                        BackupFilePath = result.BackupFilePath
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "還原資料庫失敗");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "還原失敗",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 清理資料庫
        /// </summary>
        /// <param name="options">清理選項</param>
        /// <returns>清理結果</returns>
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupDatabase([FromBody] DatabaseCleanupOptions options)
        {
            try
            {
                _logger.LogInformation("開始清理資料庫");

                var result = await _databaseManagementService.CleanupDatabaseAsync(options);

                if (result.Success)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = result.Message,
                        CleanupTime = result.CleanupTime,
                        DeletedRecords = result.DeletedRecords,
                        FreedSpaceBytes = result.FreedSpaceBytes,
                        CleanupActions = result.CleanupActions
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理資料庫失敗");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "清理失敗",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取所有備份文件
        /// </summary>
        /// <returns>備份文件列表</returns>
        [HttpGet("backups")]
        public async Task<IActionResult> GetBackupFiles()
        {
            try
            {
                var backupFiles = await _databaseManagementService.GetBackupFilesAsync();

                var result = backupFiles.Select(bf => new
                {
                    FilePath = bf.FilePath,
                    FileName = bf.FileName,
                    CreatedAt = bf.CreatedAt,
                    FileSizeBytes = bf.FileSizeBytes,
                    FileSizeMB = Math.Round(bf.FileSizeBytes / 1024.0 / 1024.0, 2),
                    IsCompressed = bf.IsCompressed,
                    IsValid = bf.IsValid,
                    Description = bf.Description
                });

                return Ok(new
                {
                    Success = true,
                    BackupFiles = result,
                    Count = result.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取備份文件列表失敗");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "獲取備份文件列表失敗",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 刪除備份文件
        /// </summary>
        /// <param name="backupFilePath">備份文件路徑</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("backups")]
        public async Task<IActionResult> DeleteBackup([FromQuery] string backupFilePath)
        {
            try
            {
                _logger.LogInformation("刪除備份文件: {BackupFilePath}", backupFilePath);

                var success = await _databaseManagementService.DeleteBackupAsync(backupFilePath);

                if (success)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = "備份文件刪除成功"
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "備份文件不存在或刪除失敗"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除備份文件失敗");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "刪除備份文件失敗",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 驗證備份文件
        /// </summary>
        /// <param name="backupFilePath">備份文件路徑</param>
        /// <returns>驗證結果</returns>
        [HttpGet("backups/validate")]
        public async Task<IActionResult> ValidateBackup([FromQuery] string backupFilePath)
        {
            try
            {
                _logger.LogInformation("驗證備份文件: {BackupFilePath}", backupFilePath);

                var result = await _databaseManagementService.ValidateBackupAsync(backupFilePath);

                return Ok(new
                {
                    Success = true,
                    IsValid = result.IsValid,
                    Message = result.Message,
                    ValidationTime = result.ValidationTime,
                    FileSizeBytes = result.FileSizeBytes,
                    FileSizeMB = Math.Round(result.FileSizeBytes / 1024.0 / 1024.0, 2),
                    IsCompressed = result.IsCompressed,
                    Errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證備份文件失敗");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "驗證備份文件失敗",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取資料庫統計信息
        /// </summary>
        /// <returns>資料庫統計信息</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetDatabaseStatistics()
        {
            try
            {
                var statistics = await _databaseManagementService.GetDatabaseStatisticsAsync();

                return Ok(new
                {
                    Success = true,
                    Statistics = new
                    {
                        GeneratedAt = statistics.GeneratedAt,
                        TotalProducts = statistics.TotalProducts,
                        TotalInventory = statistics.TotalInventory,
                        TotalInventoryMovements = statistics.TotalInventoryMovements,
                        TotalPurchases = statistics.TotalPurchases,
                        TotalSales = statistics.TotalSales,
                        DatabaseSizeBytes = statistics.DatabaseSizeBytes,
                        DatabaseSizeMB = Math.Round(statistics.DatabaseSizeBytes / 1024.0 / 1024.0, 2),
                        FreeSpaceBytes = statistics.FreeSpaceBytes,
                        LastBackupTime = statistics.LastBackupTime,
                        BackupCount = statistics.BackupCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取資料庫統計信息失敗");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "獲取資料庫統計信息失敗",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 優化資料庫
        /// </summary>
        /// <returns>優化結果</returns>
        [HttpPost("optimize")]
        public async Task<IActionResult> OptimizeDatabase()
        {
            try
            {
                _logger.LogInformation("開始優化資料庫");

                var result = await _databaseManagementService.OptimizeDatabaseAsync();

                if (result.Success)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = result.Message,
                        OptimizationTime = result.OptimizationTime,
                        FreedSpaceBytes = result.FreedSpaceBytes,
                        FreedSpaceMB = Math.Round(result.FreedSpaceBytes / 1024.0 / 1024.0, 2),
                        OptimizationActions = result.OptimizationActions
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "優化資料庫失敗");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "優化資料庫失敗",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 下載備份文件
        /// </summary>
        /// <param name="backupFilePath">備份文件路徑</param>
        /// <returns>備份文件</returns>
        [HttpGet("backups/download")]
        public async Task<IActionResult> DownloadBackup([FromQuery] string backupFilePath)
        {
            try
            {
                if (!System.IO.File.Exists(backupFilePath))
                {
                    return NotFound(new { Message = "備份文件不存在" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(backupFilePath);
                var fileName = Path.GetFileName(backupFilePath);
                var contentType = backupFilePath.EndsWith(".zip") ? "application/zip" : "application/octet-stream";

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下載備份文件失敗");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "下載備份文件失敗",
                    Error = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// 還原請求模型
    /// </summary>
    public class RestoreRequest
    {
        public string BackupFilePath { get; set; } = string.Empty;
        public bool CreateBackupBeforeRestore { get; set; } = true;
    }
}