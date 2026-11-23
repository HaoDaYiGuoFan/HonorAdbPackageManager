namespace HonorAdbPackageManager.Views;

/// <summary>
/// 应用详情对话框：点击包列表应用名称打开。
/// 展示：手机权限（按保护级别分块着色，允许/禁止）、占用存储空间、流量使用、启动控制。
/// 打开后异步加载（约 4~7 条 adb 命令），加载完成前显示提示。
/// </summary>
public partial class PackageDetailDialog : Window
{
    private readonly PackageItem _item;
    private readonly string _serial;

    public PackageDetailDialog(PackageItem item, string serial)
    {
        InitializeComponent();
        _item = item;
        _serial = serial;

        HeaderText.Text = string.IsNullOrWhiteSpace(item.AppLabel) ? "(未知应用)" : item.AppLabel;
        PkgText.Text = item.PackageName;
        VersionText.Text = string.IsNullOrWhiteSpace(item.VersionName) ? "" : $"v{item.VersionName}";
        StatusText.Text = item.StatusDisplay;
        Title = $"应用详情 · {HeaderText.Text}";

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var detail = await PackageDetailService.LoadAsync(_serial, _item);
            DataContext = detail;
            LoadingPanel.Visibility = Visibility.Collapsed;
            ContentPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            App.Log.Error($"应用详情查询失败 [{_item.PackageName}]：{ex}");
            LoadErrorText.Text = $"查询失败：{ex.GetType().Name}: {ex.Message}\n" +
                                 "（若反复出现，请查看执行日志；设备断开时请重连后再试）";
        }
    }

    // ────────── 权限开关 ──────────

    private bool _permBusy;

    /// <summary>权限开关点击：启用/停用该权限。
    /// 运行时权限走 pm grant/revoke；悬浮窗/修改设置走 appops set；停用前弹确认（影响使用）。</summary>
    private async void PermToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_permBusy) return;
        if (sender is not CheckBox { DataContext: PermissionItem pi } cb) return;
        if (!pi.CanToggle || DataContext is not PackageDetail detail) return;

        var target = !pi.Granted;                       // 开关目标状态
        var action = target ? "启用" : "停用";

        // 停用影响使用 → 二次确认
        if (!target)
        {
            var confirm = MsgBox.Show(
                $"停用「{pi.DisplayText}」后，『{detail.AppLabel}』依赖该权限的功能可能无法正常使用。\n\n" +
                "可随时重新启用。确认停用吗？",
                "停用权限", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
            {
                cb.IsChecked = pi.Granted;              // 取消：回滚视觉状态
                return;
            }
        }

        _permBusy = true;
        try
        {
            var r = pi.ToggleMode == "appop"
                ? await App.Adb.ShellAsync(_serial, $"appops set {_item.PackageName} {pi.OpName} {(target ? "allow" : "deny")}")
                : await App.Adb.ShellAsync(_serial, $"pm {(target ? "grant" : "revoke")} {_item.PackageName} {pi.Name}");

            if (IsPermOpSuccess(r))
            {
                pi.Granted = target;
                SetPermStatus(
                    target ? $"✓ 已启用「{pi.DisplayText}」" : $"✓ 已停用「{pi.DisplayText}」（应用内可能需重启生效）",
                    safe: true);
                App.Log.Info($"[{_item.PackageName}] 权限{action} {pi.Name}（{pi.ToggleMode}）");
            }
            else
            {
                cb.IsChecked = pi.Granted;              // 失败：回滚视觉状态
                var reason = r.Combined.Trim();
                SetPermStatus($"✗ {action}失败：{(reason.Length > 0 ? reason : $"adb 返回码 {r.ExitCode}")}", safe: false);
                App.Log.Error($"[{_item.PackageName}] 权限{action}失败 {pi.Name}：{reason}");
            }
        }
        catch (Exception ex)
        {
            cb.IsChecked = pi.Granted;
            SetPermStatus($"✗ {action}失败：{ex.Message}", safe: false);
        }
        finally
        {
            _permBusy = false;
        }
    }

    /// <summary>pm grant/revoke / appops set 成功判定：成功时输出为空，异常时含错误文本</summary>
    private static bool IsPermOpSuccess(AdbResult r)
    {
        var outp = r.Combined.Trim();
        return r.Ok && !new[] { "Exception", "Failure", "not allowed", "Bad argument", "Unknown", "denied" }
            .Any(k => outp.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private void SetPermStatus(string text, bool safe)
    {
        PermOpStatus.Text = text;
        PermOpStatus.Foreground = (Brush)FindResource(safe ? "SafeBrush" : "DangerBrush");
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
