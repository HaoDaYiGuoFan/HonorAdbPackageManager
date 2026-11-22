using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace HonorAdbPackageManager.Views;

/// <summary>
/// 全局日志窗口：实时显示 ADB 命令与输出，可导出 TXT。
/// 单例复用（重复点击工具栏按钮只打开已有窗口）。
/// 高频日志防卡顿：250ms 窗口内批量合并追加 + 缓冲上限保护，
/// 避免拉取 400+ 包时上万次 AppendText 把 UI 线程打满（未响应）。
/// </summary>
public partial class LogWindow : Window
{
    private static LogWindow? _instance;

    /// <summary>待刷新缓冲（UI 线程写入）</summary>
    private readonly System.Text.StringBuilder _pending = new();
    private DispatcherTimer? _flushTimer;
    private const int MaxPendingChars = 200_000;

    /// <summary>显示区行号计数（会话内连续递增；清空显示后从 1 重新开始）</summary>
    private int _nextLine = 1;

    public static void ShowOrActivate()
    {
        if (_instance is { IsLoaded: true })
        {
            _instance.Activate();
            return;
        }
        _instance = new LogWindow { Owner = Application.Current.MainWindow };
        _instance.Show();
    }

    private LogWindow()
    {
        InitializeComponent();
        RefreshAll();
        App.Log.EntryAdded += OnEntryAdded;
        Closed += (_, _) =>
        {
            App.Log.EntryAdded -= OnEntryAdded;
            _flushTimer?.Stop();
        };
    }

    private void OnEntryAdded(LogEntry entry)
    {
        // 日志产生线程 != UI 线程，切回 UI 线程写入缓冲；定时器统一刷新
        Dispatcher.BeginInvoke(() =>
        {
            _pending.AppendLine(entry.ToLine());
            if (_pending.Length > MaxPendingChars)
            {
                // 缓冲异常膨胀保护：丢弃最旧的
                _pending.Remove(0, _pending.Length - MaxPendingChars / 2);
                _pending.Insert(0, "…(日志刷入过快，中间部分已省略)…" + Environment.NewLine);
            }

            _flushTimer ??= CreateFlushTimer();
            _flushTimer.Start(); // 250ms 后首次/再次触发
        });
    }

    private DispatcherTimer CreateFlushTimer()
    {
        var t = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(250) };
        t.Tick += (_, _) =>
        {
            t.Stop();
            if (_pending.Length == 0) return;

            var chunk = _pending.ToString();
            _pending.Clear();

            LogTextBox.AppendText(FormatWithLineNumbers(chunk));
            TrimIfNeeded();
            if (AutoScrollCheck.IsChecked == true)
                LogTextBox.ScrollToEnd();
        };
        return t;
    }

    /// <summary>给整块日志文本的每个物理行加行号前缀（含 stdout 的多行输出与续行），行号连续递增</summary>
    private string FormatWithLineNumbers(string chunk)
    {
        var sb = new System.Text.StringBuilder(chunk.Length + 64);
        var lines = chunk.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            // 末尾分隔符产生的空行跳过，不占行号
            if (line.Length == 0 && i == lines.Length - 1) continue;
            sb.Append(_nextLine++.ToString().PadLeft(5)).Append("  ").AppendLine(line);
        }
        return sb.ToString();
    }

    /// <summary>显示框上限 40 万字符：超出裁掉最旧一半（落在行边界），防止长会话内存/渲染膨胀</summary>
    private void TrimIfNeeded()
    {
        if (LogTextBox.Text.Length <= 400_000) return;
        var cut = LogTextBox.Text.Length - 200_000;
        // 落到行边界再切，避免把行号前缀截成半行
        while (cut < LogTextBox.Text.Length && LogTextBox.Text[cut] != '\n') cut++;
        var keep = cut < LogTextBox.Text.Length ? LogTextBox.Text[(cut + 1)..] : string.Empty;
        LogTextBox.Text = "…(较早日志已省略，行号仍连续递增；导出 TXT 仍含完整内容)…" + Environment.NewLine + keep;
    }

    private void RefreshAll()
    {
        LogTextBox.Text = FormatWithLineNumbers(App.Log.ExportToText());
        LogTextBox.ScrollToEnd();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _pending.Clear();
        LogTextBox.Clear();
        _nextLine = 1;
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var file = App.Log.Export();
            MsgBox.Show($"日志已导出：\n{file}", "导出成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MsgBox.Show($"导出失败：{ex.Message}", "导出失败",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}