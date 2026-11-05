using System.Windows;

namespace HonorAdbPackageManager.Views;

/// <summary>
/// 启动入口窗口：检测 adb.exe 与设备，然后进入 MainWindow。
/// 也承担 adb 缺失时的引导提示（放置后点重新检测）。
/// </summary>
public partial class DevicePickerWindow : Window
{
    private DeviceInfo? _device;

    public DevicePickerWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ProbeAsync();
    }

    private async Task ProbeAsync()
    {
        EnterButton.IsEnabled = false;
        StatusText.Text = "正在检测 ADB 与设备…";

        if (!ExeLocator.IsAvailable)
        {
            StatusText.Text = ExeLocator.LastProbe +
                "\n请将 adb.exe / AdbWinApi.dll / AdbWinUsbApi.dll 放入程序目录 adb\\ 文件夹后点【重新检测】。";
            return;
        }

        var devices = await App.Adb.ListDevicesAsync();
        var online = devices.Where(d => d.IsOnline || d.IsUnauthorized).ToList();

        if (online.Count == 0)
        {
            StatusText.Text = "ADB 就绪，但未检测到设备。\n请插上 USB 线、打开 USB 调试，并在手机上允许 RSA 调试授权，然后点【重新检测】。";
            return;
        }

        if (online.Count == 1)
        {
            _device = online[0];
        }
        else
        {
            var dlg = new DeviceChooserDialog(online) { Owner = this };
            _device = dlg.ShowDialog() == true ? dlg.Chosen : null;
            if (_device is null)
            {
                StatusText.Text = "已取消选择设备。";
                return;
            }
        }

        if (_device.IsUnauthorized)
        {
            StatusText.Text = $"设备 {_device.Serial} 已连接但未授权：请在手机屏幕上点『允许 USB 调试』后重新检测。";
            _device = null;
            return;
        }

        StatusText.Text = $"已检测到设备：{_device.Serial}，正在读取设备信息…";
        var info = await App.Adb.GetDeviceInfoAsync(_device.Serial);
        _device = info;
        StatusText.Text = $"设备就绪：{info.Brand} {info.Model} | {info.MagicOsVersion} | Android {info.AndroidVersion} (API {info.SdkVersion})";
        EnterButton.IsEnabled = true;
    }

    /// <summary>无线 ADB 模式说明（无数据线时引导用户）</summary>
    private void WirelessHelp_Click(object sender, RoutedEventArgs e)
    {
        var msg = @"无线 ADB 连接步骤（手机与电脑需在同一 Wi-Fi）：

① 手机进入 设置 → 关于手机 → 版本号连点 7 次，开启开发者模式
② 开发者选项 → 系统中开启『无线调试』
③ 点『无线调试』进入详情页，记下页面上显示的
   IP 地址和端口号（如 192.168.1.100:37251）
④ 也可以直接点本弹窗下方的【是】，在弹出的输入框中输入
⑤ 输入 手机IP:端口（如 192.168.1.100:37251）确认
⑥ 手机弹出授权提示点『允许』

提示：
• 端口每次开启无线调试会变化，连不上请重新查看
• 首次连接建议先用数据线完成授权更稳定";
        var result = MsgBox.Show(msg + "\n\n\n立即打开无线连接输入框？", "无线 ADB 模式",
            MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.Yes);

        if (result == MessageBoxResult.Yes)
        {
            // 打开无线连接输入框（InputDialog 复用主界面同一逻辑）
            var dlg = new InputDialog(
                "连接无线 ADB",
                "输入手机 IP:端口（如 192.168.1.100:5555）。\n手机需先开启『无线调试』并与电脑处于同一 Wi-Fi。",
                App.Settings.LastWirelessAddress);
            if (dlg.ShowDialog() == true)
            {
                var addr = dlg.InputText?.Trim();
                if (!string.IsNullOrEmpty(addr))
                {
                    App.Settings.LastWirelessAddress = addr;
                    App.Settings.Save();
                    ConnectWireless(addr);
                }
            }
        }
    }

    private async void ConnectWireless(string addr)
    {
        StatusText.Text = $"正在连接 {addr} …";
        try
        {
            var r = await App.Adb.ConnectAsync(addr);
            if (r.StdOut.Contains("connected", StringComparison.OrdinalIgnoreCase))
            {
                App.Settings.LastWirelessAddress = addr;
                App.Settings.Save();
                StatusText.Text = $"无线连接成功：{addr}，正在读取设备信息…";
                await ProbeAsync();
            }
            else
            {
                StatusText.Text = $"无线连接失败：{r.Combined.Trim()}\n请检查 IP:端口 是否正确、手机与电脑是否同一网络。";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"无线连接失败：{ex.Message}";
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await ProbeAsync();

    private void Enter_Click(object sender, RoutedEventArgs e)
    {
        var main = new MainWindow(_device);
        Application.Current.MainWindow = main;
        main.Show();
        Close();
    }
}
