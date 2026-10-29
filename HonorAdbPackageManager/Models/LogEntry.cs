namespace HonorAdbPackageManager.Models;

/// <summary>日志级别</summary>
public enum LogLevel
{
    Info,
    Command,
    Output,
    Error,
}

/// <summary>一条日志记录</summary>
public class LogEntry
{
    public DateTime Time { get; init; } = DateTime.Now;
    public LogLevel Level { get; init; } = LogLevel.Info;
    public string Message { get; init; } = string.Empty;

    public string TimeText => Time.ToString("HH:mm:ss.fff");

    public string LevelText => Level switch
    {
        LogLevel.Command => "CMD",
        LogLevel.Output => "OUT",
        LogLevel.Error => "ERR",
        _ => "INF",
    };

    /// <summary>导出用整行文本</summary>
    public string ToLine() => $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] [{LevelText}] {Message}";
}
