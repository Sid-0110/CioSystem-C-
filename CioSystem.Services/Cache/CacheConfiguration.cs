using System;

namespace CioSystem.Services.Cache
{
    /// <summary>
    /// 快取配置
    /// </summary>
    public class CacheConfiguration
    {
        /// <summary>
        /// 快取大小限制
        /// </summary>
        public int SizeLimit { get; set; } = 1000;

        /// <summary>
        /// 壓縮百分比
        /// </summary>
        public double CompactionPercentage { get; set; } = 0.25;

        /// <summary>
        /// 預設過期時間（分鐘）
        /// </summary>
        public int DefaultExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// 預設過期時間
        /// </summary>
        public TimeSpan DefaultExpiration => TimeSpan.FromMinutes(DefaultExpirationMinutes);

        /// <summary>
        /// 清理間隔（分鐘）
        /// </summary>
        public int CleanupIntervalMinutes { get; set; } = 30;

        /// <summary>
        /// 是否啟用快取統計
        /// </summary>
        public bool EnableStatistics { get; set; } = true;

        /// <summary>
        /// 是否啟用標籤式快取管理
        /// </summary>
        public bool EnableTagBasedManagement { get; set; } = true;
    }
}