using System.Text.RegularExpressions;

namespace HonorAdbPackageManager.Services;

/// <summary>pm list packages 单条记录</summary>
public class PackageEntry
{
    public string PackageName { get; set; } = string.Empty;
    public string ApkPath { get; set; } = string.Empty;
}

/// <summary>dumpsys package 解析结果</summary>
public class DumpInfo
{
    public string VersionName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool SystemApp { get; set; }
    public string ApkPath { get; set; } = string.Empty;
}

/// <summary>
/// dumpsys package 文本解析：只做行级正则，不启动任何额外进程。
///荣耀 MagicOS 与 AOSP 输出结构一致。
/// </summary>
public static class DumpParser
{
    private static readonly Regex VerName = new(
        @"versionName=(?<v>[^\s]+)", RegexOptions.Compiled);

    private static readonly Regex EnabledRe = new(
        @"^\s*enabled=(?<v>\d+)", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex CodePath = new(
        @"^\s*codePath=(?<p>.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ApkName = new(
        @"baseApk=(?<p>\S+)", RegexOptions.Compiled);

    /// <summary>解析 dumpsys 输出；空输出返回 null</summary>
    public static DumpInfo? Parse(string dump)
    {
        if (string.IsNullOrWhiteSpace(dump)) return null;

        var info = new DumpInfo();

        var m = VerName.Match(dump);
        if (m.Success) info.VersionName = m.Groups["v"].Value.Trim();

        var e = EnabledRe.Match(dump);
        if (e.Success && int.TryParse(e.Groups["v"].Value, out var en))
            info.Enabled = en != 0;

        var cp = CodePath.Match(dump);
        if (cp.Success)
        {
            var path = cp.Groups["p"].Value.Trim();
            info.SystemApp =
                path.StartsWith("/system", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/vendor", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/product", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/odm", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/system_ext", StringComparison.OrdinalIgnoreCase);
            info.ApkPath = path;
        }

        var an = ApkName.Match(dump);
        if (an.Success)
            info.ApkPath = an.Groups["p"].Value.Trim();

        return info;
    }
}
