using System.Windows;

namespace HonorAdbPackageManager.Views;

/// <summary>
/// 卸载/停用确认弹窗：展示风险，必须手动勾选【我已知晓风险】才能确认。
/// 未知包 / 高风险包标题为红色高危样式。
/// </summary>
public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, Models.PackageItem item, string riskText, bool requireCheckbox)
    {
        InitializeComponent();

        Title = title;
        AppNameText.Text = item.AppLabel;
        PackageText.Text = $"{item.PackageName}    v{item.VersionName}";
        RiskLevelText.Text = $"风险等级：{item.RiskDisplay}" + (item.IsUnknown ? "（未知应用，风险未评估）" : string.Empty);
        RiskBodyText.Text = riskText;

        if (item.IsUnknown || item.UninstallRisk == Models.RiskLevel.High)
        {
            RiskTitleText.Text = "高危操作 - 请务必确认";
        }

        if (!requireCheckbox)
        {
            AckCheckbox.IsChecked = true;
            AckCheckbox.Visibility = Visibility.Collapsed;
            ConfirmButton.IsEnabled = true;
        }
    }

    /// <summary>确认结果（DialogResult=true 即确认）</summary>
    public bool Confirmed => DialogResult == true;

    private void AckCheckbox_Changed(object sender, RoutedEventArgs e) =>
        ConfirmButton.IsEnabled = AckCheckbox.IsChecked == true;

    private void Confirm_Click(object sender, RoutedEventArgs e) =>
        DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        DialogResult = false;
}
