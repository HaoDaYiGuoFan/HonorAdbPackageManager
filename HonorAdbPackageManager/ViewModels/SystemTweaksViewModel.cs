using System.Collections.ObjectModel;
using HonorAdbPackageManager.Commands;

namespace HonorAdbPackageManager.ViewModels;

/// <summary>动画调节 + 功能选项卡的共享 ViewModel（同一设备上下文）</summary>
public class SystemTweaksViewModel : ViewModelBase
{
    private readonly MainViewModel _main;

    public SystemTweaksViewModel(MainViewModel main)
    {
        _main = main;

        // ── 动画项 ──
        Animations.Add(new AnimItem("窗口动画缩放", "window_animation_scale", "界面小弹窗出现的过渡动画", "建议 1.5", "1"));
        Animations.Add(new AnimItem("过渡动画缩放", "transition_animation_scale", "界面切换（Activity 跳转）的过渡动画", "建议 1.5", "1"));
        Animations.Add(new AnimItem("动画程序时长缩放", "animator_duration_scale", "应用内动画时长（时钟指针、涟漪等）", "建议 1.5，关动画设 0", "1"));

        // ── 状态栏图标精简复选框 ──
        StatusBarIcons.Add(new IconToggle("NFC 图标", "icon_nfc", true));
        StatusBarIcons.Add(new IconToggle("定位图标", "icon_location", true));
        StatusBarIcons.Add(new IconToggle("省电模式图标", "icon_battery_saver", true));
        StatusBarIcons.Add(new IconToggle("闹钟图标", "icon_alarm_clock", true));
        StatusBarIcons.Add(new IconToggle("HD 高清通话", "icon_hd", true));
        StatusBarIcons.Add(new IconToggle("电池百分比", "status_bar_show_battery_percent", false, isBatteryPercent: true));
        StatusBarIcons.Add(new IconToggle("护眼模式图标", "icon_eyecare", true));
        StatusBarIcons.Add(new IconToggle("网速指示器", "show_network_speed", true));
        StatusBarIcons.Add(new IconToggle("双卡信号图标", "icon_signal_2", true));

        // ── 快捷开关 ──
        QuickToggles.Add(new QuickToggle("保持唤醒（充电时不息屏）", "stay_on_while_plugged_in", "global", "充电时屏幕常亮，调试方便", "0"));
        QuickToggles.Add(new QuickToggle("显示触摸操作", "show_touches", "system", "屏幕显示触摸轨迹圆点", "0"));
        QuickToggles.Add(new QuickToggle("指针位置", "pointer_location", "system", "显示触摸坐标与轨迹", "0"));
        QuickToggles.Add(new QuickToggle("严格模式", "strict_mode", "global", "主线程磁盘/网络操作闪红提示", "0"));
        QuickToggles.Add(new QuickToggle("USB 调试（安全设置）", "adb_enabled", "global", "ADB 调试总开关（关闭后工具断连）", "1", readOnly: true));

        // ── 命令 ──
        ReadAllCommand = new RelayCommand(async _ => await ReadAllAsync(), _ => _main.IsDeviceOnline);
        ApplyAnimationsCommand = new RelayCommand(async _ => await ApplyAnimationsAsync(), _ => _main.IsDeviceOnline);
        ApplyRefreshRateCommand = new RelayCommand(async _ => await ApplyRefreshRateAsync(), _ => _main.IsDeviceOnline);
        ApplyScreenTimeoutCommand = new RelayCommand(async _ => await ApplyScreenTimeoutAsync(), _ => _main.IsDeviceOnline);
        ApplyStatusBarCommand = new RelayCommand(async _ => await ApplyStatusBarAsync(), _ => _main.IsDeviceOnline);
        RestoreStatusBarCommand = new RelayCommand(async _ => await RestoreStatusBarAsync(), _ => _main.IsDeviceOnline);
        ToggleQuickCommand = new RelayCommand(async p => await ToggleQuickAsync(p as QuickToggle), _ => _main.IsDeviceOnline);

        // 设备连接状态变化 → 自动读取
        _main.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsDeviceOnline) && _main.IsDeviceOnline)
                await ReadAllAsync();
        };
    }

    // ────────── 动画调节 ──────────

    public ObservableCollection<AnimItem> Animations { get; } = new();

    public RelayCommand ApplyAnimationsCommand { get; }

    private string _animStatus = string.Empty;
    public string AnimStatus
    {
        get => _animStatus;
        private set { if (_animStatus != value) { _animStatus = value; Raise(); } }
    }

    private async Task ApplyAnimationsAsync()
    {
        var serial = _main.Device?.Serial;
        if (serial is null) return;

        var lines = Animations.Select(a => $"  • {a.DisplayName}: {a.CurrentValue}").ToList();
        var confirm = MsgBox.Show(
            "将修改以下系统动画参数：\n\n" + string.Join(Environment.NewLine, lines) +
            "\n\n修改立即生效（无需重启）。确认执行吗？",
            "修改动画缩放", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes) return;

        int ok = 0;
        foreach (var a in Animations)
        {
            var r = await App.SystemSettings.PutAsync(serial, "global", a.Key, a.CurrentValue);
            if (r.Ok) ok++;
            else AnimStatus = $"「{a.DisplayName}」写入失败：{r.Error}";
        }
        AnimStatus = ok == Animations.Count
            ? $"✓ 全部 {ok} 项动画参数写入成功，立即生效"
            : $"部分成功：{ok}/{Animations.Count}（详见上一条错误）";
        App.Log.Info($"动画参数写入：{ok}/{Animations.Count}");
    }

    // ────────── 亮屏时间 ──────────

    private string _screenTimeoutMinutes = "30";
    public string ScreenTimeoutMinutes
    {
        get => _screenTimeoutMinutes;
        set { if (_screenTimeoutMinutes != value) { _screenTimeoutMinutes = value; Raise(); } }
    }

    public RelayCommand ApplyScreenTimeoutCommand { get; }

    private string _timeoutStatus = string.Empty;
    public string TimeoutStatus
    {
        get => _timeoutStatus;
        private set { if (_timeoutStatus != value) { _timeoutStatus = value; Raise(); } }
    }

    private async Task ApplyScreenTimeoutAsync()
    {
        var serial = _main.Device?.Serial;
        if (serial is null) return;
        if (!int.TryParse(ScreenTimeoutMinutes, out var minutes) || minutes < 5 || minutes > 600)
        {
            MsgBox.Show("请输入 5 ~ 600 之间的分钟数（秒数自动换算：分钟 × 60000ms）。", "输入有误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ms = (minutes * 60000).ToString();
        var confirm = MsgBox.Show(
            $"将亮屏超时设置为 {minutes} 分钟（{ms} 毫秒）。\n\n过长亮屏会增加耗电，确认执行吗？",
            "设置亮屏时间", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes) return;

        var r = await App.SystemSettings.PutAsync(serial, "system", "screen_off_timeout", ms);
        TimeoutStatus = r.Ok
            ? $"✓ 亮屏超时已设为 {minutes} 分钟"
            : $"写入失败：{r.Error}";
    }

    // ────────── 刷新率 ──────────

    private string _refreshRateMin = "90";
    public string RefreshRateMin
    {
        get => _refreshRateMin;
        set { if (_refreshRateMin != value) { _refreshRateMin = value; Raise(); } }
    }

    private string _refreshRateMax = "120";
    public string RefreshRateMax
    {
        get => _refreshRateMax;
        set { if (_refreshRateMax != value) { _refreshRateMax = value; Raise(); } }
    }

    public RelayCommand ApplyRefreshRateCommand { get; }

    private string _refreshStatus = string.Empty;
    public string RefreshStatus
    {
        get => _refreshStatus;
        private set { if (_refreshStatus != value) { _refreshStatus = value; Raise(); } }
    }

    private async Task ApplyRefreshRateAsync()
    {
        var serial = _main.Device?.Serial;
        if (serial is null) return;
        if (!float.TryParse(RefreshRateMin, out var min) || !float.TryParse(RefreshRateMax, out var max)
            || min < 30 || max > 165 || min > max)
        {
            MsgBox.Show("刷新率范围无效：需要 30 ~ 165 之间的数值，且最小值 ≤ 最大值。\n\n常见方案：90/120（默认智能）、60/60（锁定 60Hz 省电）、120/120（锁定 120Hz 全开）。", "输入有误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MsgBox.Show(
            $"将屏幕刷新率范围设为 {min} ~ {max} Hz。\n\n" +
            (min == max ? (min <= 60 ? $"锁定在 {min}Hz（省电）。" : $"锁定在 {min}Hz（流畅优先）。") : "智能动态切换。") +
            "\n\n部分机型系统可能限制该设置，确认执行吗？",
            "设置刷新率", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes) return;

        var r1 = await App.SystemSettings.PutAsync(serial, "system", "min_refresh_rate", min.ToString("0"));
        var r2 = await App.SystemSettings.PutAsync(serial, "system", "peak_refresh_rate", max.ToString("0"));
        RefreshStatus = r1.Ok && r2.Ok
            ? $"✓ 刷新率范围已设为 {min}~{max}Hz"
            : $"写入失败：{(!r1.Ok ? r1.Error : r2.Error)}（荣耀部分机型由 MagicOS 智能调度接管）";
    }

    // ────────── 状态栏图标精简 ──────────

    public ObservableCollection<IconToggle> StatusBarIcons { get; } = new();

    public RelayCommand ApplyStatusBarCommand { get; }
    public RelayCommand RestoreStatusBarCommand { get; }

    private string _statusBarStatus = string.Empty;
    public string StatusBarStatus
    {
        get => _statusBarStatus;
        private set { if (_statusBarStatus != value) { _statusBarStatus = value; Raise(); } }
    }

    private async Task ApplyStatusBarAsync()
    {
        var serial = _main.Device?.Serial;
        if (serial is null) return;

        var changed = StatusBarIcons.Where(i => i.IsChanged).ToList();
        if (changed.Count == 0)
        {
            MsgBox.Show("没有勾选变化，无需执行。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var lines = changed.Select(i => $"  • {i.DisplayName}: {(i.IsChecked ? "显示" : "隐藏")}").ToList();
        var confirm = MsgBox.Show(
            "将修改以下状态栏图标显示：\n\n" + string.Join(Environment.NewLine, lines) +
            "\n\n修改立即生效。确认执行吗？",
            "状态栏精简", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes) return;

        int ok = 0;
        foreach (var i in changed)
        {
            string space = i.IsBatteryPercent ? "system" : "global";
            string value = i.IsBatteryPercent
                ? (i.IsChecked ? "2" : "0")   // battery percent: 2=显示在图标内
                : (i.IsChecked ? "1" : "0");
            var r = await App.SystemSettings.PutAsync(serial, space, i.Key, value);
            if (r.Ok) { ok++; i.Commit(); }
            else StatusBarStatus = $"「{i.DisplayName}」写入失败：{r.Error}";
        }
        StatusBarStatus = ok == changed.Count
            ? $"✓ {ok} 项状态栏设置已生效"
            : $"部分成功：{ok}/{changed.Count}";
    }

    /// <summary>还原默认状态栏显示（全部图标显示 + 电池百分比显示）</summary>
    private async Task RestoreStatusBarAsync()
    {
        var serial = _main.Device?.Serial;
        if (serial is null) return;
        var confirm = MsgBox.Show(
            "将还原全部状态栏图标为默认显示状态（NFC/定位/省电/闹钟/HD/护眼/网速/双卡信号显示，电池百分比显示）。\n\n确认执行吗？",
            "还原状态栏", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes) return;

        int ok = 0;
        foreach (var i in StatusBarIcons)
        {
            string space = i.IsBatteryPercent ? "system" : "global";
            string value = i.IsBatteryPercent ? "2" : "1";
            var r = await App.SystemSettings.PutAsync(serial, space, i.Key, value);
            if (r.Ok) { ok++; i.IsChecked = true; i.Commit(); }
        }
        StatusBarStatus = ok == StatusBarIcons.Count
            ? $"✓ 已还原全部 {ok} 项状态栏默认显示"
            : $"部分成功：{ok}/{StatusBarIcons.Count}";
    }

    // ────────── 快捷开关 ──────────

    public ObservableCollection<QuickToggle> QuickToggles { get; } = new();

    public RelayCommand ToggleQuickCommand { get; }

    private string _quickStatus = string.Empty;
    public string QuickStatus
    {
        get => _quickStatus;
        private set { if (_quickStatus != value) { _quickStatus = value; Raise(); } }
    }

    private async Task ToggleQuickAsync(QuickToggle? t)
    {
        if (t?.ReadOnly == true)
        {
            MsgBox.Show("该项为基础调试开关，请在系统设置中操作（关闭 ADB 调试会导致本工具断连）。", "只读项",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var serial = _main.Device?.Serial;
        if (serial is null || t is null) return;

        var newState = t.IsChecked ? "0" : "1";
        var action = t.IsChecked ? "关闭" : "开启";
        var confirm = MsgBox.Show(
            $"将{action}「{t.DisplayName}」。\n\n说明：{t.Description}\n\n确认执行吗？",
            t.DisplayName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes) { t.Rollback(); return; }

        var r = await App.SystemSettings.PutAsync(serial, t.Space, t.Key, newState);
        if (r.Ok)
        {
            t.IsChecked = !t.IsChecked;
            t.Commit();
            QuickStatus = $"✓ 「{t.DisplayName}」已{action}";
        }
        else
        {
            t.Rollback();
            QuickStatus = $"「{t.DisplayName}」{action}失败：{r.Error}";
        }
    }

    // ────────── 设备信息 ──────────

    private string _aospVersion = "点击【读取设备信息】查询";
    public string AospVersion
    {
        get => _aospVersion;
        private set { if (_aospVersion != value) { _aospVersion = value; Raise(); } }
    }

    private string _buildInfo = string.Empty;
    public string BuildInfo
    {
        get => _buildInfo;
        private set { if (_buildInfo != value) { _buildInfo = value; Raise(); } }
    }

    private string _resolution = string.Empty;
    public string Resolution
    {
        get => _resolution;
        private set { if (_resolution != value) { _resolution = value; Raise(); } }
    }

    private string _currentRefresh = string.Empty;
    public string CurrentRefresh
    {
        get => _currentRefresh;
        private set { if (_currentRefresh != value) { _currentRefresh = value; Raise(); } }
    }

    // ────────── 全部读取 ──────────

    public RelayCommand ReadAllCommand { get; }

    private bool _isReading;
    public bool IsReading
    {
        get => _isReading;
        private set { if (_isReading != value) { _isReading = value; Raise(); } }
    }

    private async Task ReadAllAsync()
    {
        var serial = _main.Device?.Serial;
        if (serial is null || IsReading) return;
        IsReading = true;
        try
        {
            // AOSP/系统版本
            var sdk = await App.SystemSettings.GetPropAsync(serial, "ro.build.version.sdk");
            var release = await App.SystemSettings.GetPropAsync(serial, "ro.build.version.release");
            var magicOs = await App.SystemSettings.GetPropAsync(serial, "ro.build.version.magic");
            var securityPatch = await App.SystemSettings.GetPropAsync(serial, "ro.build.version.security_patch");
            AospVersion = $"Android {release} (API {sdk})" +
                (string.IsNullOrEmpty(magicOs) ? "" : $" · MagicOS {magicOs}") +
                (string.IsNullOrEmpty(securityPatch) ? "" : $"\n安全补丁：{securityPatch}");

            var brand = await App.SystemSettings.GetPropAsync(serial, "ro.product.brand");
            var device = await App.SystemSettings.GetPropAsync(serial, "ro.product.device");
            var incremental = await App.SystemSettings.GetPropAsync(serial, "ro.build.version.incremental");
            BuildInfo = $"{brand} {device} · {incremental}";

            // 分辨率 + 当前刷新率
            Resolution = await App.SystemSettings.GetResolutionAsync(serial);
            CurrentRefresh = await App.SystemSettings.GetCurrentRefreshRateAsync(serial);

            // 动画当前值
            foreach (var a in Animations)
            {
                var r = await App.SystemSettings.GetAsync(serial, "global", a.Key);
                if (r.Ok && !string.IsNullOrEmpty(r.Value))
                    a.SystemValue = r.Value;
            }

            // 亮屏超时当前值（ms → 分钟）
            var timeout = await App.SystemSettings.GetAsync(serial, "system", "screen_off_timeout");
            if (timeout.Ok && int.TryParse(timeout.Value, out var msVal))
                TimeoutStatus = $"当前：{msVal / 60000.0:0.#} 分钟";

            // 快捷开关当前状态
            foreach (var q in QuickToggles.Where(x => !x.ReadOnly))
            {
                var r = await App.SystemSettings.GetAsync(serial, q.Space, q.Key);
                if (r.Ok)
                {
                    var on = r.Value == "1";
                    q.IsChecked = on;
                    q.Commit();
                }
            }
        }
        finally
        {
            IsReading = false;
        }
    }
}

/// <summary>动画缩放条目</summary>
public class AnimItem : ViewModelBase
{
    public string DisplayName { get; }
    public string Key { get; }
    public string Description { get; }
    public string Suggestion { get; }

    private string _currentValue = "1";
    public string CurrentValue
    {
        get => _currentValue;
        set { if (_currentValue != value) { _currentValue = value; Raise(); } }
    }

    private string _systemValue = "";
    public string SystemValue
    {
        get => _systemValue;
        set
        {
            if (_systemValue != value)
            {
                _systemValue = value;
                Raise(nameof(SystemDisplay));
                // 将系统读回值规格化到下拉框选项（1.0→1，0.0→0 等）
                if (!string.IsNullOrEmpty(value))
                {
                    if (float.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var f))
                    {
                        CurrentValue = (f == Math.Floor(f)) ? ((int)f).ToString() : f.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else CurrentValue = value;
                }
            }
        }
    }

    public string SystemDisplay => string.IsNullOrEmpty(SystemValue) ? "" : $"当前系统值：{SystemValue}";

    public AnimItem(string name, string key, string desc, string suggestion, string def)
    {
        DisplayName = name;
        Key = key;
        Description = desc;
        Suggestion = suggestion;
        CurrentValue = def;
    }
}

/// <summary>状态栏图标切换项</summary>
public class IconToggle : ViewModelBase
{
    private bool _isChecked;
    private bool _committed;

    public string DisplayName { get; }
    public string Key { get; }
    public bool IsBatteryPercent { get; }

    public IconToggle(string name, string key, bool defaultOn, bool isBatteryPercent = false)
    {
        DisplayName = name;
        Key = key;
        IsBatteryPercent = isBatteryPercent;
        _isChecked = defaultOn;
        _committed = defaultOn;
    }

    public bool IsChecked
    {
        get => _isChecked;
        set { if (_isChecked != value) { _isChecked = value; Raise(); } }
    }

    /// <summary>勾选状态是否与已生效状态不一致（提示用户可执行）</summary>
    public bool IsChanged => _isChecked != _committed;

    public void Commit() { _committed = _isChecked; Raise(nameof(IsChanged)); }
    public void Rollback() { _isChecked = _committed; Raise(); }
}

/// <summary>快捷开关项</summary>
public class QuickToggle : ViewModelBase
{
    private bool _isChecked;
    private bool _committed;

    public string DisplayName { get; }
    public string Key { get; }
    public string Space { get; }
    public string Description { get; }
    public string DefaultRaw { get; }
    public bool ReadOnly { get; }

    public QuickToggle(string name, string key, string space, string desc, string defaultRaw, bool readOnly = false)
    {
        DisplayName = name;
        Key = key;
        Space = space;
        Description = desc;
        DefaultRaw = defaultRaw;
        ReadOnly = readOnly;
        _isChecked = defaultRaw == "1";
        _committed = _isChecked;
    }

    public bool IsChecked
    {
        get => _isChecked;
        set { if (_isChecked != value) { _isChecked = value; Raise(); } }
    }

    public void Commit() { _committed = _isChecked; }
    public void Rollback() { _isChecked = _committed; Raise(); }
}
