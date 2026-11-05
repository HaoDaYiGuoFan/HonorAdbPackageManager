using System.Text.Json;

namespace HonorAdbPackageManager.Services;

/// <summary>
/// 本地设置持久化：Data\settings.json。
/// 当前存储：无线 ADB 上次成功连接的地址（输入框默认预填，可修改）。
/// </summary>
public class SettingsService
{
    private static readonly object Lock = new();
    private string _filePath;

    /// <summary>上次无线 ADB 连接地址（IP:端口）</summary>
    public string LastWirelessAddress { get; set; } = string.Empty;

    public SettingsService()
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "Data", "settings.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            var dto = JsonSerializer.Deserialize<SettingsDto>(json);
            if (dto is not null)
                LastWirelessAddress = dto.LastWirelessAddress ?? string.Empty;
        }
        catch
        {
            // 配置损坏时静默回退默认值
        }
    }

    public void Save()
    {
        lock (Lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var dto = new SettingsDto { LastWirelessAddress = LastWirelessAddress };
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json, new UTF8Encoding(false));
            }
            catch
            {
                // 只读目录等情况静默失败，不影响主流程
            }
        }
    }

    private class SettingsDto
    {
        public string LastWirelessAddress { get; set; } = string.Empty;
    }
}
