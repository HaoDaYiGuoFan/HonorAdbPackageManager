using System.Windows;
using System.Windows.Media;

namespace HonorAdbPackageManager.Views;

/// <summary>主窗口 code-behind：装配 ViewModel + 选中行按需提取图标</summary>
public partial class MainWindow : Window
{
    private readonly ViewModels.MainViewModel _vm = new();
    private string _serial = string.Empty;

    public MainWindow(DeviceInfo? device)
    {
        InitializeComponent();

        if (device is not null)
        {
            _vm.SetDevice(device);
            _serial = device.Serial;
        }

        DataContext = _vm;

        // 选中行变化 → 后台提取该包图标
        _vm.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName != nameof(ViewModels.MainViewModel.SelectedItem)) return;
            var item = _vm.SelectedItem;
            if (item is null || item.Icon is not null) return;

            var serial = _serial;
            var icon = await Task.Run(() =>
            {
                try { return App.Icons.GetIcon(serial, item); }
                catch { return null; }
            });

            // 提取期间用户可能已切换选中行
            if (icon is not null && ReferenceEquals(_vm.SelectedItem, item))
                item.Icon = icon;
        };

        // 已带设备进入时自动拉取包列表（流程2）
        if (device is { IsOnline: true })
            Loaded += async (_, _) => await _vm.LoadPackagesAsync();
    }
}