using System.Reflection;

namespace HonorAdbPackageManager.Services;

/// <summary>
/// 定位 adb.exe：
/// 1) EXE 同级 adb\adb.exe
/// 2) EXE 同级 adb.exe
/// 找不到时 IsAvailable=false，由界面提示放置位置。
/// 不修改系统 PATH、不写注册表。
/// </summary>
public static class ExeLocator
{
    /// <summary>解析出的 adb.exe 绝对路径；未找到为 null</summary>
    public static string? AdbPath { get; private set; }

    /// <summary>最近一次检测描述（用于界面提示）</summary>
    public static string LastProbe { get; private set; } = "尚未检测";

    private static bool _probed;

    public static bool IsAvailable
    {
        get
        {
            EnsureProbed();
            return AdbPath is not null;
        }
    }

    public static string AdbDir
    {
        get
        {
            var exeDir = AppContext.BaseDirectory;
            return Path.Combine(exeDir, "adb");
        }
    }

    /// <summary>重新探测 adb.exe（导入后可调用）</summary>
    public static bool Probe()
    {
        _probed = false;
        EnsureProbed();
        return AdbPath is not null;
    }

    private static void EnsureProbed()
    {
        if (_probed) return;
        _probed = true;

        AdbPath = null;
        var exeDir = AppContext.BaseDirectory;

        var candidates = new[]
        {
            Path.Combine(exeDir, "adb", "adb.exe"),
            Path.Combine(exeDir, "adb.exe"),
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                AdbPath = c;
                LastProbe = c;
                return;
            }
        }

        LastProbe = $"未找到 adb.exe（已检查：{string.Join("；", candidates)}）";
    }
}
