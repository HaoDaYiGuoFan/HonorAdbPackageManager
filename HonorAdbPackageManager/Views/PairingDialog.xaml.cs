using System.Windows;

namespace HonorAdbPackageManager.Views;

/// <summary>
/// 无线 ADB 配对弹窗：输入手机「使用配对码配对设备」弹窗显示的一次性
/// IP:端口 与 6 位配对码，调用 adb pair 完成首次信任建立。
/// </summary>
public partial class PairingDialog : Window
{
    public PairingDialog(string defaultAddr = "")
    {
        InitializeComponent();
        EnsureOwner();
        AddrBox.Text = defaultAddr;
        Loaded += (_, _) => { AddrBox.Focus(); AddrBox.SelectAll(); };
    }

    /// <summary>配对地址（IP:端口）</summary>
    public string PairAddress => AddrBox.Text.Trim();

    /// <summary>6 位配对码</summary>
    public string PairCode => CodeBox.Text.Trim();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var addr = AddrBox.Text.Trim();
        var code = CodeBox.Text.Trim();

        if (addr.Split(':').Length != 2 || string.IsNullOrWhiteSpace(addr.Split(':').FirstOrDefault()) || !int.TryParse(addr.Split(':').Skip(1).FirstOrDefault(), out _))
        {
            MessageBox.Show(this, "配对地址格式应为 IP:端口（如 192.168.3.18:41253）。\n注意：这是手机配对弹窗显示的端口，不是无线调试主页面显示的连接端口。",
                "地址无效", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (code.Length != 6 || !code.All(char.IsAsciiLetterOrDigit))
        {
            MessageBox.Show(this, "配对码应为 6 位数字/字母组合，以手机弹窗显示为准。", "配对码无效",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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
    }
}
