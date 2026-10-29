using System.Windows.Data;

namespace HonorAdbPackageManager.Converters;

/// <summary>风险等级 → 徽章前景色</summary>
public class RiskTextBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Models.RiskLevel r)
        {
            var key = r switch
            {
                Models.RiskLevel.Safe => "SafeBrush",
                Models.RiskLevel.Low => "AccentBrush",
                Models.RiskLevel.High => "WarnBrush",
                Models.RiskLevel.Forbidden => "DangerBrush",
                _ => "TextPrimary",
            };
            return Application.Current.TryFindResource(key)
                   ?? System.Windows.Media.Brushes.Black;
        }
        return System.Windows.Media.Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
