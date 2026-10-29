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

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await ProbeAsync();

    private void Enter_Click(object sender, RoutedEventArgs e)
    {
        var main = new MainWindow(_device);
        Application.Current.MainWindow = main;
        main.Show();
        Close();
    }
}
