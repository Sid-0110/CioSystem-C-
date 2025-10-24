using Microsoft.AspNetCore.Mvc;
using CioSystem.Services.Monitoring;
using CioSystem.Services.Logging;
using Microsoft.AspNetCore.Authorization;

namespace CioSystem.Web.Controllers
{
    /// <summary>
    /// 監控控制器
    /// 提供系統監控和日誌管理功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 需要身份驗證
    public class MonitoringController : ControllerBase
    {
        private readonly IAdvancedMonitoringService _monitoringService;
        private readonly IStructuredLoggingService _loggingService;
        private readonly ILogger<MonitoringController> _logger;

        public MonitoringController(
            IAdvancedMonitoringService monitoringService,
            IStructuredLoggingService loggingService,
            ILogger<MonitoringController> logger)
        {
            _monitoringService = monitoringService;
            _loggingService = loggingService;
            _logger = logger;
        }

        /// <summary>
        /// 取得系統健康狀態
        /// </summary>
        /// <returns>系統健康狀態</returns>
        [HttpGet("health")]
        public async Task<ActionResult<SystemHealthStatus>> GetSystemHealth()
        {
            try
            {
                var health = await _monitoringService.GetSystemHealthAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得系統健康狀態時發生錯誤");
                return StatusCode(500, "取得系統健康狀態時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得效能統計
        /// </summary>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">結束時間</param>
        /// <returns>效能統計</returns>
        [HttpGet("performance")]
        public async Task<ActionResult<PerformanceStatistics>> GetPerformanceStatistics(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var timeRange = new CioSystem.Services.Monitoring.TimeRange
                {
                    Start = startTime ?? DateTime.UtcNow.AddHours(-24),
                    End = endTime ?? DateTime.UtcNow
                };

                var statistics = await _monitoringService.GetPerformanceStatisticsAsync(timeRange);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得效能統計時發生錯誤");
                return StatusCode(500, "取得效能統計時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得錯誤統計
        /// </summary>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">結束時間</param>
        /// <returns>錯誤統計</returns>
        [HttpGet("errors")]
        public async Task<ActionResult<ErrorStatistics>> GetErrorStatistics(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var timeRange = new CioSystem.Services.Monitoring.TimeRange
                {
                    Start = startTime ?? DateTime.UtcNow.AddHours(-24),
                    End = endTime ?? DateTime.UtcNow
                };

                var statistics = await _monitoringService.GetErrorStatisticsAsync(timeRange);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得錯誤統計時發生錯誤");
                return StatusCode(500, "取得錯誤統計時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得用戶行為分析
        /// </summary>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">結束時間</param>
        /// <returns>用戶行為分析</returns>
        [HttpGet("user-behavior")]
        public async Task<ActionResult<UserBehaviorAnalysis>> GetUserBehaviorAnalysis(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var timeRange = new CioSystem.Services.Monitoring.TimeRange
                {
                    Start = startTime ?? DateTime.UtcNow.AddHours(-24),
                    End = endTime ?? DateTime.UtcNow
                };

                var analysis = await _monitoringService.GetUserBehaviorAnalysisAsync(timeRange);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得用戶行為分析時發生錯誤");
                return StatusCode(500, "取得用戶行為分析時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得監控儀表板資料
        /// </summary>
        /// <returns>儀表板資料</returns>
        [HttpGet("dashboard")]
        public async Task<ActionResult<MonitoringDashboard>> GetDashboard()
        {
            try
            {
                var dashboard = await _monitoringService.GetDashboardDataAsync();
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得監控儀表板資料時發生錯誤");
                return StatusCode(500, "取得監控儀表板資料時發生內部錯誤");
            }
        }

        /// <summary>
        /// 記錄效能指標
        /// </summary>
        /// <param name="request">效能指標請求</param>
        /// <returns>操作結果</returns>
        [HttpPost("metrics")]
        public async Task<ActionResult> RecordMetric([FromBody] MetricRequest request)
        {
            try
            {
                await _monitoringService.RecordMetricAsync(request.MetricName, request.Value, request.Tags);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄效能指標時發生錯誤");
                return StatusCode(500, "記錄效能指標時發生內部錯誤");
            }
        }

        /// <summary>
        /// 記錄自定義事件
        /// </summary>
        /// <param name="request">事件請求</param>
        /// <returns>操作結果</returns>
        [HttpPost("events")]
        public async Task<ActionResult> RecordEvent([FromBody] EventRequest request)
        {
            try
            {
                await _monitoringService.RecordEventAsync(request.EventName, request.Properties, request.Metrics);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄自定義事件時發生錯誤");
                return StatusCode(500, "記錄自定義事件時發生內部錯誤");
            }
        }

        /// <summary>
        /// 記錄用戶行為
        /// </summary>
        /// <param name="request">用戶行為請求</param>
        /// <returns>操作結果</returns>
        [HttpPost("user-behavior")]
        public async Task<ActionResult> RecordUserBehavior([FromBody] UserBehaviorRequest request)
        {
            try
            {
                await _monitoringService.RecordUserBehaviorAsync(request.UserId, request.Action, request.Properties);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄用戶行為時發生錯誤");
                return StatusCode(500, "記錄用戶行為時發生內部錯誤");
            }
        }

        /// <summary>
        /// 記錄業務事件
        /// </summary>
        /// <param name="request">業務事件請求</param>
        /// <returns>操作結果</returns>
        [HttpPost("business-events")]
        public async Task<ActionResult> RecordBusinessEvent([FromBody] BusinessEventRequest request)
        {
            try
            {
                await _monitoringService.RecordBusinessEventAsync(request.EventType, request.EntityType, request.EntityId, request.Properties);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄業務事件時發生錯誤");
                return StatusCode(500, "記錄業務事件時發生內部錯誤");
            }
        }

        /// <summary>
        /// 查詢日誌
        /// </summary>
        /// <param name="query">查詢條件</param>
        /// <returns>日誌記錄</returns>
        [HttpPost("logs/query")]
        public async Task<ActionResult<IEnumerable<StructuredLogEntry>>> QueryLogs([FromBody] LogQuery query)
        {
            try
            {
                var logs = await _loggingService.QueryLogsAsync(query);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢日誌時發生錯誤");
                return StatusCode(500, "查詢日誌時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得日誌統計
        /// </summary>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">結束時間</param>
        /// <returns>日誌統計</returns>
        [HttpGet("logs/statistics")]
        public async Task<ActionResult<LogStatistics>> GetLogStatistics(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var timeRange = new CioSystem.Services.Logging.TimeRange
                {
                    Start = startTime ?? DateTime.UtcNow.AddHours(-24),
                    End = endTime ?? DateTime.UtcNow
                };

                var statistics = await _loggingService.GetLogStatisticsAsync(timeRange);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得日誌統計時發生錯誤");
                return StatusCode(500, "取得日誌統計時發生內部錯誤");
            }
        }

        /// <summary>
        /// 取得日誌分析
        /// </summary>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">結束時間</param>
        /// <returns>日誌分析</returns>
        [HttpGet("logs/analysis")]
        public async Task<ActionResult<LogAnalysis>> GetLogAnalysis(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var timeRange = new CioSystem.Services.Logging.TimeRange
                {
                    Start = startTime ?? DateTime.UtcNow.AddHours(-24),
                    End = endTime ?? DateTime.UtcNow
                };

                var analysis = await _loggingService.GetLogAnalysisAsync(timeRange);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得日誌分析時發生錯誤");
                return StatusCode(500, "取得日誌分析時發生內部錯誤");
            }
        }

        /// <summary>
        /// 匯出日誌
        /// </summary>
        /// <param name="query">查詢條件</param>
        /// <param name="format">匯出格式</param>
        /// <returns>匯出檔案</returns>
        [HttpPost("logs/export")]
        public async Task<ActionResult> ExportLogs([FromBody] LogQuery query, [FromQuery] ExportFormat format = ExportFormat.Json)
        {
            try
            {
                var data = await _loggingService.ExportLogsAsync(query, format);
                var contentType = format switch
                {
                    ExportFormat.Json => "application/json",
                    ExportFormat.Csv => "text/csv",
                    ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ExportFormat.Pdf => "application/pdf",
                    _ => "application/octet-stream"
                };

                var fileName = $"logs_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format.ToString().ToLower()}";
                return File(data, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯出日誌時發生錯誤");
                return StatusCode(500, "匯出日誌時發生內部錯誤");
            }
        }

        /// <summary>
        /// 清理舊日誌
        /// </summary>
        /// <param name="retentionDays">保留天數</param>
        /// <returns>操作結果</returns>
        [HttpPost("logs/cleanup")]
        public async Task<ActionResult> CleanupLogs([FromQuery] int retentionDays = 30)
        {
            try
            {
                await _loggingService.CleanupOldLogsAsync(retentionDays);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理舊日誌時發生錯誤");
                return StatusCode(500, "清理舊日誌時發生內部錯誤");
            }
        }

        /// <summary>
        /// 設定告警規則
        /// </summary>
        /// <param name="rule">告警規則</param>
        /// <returns>操作結果</returns>
        [HttpPost("alerts/rules")]
        public async Task<ActionResult> SetAlertRule([FromBody] AlertRule rule)
        {
            try
            {
                await _monitoringService.SetAlertRuleAsync(rule);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定告警規則時發生錯誤");
                return StatusCode(500, "設定告警規則時發生內部錯誤");
            }
        }

        /// <summary>
        /// 檢查告警條件
        /// </summary>
        /// <returns>操作結果</returns>
        [HttpPost("alerts/check")]
        public async Task<ActionResult> CheckAlerts()
        {
            try
            {
                await _monitoringService.CheckAlertConditionsAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查告警條件時發生錯誤");
                return StatusCode(500, "檢查告警條件時發生內部錯誤");
            }
        }
    }

    // 請求模型
    public class MetricRequest
    {
        public string MetricName { get; set; } = string.Empty;
        public double Value { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
    }

    public class EventRequest
    {
        public string EventName { get; set; } = string.Empty;
        public Dictionary<string, string>? Properties { get; set; }
        public Dictionary<string, double>? Metrics { get; set; }
    }

    public class UserBehaviorRequest
    {
        public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, string>? Properties { get; set; }
    }

    public class BusinessEventRequest
    {
        public BusinessEventType EventType { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public Dictionary<string, string>? Properties { get; set; }
    }
}