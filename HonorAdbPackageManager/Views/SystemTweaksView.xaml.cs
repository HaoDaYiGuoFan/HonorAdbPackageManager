using System.Windows;
using System.Windows.Controls;

namespace HonorAdbPackageManager.Views;

/// <summary>
/// 动画调节 + 功能选项卡视图。
/// Tag="AnimOnly" 只显示动画卡；Tag="FuncOnly" 显示设备信息+屏幕/状态栏/快捷开关（不含动画卡）。
/// 两个 Tab 共享同一个 SystemTweaksViewModel（DataContext 由外层绑定注入）。
/// </summary>
public partial class SystemTweaksView : UserControl
{
    public SystemTweaksView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyMode();
    }

    /// <summary>按 Tag 显隐卡片</summary>
    private void ApplyMode()
    {
        var mode = Tag as string ?? string.Empty;

        bool showInfo = mode != "AnimOnly";   // 设备信息条：仅动画模式也显示（方便读当前值）
        bool showAnim = mode != "FuncOnly";
        bool showFunc = mode != "AnimOnly";

        InfoBar.Visibility = showInfo ? Visibility.Visible : Visibility.Collapsed;
        AnimCard.Visibility = showAnim ? Visibility.Visible : Visibility.Collapsed;
        DisplayCard.Visibility = showFunc ? Visibility.Visible : Visibility.Collapsed;
        StatusBarCard.Visibility = showFunc ? Visibility.Visible : Visibility.Collapsed;
        QuickCard.Visibility = showFunc ? Visibility.Visible : Visibility.Collapsed;

        // 动画模式下顶部说明条改成动画相关文案（可选，保持简单不改）
        Grid.SetRowSpan(CardsScroll, 1);
    }
}
