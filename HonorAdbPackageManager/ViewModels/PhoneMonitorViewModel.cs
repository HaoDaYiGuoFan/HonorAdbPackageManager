using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using HonorAdbPackageManager.Commands;

namespace HonorAdbPackageManager.ViewModels;

/// <summary>手机性能监测选项卡：设备状态面板（电量/信号/网络/月流量）+ 游戏性能实时统计（FPS/温度折线图 + Excel 导出）</summary>
public class PhoneMonitorViewModel : ViewModelBase
{
    private readonly MainViewModel _main;
    private readonly DispatcherTimer _statusTimer;

    /// <summary>图表逻辑尺寸（View 内 Viewbox 缩放）</summary>
    public const double ChartW = 760, ChartH = 230;

    // ────────── 设备状态面板 ──────────

    private string _batteryText = "—";
    public string BatteryText { get => _batteryText; private set { if (_batteryText != value) { _batteryText = value; Raise(); } } }

    private string _batteryTempText = "—";
    public string BatteryTempText { get => _batteryTempText; private set { if (_batteryTempText != value) { _batteryTempText = value; Raise(); } } }

    private string _signalText = "—";
    public string SignalText { get => _signalText; private set { if (_signalText != value) { _signalText = value; Raise(); } } }

    private string _networkText = "—";
    public string NetworkText { get => _networkText; private set { if (_networkText != value) { _networkText = value; Raise(); } } }

    private string _monthlyTrafficText = "尚未统计";
    public string MonthlyTrafficText { get => _monthlyTrafficText; private set { if (_monthlyTrafficText != value) { _monthlyTrafficText = value; Raise(); } } }

    private string _statusRefreshText = "";
    public string StatusRefreshText { get => _statusRefreshText; private set { if (_statusRefreshText != value) { _statusRefreshText = value; Raise(); } } }

    private bool _statusBusy;
    private int _statusTick;
    private DateTime _lastTrafficRefresh = DateTime.MinValue;

    public RelayCommand RefreshStatusCommand { get; }

    // ────────── 游戏性能统计 ──────────

    private readonly List<PerformanceSample> _samples = new();
    private Services.PerformanceMonitorService.GameSampler? _sampler;
    private CancellationTokenSource? _monitorCts;
    private Stopwatch? _sessionWatch;
    private int _batteryStart = -1;

    private bool _isMonitoring;
    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set
        {
            if (_isMonitoring != value)
            {
                _isMonitoring = value;
                Raise();
                Raise(nameof(CanStart));
                Raise(nameof(CanExport));
                Raise(nameof(MonitorButtonHint));
            }
        }
    }

    /// <summary>开始统计按钮可用性（监控中置灰，由「监控中…」占位按钮替代显示）</summary>
    public bool CanStart => !_isMonitoring;

    private string _targetText = "未开始（开始统计时自动检测前台应用）";
    public string TargetText { get => _targetText; private set { if (_targetText != value) { _targetText = value; Raise(); } } }

    public string MonitorButtonHint => IsMonitoring ? "正在统计中…" : "";

    private string _liveText = "实时：— FPS";
    public string LiveText { get => _liveText; private set { if (_liveText != value) { _liveText = value; Raise(); } } }

    // 图例实时数值（与折线颜色一一对应，方便区分数值）
    private string _fpsLegendText = "FPS";
    public string FpsLegendText { get => _fpsLegendText; private set { if (_fpsLegendText != value) { _fpsLegendText = value; Raise(); } } }

    private string _batLegendText = "电池温度°C";
    public string BatLegendText { get => _batLegendText; private set { if (_batLegendText != value) { _batLegendText = value; Raise(); } } }

    private string _cpuLegendText = "CPU温度°C";
    public string CpuLegendText { get => _cpuLegendText; private set { if (_cpuLegendText != value) { _cpuLegendText = value; Raise(); } } }

    private string _elapsedText = "";
    public string ElapsedText { get => _elapsedText; private set { if (_elapsedText != value) { _elapsedText = value; Raise(); } } }

    private string _summaryText = "";
    public string SummaryText
    {
        get => _summaryText;
        private set
        {
            if (_summaryText != value)
            {
                _summaryText = value;
                Raise();
                Raise(nameof(HasSummary));
            }
        }
    }

    /// <summary>统计摘要是否已生成（驱动摘要块显示）</summary>
    public bool HasSummary => _summaryText.Length > 0;

    private bool _hasSamples;
    public bool HasSamples { get => _hasSamples; private set { if (_hasSamples != value) { _hasSamples = value; Raise(); Raise(nameof(ChartVisibility)); Raise(nameof(CanExport)); } } }

    public System.Windows.Visibility ChartVisibility => HasSamples ? Visibility.Visible : Visibility.Collapsed;
    public bool CanExport => !IsMonitoring && _samples.Count > 0;

    public RelayCommand StartGameCommand { get; }
    public RelayCommand StopGameCommand { get; }
    public RelayCommand ExportCommand { get; }

    // ────────── 折线图 ──────────

    private PointCollection _fpsPoints = new();
    public PointCollection FpsPoints { get => _fpsPoints; private set { if (_fpsPoints != value) { _fpsPoints = value; Raise(); } } }

    private PointCollection _batTempPoints = new();
    public PointCollection BatTempPoints { get => _batTempPoints; private set { if (_batTempPoints != value) { _batTempPoints = value; Raise(); } } }

    private PointCollection _cpuTempPoints = new();
    public PointCollection CpuTempPoints { get => _cpuTempPoints; private set { if (_cpuTempPoints != value) { _cpuTempPoints = value; Raise(); } } }

    private string _fpsAxisMaxText = "120";
    public string FpsAxisMaxText { get => _fpsAxisMaxText; private set { if (_fpsAxisMaxText != value) { _fpsAxisMaxText = value; Raise(); } } }

    private string _tempAxisMaxText = "60°C";
    public string TempAxisMaxText { get => _tempAxisMaxText; private set { if (_tempAxisMaxText != value) { _tempAxisMaxText = value; Raise(); } } }

    /// <summary>Y 轴刻度（左轴 FPS，蓝）：[最大, 75%, 50%, 25%, 0]</summary>
    private string[] _fpsTicks = ["120", "90", "60", "30", "0"];
    public string[] FpsTicks { get => _fpsTicks; private set { if (_fpsTicks != value) { _fpsTicks = value; Raise(); } } }

    /// <summary>Y 轴刻度（右轴温度，橙）：[最大, 75%, 50%, 25%, 0]</summary>
    private string[] _tempTicks = ["60°", "45°", "30°", "15°", "0°"];
    public string[] TempTicks { get => _tempTicks; private set { if (_tempTicks != value) { _tempTicks = value; Raise(); } } }

    /// <summary>X 轴刻度（时间秒）：[0, 25%, 50%, 75%, 总时长]</summary>
    private string[] _xTicks = ["0", "", "", "", "0秒"];
    public string[] XTicks { get => _xTicks; private set { if (_xTicks != value) { _xTicks = value; Raise(); } } }

    public PhoneMonitorViewModel(MainViewModel main)
    {
        _main = main;
        RefreshStatusCommand = new RelayCommand(async _ => await RefreshStatusAsync(forceTraffic: true));

        StartGameCommand = new RelayCommand(async _ => await StartGameAsync());
        StopGameCommand = new RelayCommand(async _ => await StopGameAsync());
        ExportCommand = new RelayCommand(_ => ExportExcel(), _ => CanExport);

        // 状态自动刷新（5 秒；月流量每 12 次≈1 分钟）
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _statusTimer.Tick += async (_, _) => await RefreshStatusAsync(forceTraffic: false);
        _statusTimer.Start();

        // 设备上线时立即刷新一次
        _main.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsDeviceOnline) && _main.IsDeviceOnline)
                await RefreshStatusAsync(forceTraffic: true);
        };
    }

    private string Serial => _main.Device?.Serial ?? string.Empty;

    // ────────── 状态刷新 ──────────

    private async Task RefreshStatusAsync(bool forceTraffic)
    {
        if (_statusBusy) return;
        var serial = Serial;
        if (serial.Length == 0) { BatteryText = "设备未连接"; return; }

        _statusBusy = true;
        try
        {
            _statusTick++;
            var refreshTraffic = forceTraffic || (DateTime.Now - _lastTrafficRefresh).TotalMinutes >= 1;
            var monthStartUtc = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToUniversalTime();

            var snap = await Services.PerformanceMonitorService.GetStatusSnapshotAsync(serial, monthStartUtc, includeTraffic: refreshTraffic);
            BatteryText = $"电量 {snap.BatteryLevel}%（{snap.BatteryStatusText}）";
            BatteryTempText = $"{snap.BatteryTemp:0.0}°C";
            SignalText = snap.SignalText;
            NetworkText = snap.NetworkText;

            if (refreshTraffic)
            {
                _lastTrafficRefresh = DateTime.Now;
                MonthlyTrafficText = snap.TrafficOk
                    ? $"移动数据：收 {FormatSize(snap.MobileRx)} / 发 {FormatSize(snap.MobileTx)}（合计 {FormatSize(snap.MobileRx + snap.MobileTx)}）\n" +
                      $"Wi-Fi：收 {FormatSize(snap.WifiRx)} / 发 {FormatSize(snap.WifiTx)}（合计 {FormatSize(snap.WifiRx + snap.WifiTx)}）"
                    : "暂无统计数据\n（以系统流量统计范围为准）";
            }
            StatusRefreshText = $"更新于 {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusRefreshText = $"刷新失败：{ex.Message}";
        }
        finally
        {
            _statusBusy = false;
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1L << 30 => $"{bytes / (double)(1L << 30):0.##} GB",
        >= 1L << 20 => $"{bytes / (double)(1L << 20):0.##} MB",
        >= 1L << 10 => $"{bytes / (double)(1L << 10):0.##} KB",
        > 0 => $"{bytes} B",
        _ => "0 B",
    };

    // ────────── 游戏性能统计 ──────────

    private async Task StartGameAsync()
    {
        if (IsMonitoring) return;
        var serial = Serial;
        if (serial.Length == 0)
        {
            MsgBox.Show("设备未连接。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pkg = await Services.PerformanceMonitorService.GetForegroundPackageAsync(serial);
        if (pkg is null)
        {
            MsgBox.Show("无法获取当前前台应用，请确认设备已连接且屏幕已解锁。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (pkg.EndsWith(".launcher", StringComparison.OrdinalIgnoreCase) || pkg.EndsWith("Launcher", StringComparison.Ordinal))
        {
            MsgBox.Show("当前前台是桌面。请先打开要监测的游戏/应用，再点击【开始统计】。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsMonitoring = true;
        _samples.Clear();
        HasSamples = false;
        SummaryText = "";
        _batteryStart = -1;
        _sampler = new Services.PerformanceMonitorService.GameSampler(serial, pkg);
        _sessionWatch = Stopwatch.StartNew();
        var label = _main.Items.FirstOrDefault(i => i.PackageName == pkg)?.AppLabel
                    ?? App.Knowledge.Match(pkg)?.Name ?? string.Empty;
        TargetText = string.IsNullOrEmpty(label)
            ? $"目标：{pkg}（开始统计时的前台应用）"
            : $"目标：{label}（{pkg}） · 开始统计时的前台应用";

        _monitorCts = new CancellationTokenSource();
        var ct = _monitorCts.Token;
        _ = Task.Run(async () =>
        {
            int second = 0;
            while (!ct.IsCancellationRequested)
            {
                PerformanceSample? s = null;
                Exception? err = null;
                try { s = await _sampler.SampleAsync(second); }
                catch (Exception ex) { err = ex; }

                // 回到 UI 线程更新
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (ct.IsCancellationRequested) return;
                    if (s is not null)
                    {
                        _samples.Add(s);
                        if (_batteryStart < 0) _batteryStart = s.BatteryLevel;
                        HasSamples = true;
                        LiveText = $"实时：{s.Fps} FPS · 电池 {s.BatteryTemp:0.0}°C · CPU {s.CpuTemp:0.0}°C · CPU占用 {s.CpuPercent:0}% · 卡顿帧 {s.JankPercent:0}%" +
                                   (s.Fps == 0 ? "（未捕获渲染帧，重试中）" : "");
                        FpsLegendText = $"FPS {s.Fps}";
                        BatLegendText = $"电池温度 {s.BatteryTemp:0.0}°C";
                        CpuLegendText = $"CPU温度 {s.CpuTemp:0.0}°C";
                        ElapsedText = $"已统计 {second + 1} 秒 · {Math.Floor((second + 1) / 60.0):0}分{(second + 1) % 60}秒 · {s.BatteryLevel}% 电量";
                        RedrawChart();
                    }
                    else if (err is not null && second % 5 == 0)
                        LiveText = $"实时：采样异常 {err.Message}";
                });

                second++;
                try { await Task.Delay(1000, ct); }
                catch (OperationCanceledException) { break; }
            }
        }, ct);
    }

    private async Task StopGameAsync()
    {
        if (!IsMonitoring) return;
        _monitorCts?.Cancel();
        await Task.Delay(150);   // 等待循环退出
        IsMonitoring = false;
        _sessionWatch?.Stop();
        _sampler = null;
        SummaryText = BuildSummary();
        if (_samples.Count > 0)
            MsgBox.Show($"统计完成：{_samples.Count} 秒样本。\n\n摘要已显示，可点击【导出 Excel】保存完整数据。",
                "统计完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>统计摘要：帧率均值/峰值/波动、卡顿率、温度、CPU 占用、电量消耗</summary>
    private string BuildSummary()
    {
        if (_samples.Count == 0) return "";
        var fps = _samples.Select(s => (double)s.Fps).Where(f => f > 0).ToList();
        var fpsAll = _samples.Select(s => (double)s.Fps).ToList();
        var avgFps = fps.Count > 0 ? fps.Average() : 0;
        var std = fps.Count > 1 ? Math.Sqrt(fps.Sum(f => (f - avgFps) * (f - avgFps)) / fps.Count) : 0;
        var waveRate = avgFps > 0 ? std / avgFps * 100 : 0;
        var waveLevel = std < 2 ? "低（稳定）" : std < 5 ? "中（一般波动）" : "高（明显波动）";

        var lines = new List<string>
        {
            $"统计时长：{_samples.Count} 秒（{_samples.Count / 60.0:0.#} 分钟） · 样本 {_samples.Count} 个",
            fps.Count > 0
                ? $"帧率：平均 {avgFps:0.#} · 最高 {fps.Max():0} · 最低 {fps.Min():0}"
                : "帧率：未采集到渲染数据（游戏可能使用独立渲染通道）",
            $"帧率波动：±{std:0.#}（波动率 {waveRate:0.#}%，{waveLevel}）",
            $"卡顿帧占比（帧间隔>20ms）：{_samples.Select(s => s.JankPercent).Where(j => j > 0).DefaultIfEmpty(0).Average():0.#}%（平均）",
            $"电池温度：平均 {_samples.Average(s => s.BatteryTemp):0.0}°C · 最高 {_samples.Max(s => s.BatteryTemp):0.0}°C",
            $"CPU 温度：平均 {_samples.Where(s => s.CpuTemp > 0).DefaultIfEmpty().Average(s => s?.CpuTemp ?? 0):0.0}°C · " +
            $"最高 {_samples.Select(s => s.CpuTemp).DefaultIfEmpty(0).Max():0.0}°C",
            $"CPU 占用：平均 {_samples.Average(s => s.CpuPercent):0.#}% · 峰值 {_samples.Max(s => s.CpuPercent):0.#}%",
        };
        if (_batteryStart >= 0 && _samples[^1].BatteryLevel != _batteryStart)
        {
            var drop = _batteryStart - _samples[^1].BatteryLevel;
            var perHour = _samples.Count > 60 ? drop / (_samples.Count / 3600.0) : double.NaN;
            lines.Add($"电量：{_batteryStart}% → {_samples[^1].BatteryLevel}%（下降 {drop}%{(double.IsNaN(perHour) ? "" : $"，约 {perHour:0.#}%/小时")}）");
        }
        return string.Join("\n", lines);
    }

    // ────────── 折线图 ──────────

    private void RedrawChart()
    {
        if (_samples.Count == 0) return;
        var n = _samples.Count;
        // FPS 轴上限取整数步进（5/10/15/20/25/30/40/50/60/80/100/150/200 的 4 倍），
        // 例如 120fps 数据 → 步进 40 → 轴顶 160，刻度 160/120/80/40/0
        var rawMax = Math.Max(60, _samples.Max(s => s.Fps) * 1.1);
        double[] niceSteps = [5, 10, 15, 20, 25, 30, 40, 50, 60, 80, 100, 150, 200];
        var step = niceSteps.FirstOrDefault(s => s >= rawMax / 4, Math.Ceiling(rawMax / 4 / 5) * 5);
        var maxFps = Math.Max(60, step * 4);
        var maxTemp = Math.Max(50, Math.Ceiling(_samples.Max(s => Math.Max(s.BatteryTemp, s.CpuTemp)) / 10.0) * 10);

        var fpsPts = new PointCollection();
        var batPts = new PointCollection();
        var cpuPts = new PointCollection();
        for (int i = 0; i < n; i++)
        {
            var x = n == 1 ? 0 : i / (double)(n - 1) * ChartW;
            var s = _samples[i];
            fpsPts.Add(new Point(x, ChartH - Math.Min(s.Fps / maxFps, 1) * ChartH));
            batPts.Add(new Point(x, ChartH - Math.Min(s.BatteryTemp / maxTemp, 1) * ChartH));
            cpuPts.Add(new Point(x, ChartH - Math.Min(s.CpuTemp / maxTemp, 1) * ChartH));
        }
        FpsPoints = fpsPts;
        BatTempPoints = batPts;
        CpuTempPoints = cpuPts;
        FpsAxisMaxText = $"{maxFps:0}";
        TempAxisMaxText = $"{maxTemp:0}°C";

        // 轴刻度：Y 轴按 25% 分档（FPS 左轴 / 温度右轴），X 轴按 25% 时间分档
        FpsTicks = new[] { maxFps, maxFps * 0.75, maxFps * 0.5, maxFps * 0.25, 0 }
            .Select(v => v >= 10 ? $"{v:0}" : $"{v:0.#}").ToArray();
        TempTicks = new[] { maxTemp, maxTemp * 0.75, maxTemp * 0.5, maxTemp * 0.25, 0 }
            .Select(v => $"{v:0}°").ToArray();
        var total = _samples.Count;
        XTicks = new[] { "0", $"{total / 4.0:0.#}", $"{total / 2.0:0.#}", $"{total * 3 / 4.0:0.#}", $"{total}秒" };
    }

    // ────────── Excel 导出 ──────────

    private void ExportExcel()
    {
        if (_samples.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "导出游戏性能统计",
            Filter = "Excel 工作簿 (*.xlsx)|*.xlsx",
            FileName = $"游戏性能统计_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var detail = new List<IReadOnlyList<object?>>
            {
                new[] { "时间", "相对秒", "FPS", "电池温度°C", "CPU温度°C", "CPU占用%", "卡顿帧占比%", "电量%" },
            };
            var t0 = _samples[0].Time;
            foreach (var s in _samples)
                detail.Add(new object?[]
                {
                    s.Time.ToString("HH:mm:ss"), s.Second, s.Fps,
                    Math.Round(s.BatteryTemp, 1), Math.Round(s.CpuTemp, 1),
                    Math.Round(s.CpuPercent, 1), Math.Round(s.JankPercent, 1), s.BatteryLevel,
                });

            var fps = _samples.Select(s => (double)s.Fps).Where(f => f > 0).ToList();
            var avgFps = fps.Count > 0 ? fps.Average() : 0;
            var std = fps.Count > 1 ? Math.Sqrt(fps.Sum(f => (f - avgFps) * (f - avgFps)) / fps.Count) : 0;
            var summary = new List<IReadOnlyList<object?>>
            {
                new object?[] { "指标", "数值" },
                new object?[] { "目标应用", TargetText },
                new object?[] { "统计时长(秒)", _samples.Count },
                new object?[] { "帧率-平均", Math.Round(avgFps, 1) },
                new object?[] { "帧率-最高", fps.Count > 0 ? fps.Max() : 0 },
                new object?[] { "帧率-最低", fps.Count > 0 ? fps.Min() : 0 },
                new object?[] { "帧率波动(标准差)", Math.Round(std, 2) },
                new object?[] { "波动率%", std / avgFps * 100 },
                new object?[] { "卡顿帧占比%(平均)", Math.Round(_samples.Average(s => s.JankPercent), 2) },
                new object?[] { "电池温度-平均°C", Math.Round(_samples.Average(s => s.BatteryTemp), 1) },
                new object?[] { "电池温度-最高°C", Math.Round(_samples.Max(s => s.BatteryTemp), 1) },
                new object?[] { "CPU温度-平均°C", Math.Round(_samples.Average(s => s.CpuTemp), 1) },
                new object?[] { "CPU温度-最高°C", Math.Round(_samples.Max(s => s.CpuTemp), 1) },
                new object?[] { "CPU占用-平均%", Math.Round(_samples.Average(s => s.CpuPercent), 1) },
                new object?[] { "CPU占用-峰值%", Math.Round(_samples.Max(s => s.CpuPercent), 1) },
                new object?[] { "电量-起始%", _batteryStart },
                new object?[] { "电量-结束%", _samples[^1].BatteryLevel },
            };

            Services.XlsxExporter.Write(dlg.FileName, ("监测明细", detail), ("统计摘要", summary));
            MsgBox.Show($"已导出：\n{dlg.FileName}\n\n包含「监测明细」（{_samples.Count} 行逐秒数据）与「统计摘要」两个工作表。",
                "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            App.Log.Info($"游戏性能统计已导出：{dlg.FileName}");
        }
        catch (Exception ex)
        {
            MsgBox.Show($"导出失败：{ex.Message}", "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
