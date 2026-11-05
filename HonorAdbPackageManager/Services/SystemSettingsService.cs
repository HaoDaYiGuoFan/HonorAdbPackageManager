using System.Diagnostics;

namespace HonorAdbPackageManager.Services;

/// <summary>settings 命令执行结果</summary>
public class SettingsResult
{
    public bool Ok { get; init; }
    public string Value { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
}

/// <summary>
/// Android settings get/put 封装（global / system / secure 三个命名空间）。
/// 全部走 adb shell settings 命令，无设备时返回失败。
/// </summary>
public class SystemSettingsService
{
    private readonly AdbService _adb;

    public SystemSettingsService(AdbService adb) => _adb = adb;

    /// <summary>读取值（不存在返回空串）</summary>
    public async Task<SettingsResult> GetAsync(string serial, string space, string key)
    {
        var r = await _adb.ShellAsync(serial, $"settings get {space} {key}", 10000);
        var val = r.StdOut.Trim();
        // "null" 表示键不存在
        if (!r.Ok) return new SettingsResult { Ok = false, Error = r.StdErr.Trim() };
        return new SettingsResult { Ok = true, Value = val == "null" ? string.Empty : val };
    }

    /// <summary>写入值</summary>
    public async Task<SettingsResult> PutAsync(string serial, string space, string key, string value)
    {
        var r = await _adb.ShellAsync(serial, $"settings put {space} {key} {value}", 10000);
        if (!r.Ok) return new SettingsResult { Ok = false, Error = r.StdErr.Trim() };
        // settings put 成功通常无输出；验证回读
        var check = await GetAsync(serial, space, key);
        if (check.Value != value)
            return new SettingsResult { Ok = false, Error = $"写入后回读不一致（读到：{check.Value}）——系统可能限制了该设置项" };
        return new SettingsResult { Ok = true, Value = value };
    }

    /// <summary>
    /// 写入 AOSP 状态栏图标黑名单（secure/icon_blacklist，逗号分隔的图标名）。
    /// 这是 SystemUI 真正监听的键；清空时须传 ""，否则 settings put 会因缺少参数失败。
    /// </summary>
    public async Task<SettingsResult> PutIconBlacklistAsync(string serial, string csv)
    {
        var arg = string.IsNullOrEmpty(csv) ? "\"\"" : csv;
        var r = await _adb.ShellAsync(serial, $"settings put secure icon_blacklist {arg}", 10000);
        if (!r.Ok) return new SettingsResult { Ok = false, Error = r.StdErr.Trim() };
        var check = await GetAsync(serial, "secure", "icon_blacklist");
        if (check.Value != csv)
            return new SettingsResult { Ok = false, Error = $"写入后回读不一致（读到：{check.Value}）" };
        return new SettingsResult { Ok = true, Value = csv };
    }

    /// <summary>getprop 只读属性</summary>
    public async Task<string> GetPropAsync(string serial, string prop)
    {
        var r = await _adb.ShellAsync(serial, $"getprop {prop}", 10000);
        return r.Ok ? r.StdOut.Trim() : string.Empty;
    }

    /// <summary>dumpsys display 读取当前刷新率（简化解析）</summary>
    public async Task<string> GetCurrentRefreshRateAsync(string serial)
    {
        var r = await _adb.ShellAsync(serial, "dumpsys display | grep -E 'mActiveModeId|refreshRate|fps' | head -5", 10000);
        return r.StdOut.Trim();
    }

    /// <summary>wm size 读取分辨率</summary>
    public async Task<string> GetResolutionAsync(string serial)
    {
        var r = await _adb.ShellAsync(serial, "wm size", 10000);
        // 输出形如 "Physical size: 1344x2772"
        var line = r.StdOut.Split('\n').FirstOrDefault(l => l.Contains("Physical size") || l.Contains("Override size"));
        return line?.Trim() ?? string.Empty;
    }

    /// <summary>设备是否在线</summary>
    public bool IsDeviceOnline(string? serial) =>
        !string.IsNullOrWhiteSpace(serial);
}
