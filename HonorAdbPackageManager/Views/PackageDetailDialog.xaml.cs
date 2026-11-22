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

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
