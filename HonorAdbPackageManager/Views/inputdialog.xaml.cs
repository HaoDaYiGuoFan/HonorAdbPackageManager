using System.Windows;

namespace HonorAdbPackageManager.Views;

/// <summary>单行文本输入弹窗（无线 ADB 地址等）</summary>
public partial class InputDialog : Window
{
    public InputDialog(string title, string hint, string defaultValue = "")
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        HintText.Text = hint;
        InputBox.Text = defaultValue;
        Loaded += (_, _) => { InputBox.Focus(); InputBox.SelectAll(); };
    }

    /// <summary>用户输入内容</summary>
    public string InputText => InputBox.Text.Trim();

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
