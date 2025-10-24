namespace CioSystem.Web.Models
{
    public class BasicSettingsViewModel
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyAddress { get; set; }
        public string? CompanyPhone { get; set; }
        public string? CompanyEmail { get; set; }
        public string DefaultCurrency { get; set; } = "TWD";
        public string DefaultLanguage { get; set; } = "zh-TW";
        public string TimeZone { get; set; } = "Asia/Taipei";
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public string TimeFormat { get; set; } = "HH:mm:ss";
        public int ItemsPerPage { get; set; } = 10;
        public bool EnableNotifications { get; set; } = true;
        public bool EnableAuditLog { get; set; } = true;
    }

    public class SystemInfoViewModel
    {
        public string SystemName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public DateTime ServerTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public long MemoryUsage { get; set; }
        public int ProcessorCount { get; set; }
        public string OperatingSystem { get; set; } = string.Empty;
        public string DotNetVersion { get; set; } = string.Empty;
    }

    public class DatabaseStatsViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalInventory { get; set; }
        public int TotalSales { get; set; }
        public int TotalPurchases { get; set; }
        public string DatabaseSize { get; set; } = string.Empty;
        public DateTime LastBackup { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class DatabaseInfoViewModel
    {
        public string DatabaseType { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabasePath { get; set; } = string.Empty;
        public DateTime LastMigration { get; set; }
        public int TotalTables { get; set; }
        public string DatabaseSize { get; set; } = string.Empty;
    }

    // UserViewModel 已移至 CioSystem.Models.UserViewModel 以避免重複定義

    public class LogEntryViewModel
    {
        public int Id { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string User { get; set; } = string.Empty;
    }

    public class SecuritySettingsViewModel
    {
        public int PasswordMinLength { get; set; } = 8;
        public bool PasswordRequireUppercase { get; set; } = true;
        public bool PasswordRequireLowercase { get; set; } = true;
        public bool PasswordRequireNumbers { get; set; } = true;
        public bool PasswordRequireSpecialChars { get; set; } = true;
        public int SessionTimeout { get; set; } = 30;
        public int MaxLoginAttempts { get; set; } = 5;
        public int LockoutDuration { get; set; } = 15;
        public bool EnableTwoFactor { get; set; } = false;
        public bool EnableAuditLog { get; set; } = true;
    }

}