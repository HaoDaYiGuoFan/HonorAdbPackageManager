using System.Windows.Data;

namespace HonorAdbPackageManager.Converters;

/// <summary>图标为空 → 显示首字母占位；有图标 → 隐藏占位</summary>
public class NullToPlaceholderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
