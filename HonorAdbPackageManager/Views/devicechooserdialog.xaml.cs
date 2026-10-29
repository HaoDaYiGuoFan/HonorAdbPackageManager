using System.Windows;
using System.Windows.Controls;

namespace HonorAdbPackageManager.Views;

/// <summary>多设备选择弹窗（流程1-4）</summary>
public partial class DeviceChooserDialog : Window
{
    private readonly IList<DeviceInfo> _devices;

    /// <summary>用户选中的设备</summary>
    public DeviceInfo? Chosen { get; private set; }

    public DeviceChooserDialog(IList<DeviceInfo> devices)
    {
        InitializeComponent();
        _devices = devices;

        var list = new List<DeviceItem>();
        foreach (var d in devices)
        {
            list.Add(new DeviceItem
            {
                Serial = d.Serial,
                State = d.IsUnauthorized ? "待授权" : d.State,
                Model = d.Model,
            });
        }
        DeviceList.ItemsSource = list;
        DeviceList.DisplayMemberPath = nameof(DeviceItem.Display);
        if (DeviceList.Items.Count > 0)
            DeviceList.SelectedIndex = 0;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceList.SelectedIndex < 0 || DeviceList.SelectedIndex >= _devices.Count)
        {
            MessageBox.Show("请先选择一台设备。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Chosen = _devices[DeviceList.SelectedIndex];
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
