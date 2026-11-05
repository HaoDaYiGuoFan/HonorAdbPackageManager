using System.Windows;
using HonorAdbPackageManager.Commands;

namespace HonorAdbPackageManager.ViewModels;

/// <summary>主窗口 ViewModel：设备 / 列表 / 操作 / 日志的总控制器</summary>
public class MainViewModel : ViewModelBase
{
    private readonly LogService _log = App.Log;

    public ObservableCollection<PackageItem> Items { get; } = new();
    public ObservableCollection<LogEntry> Logs { get; } = new();

    public RelayCommand RefreshDeviceCommand { get; }
    public RelayCommand LoadPackagesCommand { get; }
    public RelayCommand WirelessConnectCommand { get; }
    public RelayCommand WirelessDisconnectCommand { get; }
    public RelayCommand BackupSelectedCommand { get; }
    public RelayCommand ImportKnowledgeCommand { get; }
    public RelayCommand ExportLogCommand { get; }
    public RelayCommand ShowLogCommand { get; }
    public RelayCommand OpenCacheFolderCommand { get; }
    public RelayCommand PackageActionCommand { get; }

    /// <summary>快速工具箱（仿鸿蒙工具箱选项卡）</summary>
    public ViewModels.ToolboxViewModel Toolbox { get; }

    /// <summary>动画调节 + 功能选项卡</summary>
    public ViewModels.SystemTweaksViewModel Tweaks { get; }
    public RelayCommand RestoreHintCommand { get; }
    public RelayCommand ClearSearchCommand { get; }

    public MainViewModel()
    {
        RefreshDeviceCommand = new(async _ => await RefreshDeviceAsync());
        LoadPackagesCommand = new(async _ => await LoadPackagesAsync(),
            _ => !IsLoadingPackages && Device is { IsOnline: true });
        WirelessConnectCommand = new(async _ => await WirelessConnectAsync());
        WirelessDisconnectCommand = new(async _ => await WirelessDisconnectAsync());
        BackupSelectedCommand = new(async _ => await BackupSelectedAsync(),
            _ => SelectedItem is not null && !IsBusy);
        ImportKnowledgeCommand = new(_ => ImportKnowledge());
        ExportLogCommand = new(_ => ExportLog());
        ShowLogCommand = new(_ => Views.LogWindow.ShowOrActivate());
        OpenCacheFolderCommand = new(_ => OpenFolder(Services.ExeLocator.AdbDir));
        PackageActionCommand = new(async p => await ExecutePackageActionAsync(p as PackageItem));
        RestoreHintCommand = new(_ => ShowRestoreHint(_ as PackageItem));
        ClearSearchCommand = new(_ => SearchText = string.Empty);
        Toolbox = new ToolboxViewModel(this);
        Tweaks = new SystemTweaksViewModel(this);
    }

    // ================= 设备状态 =================

    private DeviceInfo? _device;
    public DeviceInfo? Device
    {
        get => _device;
        private set
        {
            if (_device == value) return;
            _device = value;
            Raise();
            Raise(nameof(DeviceStatusText));
            Raise(nameof(IsDeviceOnline));
        }
    }

    public string DeviceStatusText =>
        Device is null ? "未连接设备"
        : Device.IsUnauthorized ? "设备待授权（请在手机上允许 USB 调试）"
        : Device.IsOnline ? $"已连接：{Device.DisplayName}"
        : $"设备状态异常：{Device.State}";

    public bool IsDeviceOnline => Device is { IsOnline: true };

    private string _adbProbeText = string.Empty;
    public string AdbProbeText
    {
        get => _adbProbeText;
        private set { if (_adbProbeText != value) { _adbProbeText = value; Raise(); } }
    }

    private bool _isRefreshingDevice;
    public bool IsRefreshingDevice
    {
        get => _isRefreshingDevice;
        private set { if (_isRefreshingDevice != value) { _isRefreshingDevice = value; Raise(); } }
    }

    /// <summary>等待用户选择多设备之一（流程1-4）</summary>
    public TaskCompletionSource<DeviceInfo?>? PendingDeviceChoice { get; private set; }

    // ================= 加载状态 =================

    private bool _isLoadingPackages;
    public bool IsLoadingPackages
    {
        get => _isLoadingPackages;
        private set
        {
            if (_isLoadingPackages != value)
            {
                _isLoadingPackages = value;
                Raise();
                Raise(nameof(IsBusy));
                LoadPackagesCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                Raise();
                Raise(nameof(IsIdle));
                BackupSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsIdle => !IsBusy;

    private int _progressDone;
    public int ProgressDone
    {
        get => _progressDone;
        private set { if (_progressDone != value) { _progressDone = value; Raise(); } }
    }

    private int _progressTotal;
    public int ProgressTotal
    {
        get => _progressTotal;
        private set { if (_progressTotal != value) { _progressTotal = value; Raise(); } }
    }

    private string _progressText = string.Empty;
    public string ProgressText
    {
        get => _progressText;
        private set { if (_progressText != value) { _progressText = value; Raise(); } }
    }

    // ================= 列表 =================

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                Raise();
                ApplyFilter();
            }
        }
    }

    private string _typeFilter = "全部";
    public string TypeFilter
    {
        get => _typeFilter;
        set
        {
            if (_typeFilter != value)
            {
                _typeFilter = value;
                Raise();
                ApplyFilter();
            }
        }
    }

    private PackageItem? _selectedItem;
    public PackageItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                Raise();
                Raise(nameof(DetailThirdHint));
                Raise(nameof(DetailSystemHint));
                Raise(nameof(DetailCriticalWarning));
            }
        }
    }

    public string DetailThirdHint =>
        SelectedItem is { PackageType: Models.PackageType.ThirdParty } ? "第三方应用：卸载后需要原 APK 才能还原安装。" : string.Empty;
    public string DetailSystemHint =>
        SelectedItem is { PackageType: Models.PackageType.SystemNormal } ? "荣耀系统可停用包：仅停用，数据保留，可随时一键还原。" : string.Empty;
    public string DetailCriticalWarning =>
        SelectedItem is { PackageType: Models.PackageType.SystemCritical }
            ? "核心系统包：禁用可能导致相机、桌面、通话、指纹等异常，禁止操作！" : string.Empty;

    public ICollectionView FilteredItems => CollectionViewSource.GetDefaultView(Items);

    /// <summary>筛选下拉选项（按类型显示名过滤）</summary>
    public string[] TypeFilterOptions { get; } = { "全部", "第三方", "系统", "核心系统" };

    /// <summary>知识库状态（状态栏）</summary>
    public string KnowledgeSourceText => $"知识库：{App.Knowledge.Source}";

    /// <summary>空态显示开关</summary>
    public bool HasNoItems => Items.Count == 0 && !IsLoadingPackages;

    private string _filterSummary = "尚未加载数据";
    public string FilterSummary
    {
        get => _filterSummary;
        private set { if (_filterSummary != value) { _filterSummary = value; Raise(); } }
    }

    public int TotalCount => Items.Count;
    public int UnknownCount => Items.Count(i => i.IsUnknown);

    private int _enabledCount;
    public int EnabledCount { get => _enabledCount; private set { if (_enabledCount != value) { _enabledCount = value; Raise(); } } }

    private int _disabledCount;
    public int DisabledCount { get => _disabledCount; private set { if (_disabledCount != value) { _disabledCount = value; Raise(); } } }

    private int _uninstalledCount;
    public int UninstalledCount { get => _uninstalledCount; private set { if (_uninstalledCount != value) { _uninstalledCount = value; Raise(); } } }

    /// <summary>会话内已执行操作数</summary>
    private int _sessionActionCount;
    public int SessionActionCount { get => _sessionActionCount; private set { if (_sessionActionCount != value) { _sessionActionCount = value; Raise(); } } }

    public void RefreshCounts()
    {
        EnabledCount = Items.Count(i => i.Status == Models.PackageStatus.Enabled);
        DisabledCount = Items.Count(i => i.Status == Models.PackageStatus.Disabled);
        UninstalledCount = Items.Count(i => i.Status == Models.PackageStatus.Uninstalled);
        Raise(nameof(HasNoItems));
        FilterSummary = Items.Count == 0 ? "尚未加载数据" : $"共 {Items.Count} 个包（启用 {EnabledCount} / 停用 {DisabledCount} / 已卸载 {UninstalledCount}）";
    }

    /// <summary>启动窗口直接注入已检测设备（跳过再次扫描）</summary>
    public void SetDevice(DeviceInfo d)
    {
        Device = d;
        AdbProbeText = $"adb: {ExeLocator.AdbPath}";
        _log.Info($"设备就绪：{d.Brand} {d.Model} | {d.MagicOsVersion} | Android {d.AndroidVersion} (API {d.SdkVersion})");
    }

    /// <summary>搜索 + 筛选联合过滤（流程6）</summary>
    public void ApplyFilter()
    {
        if (FilteredItems is null) return;
        var kw = SearchText?.Trim() ?? string.Empty;
        FilteredItems.Filter = string.IsNullOrEmpty(kw) && TypeFilter == "全部"
            ? null
            : o =>
            {
                if (o is not PackageItem it) return false;
                if (TypeFilter != "全部" && TypeFilter != it.TypeDisplay) return false;
                if (string.IsNullOrEmpty(kw)) return true;
                return it.PackageName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                    || it.AppLabel.Contains(kw, StringComparison.OrdinalIgnoreCase)
                    || it.Description.Contains(kw, StringComparison.OrdinalIgnoreCase)
                    || it.Remark.Contains(kw, StringComparison.OrdinalIgnoreCase);
            };
    }

    // ================= 流程1：设备检测 =================

    public async Task RefreshDeviceAsync()
    {
        if (!Services.ExeLocator.IsAvailable)
        {
            AdbProbeText = Services.ExeLocator.LastProbe;
            MsgBox.Show("未找到 adb.exe，请将其放入程序目录 adb\\ 文件夹。",
                "缺少 ADB", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (IsRefreshingDevice) return;
        IsRefreshingDevice = true;
        try
        {
            var devices = await App.Adb.ListDevicesAsync();
            var online = devices.Where(d => d.IsOnline || d.IsUnauthorized).ToList();

            if (online.Count == 0)
            {
                Device = null;
                _log.Info("未检测到设备。请：1) 插上 USB；2) 手机打开 USB 调试；3) 在手机上允许 RSA 调试授权弹窗。");
                MsgBox.Show("未检测到设备。\n\n请检查：\n1. 手机已插 USB 线\n2. 已开启开发者选项与 USB 调试\n3. 手机弹出的『允许 USB 调试』已点允许",
                    "无设备", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (online.Count == 1)
            {
                await SelectDeviceAsync(online[0]);
            }
            else
            {
                var dlg = new Views.DeviceChooserDialog(online) { Owner = Application.Current.MainWindow };
                var chosen = dlg.ShowDialog() == true ? dlg.Chosen : null;
                if (chosen is not null)
                    await SelectDeviceAsync(chosen);
            }
        }
        finally
        {
            IsRefreshingDevice = false;
        }
    }

    private async Task SelectDeviceAsync(DeviceInfo d)
    {
        if (d.IsUnauthorized)
        {
            Device = d;
            _log.Error("设备未授权：请在手机上允许 USB 调试授权后重新刷新。");
            MsgBox.Show("设备已连接但未授权。\n\n请在手机屏幕上点击『允许 USB 调试』后再点【刷新设备】。",
                "待授权", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            Device = await App.Adb.GetDeviceInfoAsync(d.Serial);
            _log.Info($"设备已连接：{Device.Brand} {Device.Model} | {Device.MagicOsVersion} | Android {Device.AndroidVersion} (API {Device.SdkVersion})");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ================= 流程2：拉取应用包 =================

    public async Task LoadPackagesAsync()
    {
        if (Device is null || !Device.IsOnline)
        {
            MsgBox.Show("请先连接设备。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsLoadingPackages = true;
        ProgressText = "正在获取包列表…";
        Items.Clear();
        RefreshCounts();

        try
        {
            var serial = Device.Serial;

            // 1) 全量 + 分类（-s / -3）
            var all = await App.Adb.ListPackagesAsync(serial, system: false, third: false);
            var sysSet = (await App.Adb.ListPackagesAsync(serial, system: true, third: false))
                .Select(p => p.PackageName).ToHashSet();
            var disabledSet = await App.Adb.ListDisabledPackagesAsync(serial);

            _log.Info($"共获取 {all.Count} 个包（系统 {sysSet.Count} / 已停用 {disabledSet.Count}）");

            ProgressTotal = all.Count;
            ProgressDone = 0;
            ProgressText = "正在解析包详情…";

            var semaphore = new SemaphoreSlim(4);
            var tasks = all.Select(entry => Task.Run(async () =>
            {
                try
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var item = await BuildPackageItemAsync(serial, entry, sysSet, disabledSet);
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Items.Add(item);
                            ProgressDone++;
                            if (ProgressDone % 20 == 0 || ProgressDone == ProgressTotal)
                                ProgressText = $"解析进度 {ProgressDone}/{ProgressTotal}";
                        });
                    }
                    finally { semaphore.Release(); }
                }
                catch { /* 单包失败不影响整体 */ }
            })).ToArray();

            await Task.WhenAll(tasks);
            RefreshCounts();
            ApplyFilter();
            Toolbox.RebindAll();
            ProgressText = $"加载完成：{Items.Count} 个包";
            _log.Info($"包列表加载完成：{Items.Count} 个（未知 {UnknownCount} / 核心禁止 {Items.Count(i => i.PackageType == Models.PackageType.SystemCritical)}）");
        }
        catch (Exception ex)
        {
            _log.Error($"拉取包列表失败：{ex.Message}");
            MsgBox.Show($"拉取包列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoadingPackages = false;
        }
    }

    /// <summary>构建单行 PackageItem：dumpsys + 知识库匹配（流程2-2/2-3）</summary>
    private async Task<PackageItem> BuildPackageItemAsync(
        string serial, PackageEntry entry, HashSet<string> sysSet, HashSet<string> disabledSet)
    {
        var item = new PackageItem { PackageName = entry.PackageName, SourceApkPath = entry.ApkPath };

        var dump = await App.Adb.DumpPackageAsync(serial, entry.PackageName);
        var info = Services.DumpParser.Parse(dump.StdOut);
        if (info is not null)
        {
            item.VersionName = info.VersionName;
            item.SourceApkPath = string.IsNullOrEmpty(info.ApkPath) ? entry.ApkPath : info.ApkPath;
            item.Status = info.Enabled
                ? Models.PackageStatus.Enabled
                : Models.PackageStatus.Disabled;
        }
        if (disabledSet.Contains(entry.PackageName))
            item.Status = Models.PackageStatus.Disabled;

        var isSystem = sysSet.Contains(entry.PackageName) ||
                       (info?.SystemApp ?? false);

        var kb = App.Knowledge.Match(entry.PackageName);
        if (kb is not null)
        {
            item.AppLabel = kb.Name;
            item.Description = kb.Desc;
            item.Remark = kb.Remark;
            item.KbName = kb.Name;
            item.KbDescription = kb.Desc;
            item.UninstallRisk = ParseRisk(kb.Risk);
            item.PackageType = isSystem
                ? (item.UninstallRisk == Models.RiskLevel.Forbidden
                    ? Models.PackageType.SystemCritical
                    : Models.PackageType.SystemNormal)
                : Models.PackageType.ThirdParty;
            item.CanUninstall = kb.CanDisable && item.UninstallRisk != Models.RiskLevel.Forbidden;
        }
        else
        {
            item.AppLabel = entry.PackageName;
            item.Description = string.Empty;
            item.Remark = "未知应用：未收录在知识库中，操作前请确认来源";
            item.IsUnknown = true;
            item.UninstallRisk = Models.RiskLevel.Low;
            item.PackageType = isSystem ? Models.PackageType.SystemNormal : Models.PackageType.ThirdParty;
            item.CanUninstall = true;
        }

        return item;
    }

    private static Models.RiskLevel ParseRisk(string risk) => risk switch
    {
        "安全" => Models.RiskLevel.Safe,
        "低风险" => Models.RiskLevel.Low,
        "高风险" => Models.RiskLevel.High,
        _ => Models.RiskLevel.Low,
    };

    // ================= 流程3/4：卸载/停用/还原 =================

    /// <summary>公开操作入口（快速工具箱复用：内部包含禁止拦截与双重确认）</summary>
    public async Task ExecutePackageActionPublicAsync(PackageItem item) =>
        await ExecutePackageActionAsync(item);

    private async Task ExecutePackageActionAsync(PackageItem? item)
    {
        if (item is null) return;
        if (Device is null || !Device.IsOnline)
        {
            MsgBox.Show("设备未连接。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 禁止操作拦截
        if (!item.CanUninstall)
        {
            MsgBox.Show("该组件为系统核心，禁用会导致设备异常，已拦截操作。",
                "禁止操作", MessageBoxButton.OK, MessageBoxImage.Stop);
            return;
        }

        switch (item.Status)
        {
            case Models.PackageStatus.Enabled:
                await DoDisableOrUninstall(item);
                break;
            case Models.PackageStatus.Disabled:
                await DoEnable(item);
                break;
            case Models.PackageStatus.Uninstalled:
                MsgBox.Show("第三方应用已彻底卸载，无法通过 ADB 还原；\n请使用『备份选中 APK』或本地 APK 重新安装。",
                    "无法还原", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    /// <summary>分支 A：第三方卸载；分支 B：系统包停用（流程3）</summary>
    private async Task DoDisableOrUninstall(PackageItem item)
    {
        var isThird = item.PackageType == Models.PackageType.ThirdParty;
        var riskText = isThird
            ? "这是第三方应用，执行『pm uninstall -k --user 0』将从当前用户卸载。\n卸载后需要原 APK 才能重装。"
            : "这是系统应用，执行『pm disable-user --user 0』将停用该组件（模拟卸载）。\n停用后可随时一键还原。";

        if (item.IsUnknown)
            riskText = "【未知应用】该包未收录在知识库中，风险未知！\n\n" + riskText;

        var confirm = new Views.ConfirmDialog(
            isThird ? "确认卸载第三方应用" : "确认停用系统组件",
            item, riskText, requireCheckbox: true)
        { Owner = Application.Current.MainWindow };

        if (confirm.ShowDialog() != true) return;

        // 双重确认：紧接一次轻量最终确认，防手滑
        var finalHint = isThird
            ? $"最后确认：真的要卸载『{item.AppLabel}』吗？\n\n卸载后需要原 APK 才能恢复。"
            : $"最后确认：真的要停用『{item.AppLabel}』吗？\n\n停用后可随时一键还原。";
        if (MsgBox.Show(finalHint, "最终确认", MessageBoxButton.YesNo, MessageBoxImage.Question,
                MessageBoxResult.No) != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var r = isThird
                ? await App.Adb.UninstallAsync(Device!.Serial, item.PackageName)
                : await App.Adb.DisableAsync(Device!.Serial, item.PackageName);

            var output = r.Combined.Trim();
            // 成功判定：新版系统成功回执有三种形态 ——
            //   "Success"（经典）、空输出（旧版）、"Package xxx new state: disabled-user/enabled/currently-enabled"（Android 12+）
            var success = r.Ok && !output.Contains("Failed", StringComparison.OrdinalIgnoreCase) && !output.Contains("not installed", StringComparison.OrdinalIgnoreCase) && !output.Contains("Exception", StringComparison.OrdinalIgnoreCase);

            if (success)
            {
                if (isThird)
                {
                    item.Status = Models.PackageStatus.Uninstalled;
                    item.AddHistory("卸载成功 (pm uninstall -k --user 0)");
                    _log.Info($"[{item.PackageName}] 卸载成功");
                }
                else
                {
                    item.Status = Models.PackageStatus.Disabled;
                    item.AddHistory("停用成功 (pm disable-user --user 0)");
                    _log.Info($"[{item.PackageName}] 停用成功");
                }
                SessionActionCount++;
                MsgBox.Show($"『{item.AppLabel}』{(isThird ? "卸载" : "停用")}成功。",
                    "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var reason = DescribeFailure(r);
                item.AddHistory($"{(isThird ? "卸载" : "停用")}失败：{reason}");
                _log.Error($"[{item.PackageName}] {(isThird ? "卸载" : "停用")}失败：{reason}");
                MsgBox.Show(
                    $"『{item.AppLabel}』{(isThird ? "卸载" : "停用")}失败。\n\n可能原因：{reason}\n\n详见日志窗口。",
                    "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>还原启用（流程4分支A）</summary>
    private async Task DoEnable(PackageItem item)
    {
        if (MsgBox.Show($"确认还原启用『{item.AppLabel}』吗？",
                "还原确认", MessageBoxButton.YesNo, MessageBoxImage.Question,
                MessageBoxResult.No) != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var r = await App.Adb.EnableAsync(Device!.Serial, item.PackageName);
            var output = r.Combined.Trim();
            // 成功判定：新版系统成功回执有三种形态 ——
            //   "Success"（经典）、空输出（旧版）、"Package xxx new state: disabled-user/enabled/currently-enabled"（Android 12+）
            var success = r.Ok && !output.Contains("Failed", StringComparison.OrdinalIgnoreCase) && !output.Contains("not installed", StringComparison.OrdinalIgnoreCase) && !output.Contains("Exception", StringComparison.OrdinalIgnoreCase);

            if (success)
            {
                item.Status = Models.PackageStatus.Enabled;
                item.AddHistory("还原启用成功 (pm enable)");
                SessionActionCount++;
                _log.Info($"[{item.PackageName}] 还原启用成功");
                MsgBox.Show($"『{item.AppLabel}』已还原启用。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var reason = DescribeFailure(r);
                item.AddHistory($"还原失败：{reason}");
                _log.Error($"[{item.PackageName}] 还原失败：{reason}");
                MsgBox.Show($"『{item.AppLabel}』还原失败。\n\n可能原因：{reason}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string DescribeFailure(AdbResult r)
    {
        var output = r.Combined.Trim();
        if (output.Contains("not installed for user", StringComparison.OrdinalIgnoreCase))
            return "该包未为当前用户安装";
        if (output.Contains("Permission Denial", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("SecurityException", StringComparison.OrdinalIgnoreCase))
            return "系统安全限制，拒绝操作（SPC/系统保护）";
        if (output.Contains("Failed", StringComparison.OrdinalIgnoreCase))
            return output;
        if (!r.Ok)
            return $"adb 返回码 {r.ExitCode}；{output}";
        return output;
    }

    // ================= 流程5：APK 备份 =================

    private async Task BackupSelectedAsync()
    {
        var item = SelectedItem;
        if (item is null)
        {
            MsgBox.Show("请先在列表中选择一个应用。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrEmpty(item.SourceApkPath) || !item.SourceApkPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
        {
            MsgBox.Show("该包没有可备份的 APK 路径（可能是拆分 APK 或无权限读取）。",
                "无法备份", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var serial = Device?.Serial ?? "device";
        var backupDir = Path.Combine(AppContext.BaseDirectory, "Backup", serial);
        IsBusy = true;
        ProgressText = $"正在备份 {item.PackageName} …";
        try
        {
            var r = await App.Adb.PullAsync(serial, item.SourceApkPath, backupDir);
            if (r.Ok)
            {
                item.AddHistory($"APK 已备份到 {backupDir}");
                SessionActionCount++;
                _log.Info($"[{item.PackageName}] APK 备份完成：{backupDir}");
                MsgBox.Show($"备份完成！\n\n目录：{backupDir}", "备份成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _log.Error($"[{item.PackageName}] 备份失败：{r.Combined.Trim()}");
                MsgBox.Show($"备份失败：\n{r.Combined.Trim()}", "备份失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            IsBusy = false;
            ProgressText = string.Empty;
        }
    }

    // ================= 工具栏其他 =================

    private async Task WirelessConnectAsync()
    {
        var dlg = new Views.InputDialog(
            "连接无线 ADB",
            "输入手机 IP:端口（如 192.168.1.100:5555）。\n手机需先在开发者选项中开启『无线调试』并处于同一 Wi-Fi。",
            App.Settings.LastWirelessAddress)
        { Owner = Application.Current.MainWindow };
        if (dlg.ShowDialog() != true) return;
        var addr = dlg.InputText?.Trim();
        if (string.IsNullOrEmpty(addr)) return;

        IsBusy = true;
        try
        {
            var r = await App.Adb.ConnectAsync(addr);
            var ok = r.StdOut.Contains("connected", StringComparison.OrdinalIgnoreCase);
            App.Settings.LastWirelessAddress = addr;
            App.Settings.Save();

            if (ok)
            {
                _log.Info($"无线连接成功：{addr}（已记住地址）");
                await RefreshDeviceAsync();
            }
            else
            {
                _log.Error($"无线连接失败：{r.Combined.Trim()}");
                MsgBox.Show($"连接失败：\n{r.Combined.Trim()}", "无线 ADB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task WirelessDisconnectAsync()
    {
        var serial = Device?.Serial;
        if (serial is not null && serial.Contains(':'))
        {
            await App.Adb.DisconnectAsync(serial);
            _log.Info($"已断开：{serial}");
        }
        else
        {
            await App.Adb.DisconnectAsync();
            _log.Info("已断开全部无线连接");
        }
        Device = null;
    }

    private void ImportKnowledge()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "导入外部知识库 JSON",
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            App.Knowledge.Import(dlg.FileName);
            MsgBox.Show($"知识库导入成功，当前共 {App.Knowledge.Count} 条。\n重新拉取包列表后生效。",
                "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MsgBox.Show($"导入失败：{ex.Message}", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportLog()
    {
        try
        {
            var file = _log.Export();
            _log.Info($"日志已导出：{file}");
            MsgBox.Show($"日志已导出：\n{file}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MsgBox.Show($"导出失败：{ex.Message}", "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>第三方已卸载包的还原引导（流程4分支B）</summary>
    private void ShowRestoreHint(PackageItem? item)
    {
        if (item is null) return;
        var msg = $"『{item.AppLabel}』是第三方应用，已彻底卸载。\n\nADB 无法直接恢复，需要原 APK 文件重新安装（adb install）。\n\n如之前做过备份，请将 APK 放到任意目录后用以下命令安装：\nadb install 路径\\{item.PackageName}.apk";
        MsgBox.Show(msg, "需要 APK 重新安装", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenFolder(string path)
    {
        try { System.Diagnostics.Process.Start("explorer.exe", path); }
        catch { }
    }
}




