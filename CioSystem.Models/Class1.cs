namespace CioSystem.Models;

public class LogEntryViewModel
{
    public int Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string User { get; set; } = string.Empty;
}

public class LogStatisticsViewModel
{
    public int InfoCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public int DebugCount { get; set; }
    public int TotalCount { get; set; }
    public DateTime? LastLogTime { get; set; }
    public string MostActiveUser { get; set; } = string.Empty;
    public string MostCommonLevel { get; set; } = string.Empty;
}

public class LogFilterViewModel
{
    public string? Level { get; set; }
    public string? User { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchKeyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
