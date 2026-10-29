using System.Windows.Data;

namespace HonorAdbPackageManager.Converters;

/// <summary>包名 → 列表单元格显示文字：优先 AppLabel，空则显示包名尾段</summary>
public class AppLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string label && !string.IsNullOrWhiteSpace(label))
            return label;
        return "(未知应用)";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
