using System.Windows;
using System.Windows.Controls;

namespace HonorAdbPackageManager.Views;

/// <summary>
/// 全局日志窗口：实时显示 ADB 命令与输出，可导出 TXT。
/// 单例复用（重复点击工具栏按钮只打开已有窗口）。
/// </summary>
public partial class LogWindow : Window
{
    private static LogWindow? _instance;

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
        Closed += (_, _) => App.Log.EntryAdded -= OnEntryAdded;
    }

    private void OnEntryAdded(LogEntry entry)
    {
        // 日志产生线程 != UI 线程，切回 UI 追加
        Dispatcher.BeginInvoke(() =>
        {
            LogTextBox.AppendText(entry.ToLine() + Environment.NewLine);
            if (AutoScrollCheck.IsChecked == true)
                LogTextBox.ScrollToEnd();
        });
    }

    private void RefreshAll()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var e in EnumerateLogs())
            sb.AppendLine(e.ToLine());
        LogTextBox.Text = sb.ToString();
        LogTextBox.ScrollToEnd();
    }

    private static IEnumerable<LogEntry> EnumerateLogs()
    {
        // LogService 内部队列不直接暴露；导出方法复用其完整快照
        var tmp = Path.GetTempFileName();
        try
        {
            // 直接复用 Export 的快照逻辑：这里通过反射/复制实现会过度设计，
            // 简化处理：调用 Export 写入临时目录，再读回显示。
            // 但 Export 会写 Logs 目录——为避免副作用，这里改为重放事件缓存。
            // LogService 同时缓存了最近 5000 条，通过 ExportFileOnly 获取文本。
            var text = App.Log.ExportToText();
            foreach (var line in text.Split('\n'))
                yield return new LogEntry { Message = line.TrimEnd('\r') };
        }
        finally
        {
            try { File.Delete(tmp); } catch { }
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) =>
        LogTextBox.Clear();

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
