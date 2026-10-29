namespace HonorAdbPackageManager.Models;

/// <summary>已连接设备信息</summary>
public class DeviceInfo
{
    /// <summary>ADB 序列号</summary>
    public string Serial { get; set; } = string.Empty;

    /// <summary>设备状态（device / offline / unauthorized）</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>型号（ro.product.model）</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>品牌（ro.product.brand）</summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>Android 版本（ro.build.version.release）</summary>
    public string AndroidVersion { get; set; } = string.Empty;

    /// <summary>SDK 版本（ro.build.version.sdk）</summary>
    public string SdkVersion { get; set; } = string.Empty;

    /// <summary>MagicOS 版本（检测不到时回退 EMUI / 显示未知）</summary>
    public string MagicOsVersion { get; set; } = "未知";

    /// <summary>是否为 unauthorized（待授权）</summary>
    public bool IsUnauthorized => State.Equals("unauthorized", StringComparison.OrdinalIgnoreCase);

    /// <summary>是否可用（device）</summary>
    public bool IsOnline => State.Equals("device", StringComparison.OrdinalIgnoreCase);

    public string DisplayName =>
        string.IsNullOrEmpty(Model) ? Serial : $"{Model} ({Serial})";
}
