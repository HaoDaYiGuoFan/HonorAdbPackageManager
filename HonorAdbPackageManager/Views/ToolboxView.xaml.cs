using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HonorAdbPackageManager.Views;

/// <summary>
/// 快速工具箱视图：仿鸿蒙工具箱的彩色分类卡片流式布局。
/// code-behind 动态生成卡片（WrapPanel 自适应宽度），条目按钮全部绑定双重确认命令。
/// </summary>
public partial class ToolboxView : UserControl
{
    public ToolboxView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BuildCards();
        Loaded += (_, _) => BuildCards();
    }

    private ViewModels.ToolboxViewModel? Vm => DataContext as ViewModels.ToolboxViewModel;

    /// <summary>按分类动态生成卡片</summary>
    private void BuildCards()
    {
        if (Vm is null || CardsPanel.Children.Count > 0) return;

        foreach (var cat in Vm.Categories)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 10, 10),
                Padding = new Thickness(1),
                Width = 320,
                VerticalAlignment = VerticalAlignment.Top,
            };
            card.Effect = FindResource("CardShadow") as System.Windows.Media.Effects.Effect;

            var stack = new StackPanel();

            // 彩色标题
            var header = new Border
            {
                Background = cat.HeaderBrush,
                CornerRadius = new CornerRadius(9, 9, 0, 0),
                Padding = new Thickness(0, 8, 0, 8),
            };
            header.Child = new TextBlock
            {
                Text = cat.Name,
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            stack.Children.Add(header);

            // 条目行
            foreach (var item in cat.Items)
            {
                var row = BuildItemRow(item);
                stack.Children.Add(row);
            }

            card.Child = stack;
            CardsPanel.Children.Add(card);
        }
    }

    /// <summary>单条目行：名称 + 状态 + 操作按钮组</summary>
    private Border BuildItemRow(Models.ToolboxItem item)
    {
        var row = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xF0, 0xF3)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(10, 7, 10, 7),
        };

        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };

        // 左：名称 + 状态
        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var nameText = new TextBlock
        {
            Text = item.Name,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x19, 0x19, 0x19)),
        };
        var statusText = new TextBlock { FontSize = 11, Margin = new Thickness(0, 1, 0, 0) };
        statusText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("StatusText") { Source = item });
        statusText.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("StatusText") { Source = item, Converter = new Converters.StatusTextBrushConverter() });
        left.Children.Add(nameText);
        left.Children.Add(statusText);
        Grid.SetColumn(left, 0);

        // 右：按钮组
        var btns = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var actionBtn = new Button
        {
            Padding = new Thickness(10, 3, 10, 3),
            FontSize = 12,
            Command = Vm!.ToolboxActionCommand,
            CommandParameter = item,
        };
        actionBtn.SetBinding(ContentControl.ContentProperty, new System.Windows.Data.Binding("ActionText") { Source = item });
        actionBtn.SetBinding(VisibilityProperty, new System.Windows.Data.Binding("ActionVis") { Source = item });
        actionBtn.SetResourceRef(StyleProperty, "PrimaryButton");

        var restoreBtn = new Button
        {
            Content = "还原",
            Padding = new Thickness(10, 3, 10, 3),
            FontSize = 12,
            Command = Vm!.ToolboxRestoreCommand,
            CommandParameter = item,
        };
        restoreBtn.SetBinding(VisibilityProperty, new System.Windows.Data.Binding("RestoreVis") { Source = item });
        restoreBtn.SetResourceRef(StyleProperty, "GhostButton");

        var forbidden = new TextBlock
        {
            Text = "禁止操作",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x40, 0x26)),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "该组件为系统核心，禁用会导致设备异常",
        };
        forbidden.SetBinding(VisibilityProperty, new System.Windows.Data.Binding("ForbiddenVis") { Source = item });

        var missing = new TextBlock
        {
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x8E)),
            VerticalAlignment = VerticalAlignment.Center,
        };
        missing.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("StatusText") { Source = item });
        missing.SetBinding(VisibilityProperty, new System.Windows.Data.Binding("NotInstalledVis") { Source = item });

        btns.Children.Add(actionBtn);
        btns.Children.Add(restoreBtn);
        btns.Children.Add(forbidden);
        btns.Children.Add(missing);
        Grid.SetColumn(btns, 1);

        grid.Children.Add(left);
        grid.Children.Add(btns);
        row.Child = grid;
        return row;
    }
}

/// <summary>扩展方法：Style 资源引用</summary>
internal static class FrameworkElementExtensions
{
    public static void SetResourceRef(this FrameworkElement fe, System.Windows.DependencyProperty dp, string key) =>
        fe.SetValue(dp, fe.FindResource(key));
}
