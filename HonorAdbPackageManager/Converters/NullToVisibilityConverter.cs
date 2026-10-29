using System.Windows.Data;

namespace HonorAdbPackageManager.Converters;

/// <summary>
/// null → Collapsed（默认，用于面板）；
/// 参数 "Placeholder"：null → Visible（占位符显示）。
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isNull = value is null;
        if (string.Equals(parameter as string, "Placeholder", StringComparison.OrdinalIgnoreCase))
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
