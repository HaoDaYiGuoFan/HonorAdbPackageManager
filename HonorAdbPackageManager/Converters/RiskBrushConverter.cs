using System.Windows.Data;

namespace HonorAdbPackageManager.Converters;

/// <summary>风险等级 → 徽章背景色</summary>
public class RiskBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Models.RiskLevel r)
        {
            return r switch
            {
                Models.RiskLevel.Safe => Application.Current.TryFindResource("SafeLightBrush")
                    ?? System.Windows.Media.Brushes.Gray,
                Models.RiskLevel.Low => Application.Current.TryFindResource("InfoLightBrush")
                    ?? System.Windows.Media.Brushes.Gray,
                Models.RiskLevel.High => Application.Current.TryFindResource("WarnLightBrush")
                    ?? System.Windows.Media.Brushes.Gray,
                Models.RiskLevel.Forbidden => Application.Current.TryFindResource("DangerLightBrush")
                    ?? System.Windows.Media.Brushes.Gray,
                _ => System.Windows.Media.Brushes.Gray,
            };
        }
        return System.Windows.Media.Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
