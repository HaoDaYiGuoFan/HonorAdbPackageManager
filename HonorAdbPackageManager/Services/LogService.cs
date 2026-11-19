using System.Collections.Concurrent;

namespace HonorAdbPackageManager.Services;

/// <summary>全局日志服务：内存环形存储 + 变更通知 + 文本导出</summary>
public class LogService
{
    private const int MaxEntries = 5000;
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private long _dropped;

    /// <summary>新日志到达（UI 订阅刷新；事件在任意线程触发）</summary>
    public event Action<LogEntry>? EntryAdded;

    public int Count => _entries.Count;

    public void Info(string msg) => Add(LogLevel.Info, msg);
    public void Command(string msg) => Add(LogLevel.Command, msg);
    public void Output(string msg) => Add(LogLevel.Output, msg);
    public void Error(string msg) => Add(LogLevel.Error, msg);

    /// <summary>单条日志最大长度：dumpsys 等大输出截断，避免 UI 与内存被刷爆</summary>
    private const int MaxEntryLength = 4000;

    private void Add(LogLevel level, string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        if (msg.Length > MaxEntryLength)
            msg = msg[..MaxEntryLength] + $"\n…(输出过长，已截断 {msg.Length - MaxEntryLength} 字符)";
        var entry = new LogEntry { Level = level, Message = msg };
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _))
            Interlocked.Increment(ref _dropped);
        EntryAdded?.Invoke(entry);
        AppendToFile(entry);
    }

    /// <summary>获取全部日志文本快照（供日志窗口初始显示）</summary>
    public string ExportToText()
    {
        var sb = new StringBuilder();
        foreach (var e in _entries)
            sb.AppendLine(e.ToLine());
        return sb.ToString();
    }

    /// <summary>导出全部日志到 Logs\log_yyyyMMdd_HHmmss.txt，返回文件路径</summary>
    public string Export()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"HonorAdbPackageManager 日志导出  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        if (_dropped > 0) sb.AppendLine($"(早期日志已截断，丢弃 {_dropped} 条)");
        sb.AppendLine(new string('=', 60));
        foreach (var e in _entries)
            sb.AppendLine(e.ToLine());
        File.WriteAllText(file, sb.ToString(), new UTF8Encoding(false));
        return file;
    }

    /// <summary>当前会话日志文件路径（启动时创建，全程增量写入）</summary>
    private string? _sessionFile;
    private readonly object _fileLock = new();

    /// <summary>
    /// 启动会话日志：在 Logs\ 下创建 log_yyyyMMdd_HHmmss_session.txt，
    /// 之后每条日志实时追加（线程安全）。用于问题反馈时直接提供文件。
    /// </summary>
    public void StartSessionFile()
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(dir);
            _sessionFile = Path.Combine(dir, $"log_{DateTime.Now:yyyyMMdd_HHmmss}_session.txt");
            File.WriteAllText(_sessionFile,
                $"HonorAdbPackageManager 会话日志  {DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine +
                new string('=', 60) + Environment.NewLine,
                new UTF8Encoding(false));
        }
        catch
        {
            _sessionFile = null; // 日志落盘失败不影响主功能
        }
    }

    /// <summary>追加一条日志到会话文件（尽力而为，失败静默）</summary>
    private void AppendToFile(LogEntry entry)
    {
        if (_sessionFile is null) return;
        try
        {
            lock (_fileLock)
                File.AppendAllText(_sessionFile, entry.ToLine() + Environment.NewLine, new UTF8Encoding(false));
        }
        catch { /* 磁盘/权限问题不影响主流程 */ }
    }
}

