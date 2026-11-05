using System.Windows.Data;
using System.Windows.Media;

namespace HonorAdbPackageManager.Converters;

/// <summary>工具箱状态文本 → 颜色（已启用灰、已停用橙、已卸载红、禁止红、未加载灰）</summary>
public class StatusTextBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value as string ?? string.Empty;
        Color c = s switch
        {
            "已启用" => Color.FromRgb(0x19, 0x19, 0x19),
            "已停用" => Color.FromRgb(0xE6, 0x86, 0x00),
            "已卸载" => Color.FromRgb(0xE8, 0x40, 0x26),
            "禁止操作" => Color.FromRgb(0xE8, 0x40, 0x26),
            _ => Color.FromRgb(0x8A, 0x8A, 0x8E),
        };
        var brush = new SolidColorBrush(c);
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
