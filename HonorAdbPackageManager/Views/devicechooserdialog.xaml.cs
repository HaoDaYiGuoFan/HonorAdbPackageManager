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
        EnsureOwner();
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
            MsgBox.Show("请先选择一台设备。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Chosen = _devices[DeviceList.SelectedIndex];
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    /// <summary>无 Owner 时自动附加当前活动窗口，保证 CenterOwner 居中生效</summary>
    private void EnsureOwner()
    {
        if (Owner is null)
        {
            Owner = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.IsActive && w != this)
                ?? Application.Current.MainWindow;
            if (Owner == this) Owner = null;
        }
    }}
