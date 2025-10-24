using System;
using System.Threading.Tasks;

namespace CioSystem.Core.Interfaces
{
    /// <summary>
    /// 基礎服務接口
    /// 定義所有服務的通用行為
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// 服務名稱
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// 服務版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 服務是否可用
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 健康檢查
        /// </summary>
        /// <returns>健康狀態</returns>
        Task<ServiceHealthStatus> HealthCheckAsync();

        /// <summary>
        /// 初始化服務
        /// </summary>
        /// <returns>初始化結果</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// 清理資源
        /// </summary>
        /// <returns>清理結果</returns>
        Task<bool> CleanupAsync();
    }

    /// <summary>
    /// 服務健康狀態
    /// </summary>
    public class ServiceHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ResponseTime { get; set; }
    }
}