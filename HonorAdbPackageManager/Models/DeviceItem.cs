namespace HonorAdbPackageManager.Models;

/// <summary>多设备选择窗口的条目</summary>
public class DeviceItem
{
    public string Serial { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    public string Display => string.IsNullOrEmpty(Model)
        ? $"{Serial}  [{State}]"
        : $"{Model}  ({Serial})  [{State}]";
}
