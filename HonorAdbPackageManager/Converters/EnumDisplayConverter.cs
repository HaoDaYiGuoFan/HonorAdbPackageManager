using System.Windows.Data;

namespace HonorAdbPackageManager.Converters;

/// <summary>枚举/值转显示文本（通用 switch 转换器）</summary>
public class EnumDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Models.PackageType t)
            return t switch
            {
                Models.PackageType.ThirdParty => "第三方",
                Models.PackageType.SystemNormal => "系统",
                Models.PackageType.SystemCritical => "核心系统",
                _ => "?",
            };
        if (value is Models.RiskLevel r)
            return r switch
            {
                Models.RiskLevel.Safe => "安全",
                Models.RiskLevel.Low => "低风险",
                Models.RiskLevel.High => "高风险",
                Models.RiskLevel.Forbidden => "禁止操作",
                _ => "?",
            };
        if (value is Models.PackageStatus s)
            return s switch
            {
                Models.PackageStatus.Enabled => "已启用",
                Models.PackageStatus.Disabled => "已停用",
                Models.PackageStatus.Uninstalled => "已卸载",
                _ => "?",
            };
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
