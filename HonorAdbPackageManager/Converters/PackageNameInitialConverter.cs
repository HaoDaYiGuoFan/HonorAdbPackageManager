using System.Windows.Data;

namespace HonorAdbPackageManager.Converters;

/// <summary>包名 → 首字母占位文字（取最后一段首字符大写）</summary>
public class PackageNameInitialConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string name && !string.IsNullOrEmpty(name))
        {
            var last = name.Split('.').LastOrDefault() ?? name;
            return string.IsNullOrEmpty(last) ? "?" : last[..1].ToUpperInvariant();
        }
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
