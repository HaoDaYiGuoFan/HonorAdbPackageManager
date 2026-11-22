using System.Text.RegularExpressions;

namespace HonorAdbPackageManager.Services;

/// <summary>
/// 应用详情查询服务：权限（含保护级别）/ 存储空间 / 流量使用 / 启动控制。
/// 数据源（真机 Magic5 Pro MagicOS 11 实测）：
///   dumpsys package      → requested/install/runtime permissions + appId/User 段
///   pm list permissions -f → 权限 → protectionLevel 映射（设备级缓存）
///   dumpsys diskstats    → Package Names / App Sizes / App Data Sizes / Cache Sizes 平行数组
///   dumpsys netstats     → mAppUidStatsMap（uid rxBytes rxPackets txBytes txPackets，开机以来累计）
///   appops get pkg OP    → RUN_IN_BACKGROUND / RUN_ANY_IN_BACKGROUND 后台运行模式
///   dumpsys deviceidle whitelist → 电池优化白名单
/// MagicOS 应用启动管理（自启动/关联启动）为系统私有存储，ADB 不可读。
/// </summary>
public static class PackageDetailService
{
    /// <summary>protectionLevel 映射缓存（设备串号 → 权限名 → (级别, 中文label)）</summary>
    private static readonly Dictionary<string, Dictionary<string, (string Level, string Label)>> _levelCache = new();

    /// <summary>加载详情（对话框一次性调用，约 4~7 条 adb 命令）</summary>
    public static async Task<PackageDetail> LoadAsync(string serial, PackageItem item)
    {
        var adb = App.Adb;

        var dump = await adb.ShellAsync(serial, $"dumpsys package {item.PackageName}", 20000);
        var pkg = ParsePackageDump(dump.StdOut);

        var levels = await GetPermissionLevelsAsync(serial, dump.StdOut);

        var disk = await adb.ShellAsync(serial, "dumpsys diskstats", 30000);
        var sizes = ParseDiskStats(disk.StdOut, item.PackageName);

        var net = await adb.ShellAsync(serial, "dumpsys netstats", 30000);
        var (rx, tx, trafficOk) = ParseNetStats(net.StdOut, pkg.AppId, pkg.Users);

        var opBg = await adb.ShellAsync(serial, $"appops get {item.PackageName} RUN_IN_BACKGROUND");
        var opBgAny = await adb.ShellAsync(serial, $"appops get {item.PackageName} RUN_ANY_IN_BACKGROUND");
        var idle = await adb.ShellAsync(serial, "dumpsys deviceidle whitelist", 15000);

        var groups = BuildPermissionGroups(pkg, levels);

        var bootPerm = pkg.Requested.FirstOrDefault(p => p == "android.permission.RECEIVE_BOOT_COMPLETED");
        var bootGranted = bootPerm is not null && (pkg.Install.TryGetValue(bootPerm, out var g1) && g1
            || pkg.Runtime.TryGetValue(bootPerm, out var g2) && g2);

        var dozeWhitelisted = idle.StdOut.Split('\n')
            .Any(l => l.Trim().EndsWith(item.PackageName, StringComparison.OrdinalIgnoreCase));

        return new PackageDetail
        {
            AppLabel = item.AppLabel,
            PackageName = item.PackageName,
            VersionName = item.VersionName,
            StatusDisplay = item.StatusDisplay,
            Groups = groups,
            StorageAvailable = sizes is not null,
            AppSizeText = sizes is { } s ? FormatSize(s.App) : "—",
            DataSizeText = sizes is { } s2 ? FormatSize(s2.Data) : "—",
            CacheSizeText = sizes is { } s3 ? FormatSize(s3.Cache) : "—",
            TotalSizeText = sizes is { } s4 ? FormatSize(s4.App + s4.Data + s4.Cache) : "—",
            TrafficAvailable = trafficOk,
            RxText = trafficOk ? FormatSize(rx) : "—",
            TxText = trafficOk ? FormatSize(tx) : "—",
            TotalTrafficText = trafficOk ? FormatSize(rx + tx) : "—",
            StartupControls = BuildStartupControls(item, bootPerm is not null, bootGranted,
                opBg.StdOut, opBgAny.StdOut, dozeWhitelisted),
        };
    }

    // ────────── 权限 ──────────

    private sealed record PkgDump(List<string> Requested,
        Dictionary<string, bool> Install, Dictionary<string, bool> Runtime,
        int AppId, List<int> Users);

    /// <summary>解析 dumpsys package：requested/install/runtime 权限 + appId + 用户列表</summary>
    private static PkgDump ParsePackageDump(string text)
    {
        var requested = new List<string>();
        var install = new Dictionary<string, bool>(StringComparer.Ordinal);
        var runtime = new Dictionary<string, bool>(StringComparer.Ordinal);
        int appId = 0;
        var users = new List<int>();

        // 当前所处段：requested / install / runtime / other
        string? section = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');

            if (line.Contains("appId=") && line.TrimStart().StartsWith("appId="))
                int.TryParse(line.Split('=')[1].Trim(), out appId);

            var userMatch = Regex.Match(line, @"^\s*User (\d+):");
            if (userMatch.Success)
            {
                users.Add(int.Parse(userMatch.Groups[1].Value));
                section = null; // User 段内的 runtime permissions 属于各用户，仍会重复匹配到同名权限
                continue;
            }

            if (line.Trim() == "requested permissions:") { section = "requested"; continue; }
            if (line.Trim() == "install permissions:") { section = "install"; continue; }
            if (line.Trim() == "runtime permissions:") { section = "runtime"; continue; }

            var t = line.Trim();
            if (t.Length == 0) { if (section == "requested") section = null; continue; }

            // 段内有效行：请求段是纯权限名；install/runtime 是 "perm: granted=true[, flags=…]"
            if (section == "requested")
            {
                if (t.EndsWith(":") || t.StartsWith("User ")) { section = null; continue; }
                if (t.Contains(' ') || !t.Contains('.')) { section = null; continue; }
                requested.Add(t);
            }
            else if (section is "install" or "runtime")
            {
                var m = Regex.Match(t, @"^([\w.]+):\s*granted=(true|false)");
                if (!m.Success) { if (!t.StartsWith("android.") && !t.StartsWith("com.") && !t.Contains(".permission.")) section = null; continue; }
                var dict = section == "install" ? install : runtime;
                // 主用户（默认段）先解析；分身/隐私空间（User N）段的同名权限不覆盖
                var key = m.Groups[1].Value;
                if (!dict.ContainsKey(key)) dict[key] = m.Groups[2].Value == "true";
            }
        }

        if (!users.Contains(0)) users.Insert(0, 0);
        users = users.Distinct().ToList();   // dumpsys 会重复列出各 User 段
        return new PkgDump(requested, install, runtime, appId, users);
    }

    /// <summary>设备级权限 → (protectionLevel, 中文label) 映射（一次拉取，按设备缓存）。
    /// 用 -g -f：真机实测 -f 缺失部分系统权限（如 READ_CALENDAR），-g -f 完整且带中文名称</summary>
    private static async Task<Dictionary<string, (string Level, string Label)>> GetPermissionLevelsAsync(string serial, string packageDump)
    {
        if (_levelCache.TryGetValue(serial, out var cached)) return cached;

        var r = await App.Adb.ShellAsync(serial, "pm list permissions -g -f", 30000);
        var map = ParsePermissionLevels(r.StdOut);
        if (map.Count > 0) _levelCache[serial] = map;
        return map;
    }

    /// <summary>解析 pm list permissions -g -f：+ permission:xxx / label:yyy / protectionLevel:zzz</summary>
    private static Dictionary<string, (string Level, string Label)> ParsePermissionLevels(string text)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        string? current = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.StartsWith("+ permission:"))
            {
                current = line["+ permission:".Length..].Trim();
            }
            else if (current is not null && line.StartsWith("protectionLevel:"))
            {
                map[current] = line["protectionLevel:".Length..].Trim();
                current = null;
            }
            else if (current is not null && line.StartsWith("label:"))
            {
                var label = line["label:".Length..].Trim();
                if (label is not ("" or "null")) labels[current] = label;
            }
        }
        return map.ToDictionary(kv => kv.Key, kv => (kv.Value, labels.GetValueOrDefault(kv.Key, string.Empty)), StringComparer.Ordinal);
    }

    /// <summary>protectionLevel 串 → 分级枚举（多段如 signature|privileged 取第一关键段）</summary>
    private static PermLevel MapLevel(string? protection)
    {
        if (string.IsNullOrEmpty(protection)) return PermLevel.Unknown;
        var first = protection.Split('|')[0].Trim();
        return first switch
        {
            "dangerous" => PermLevel.Dangerous,
            "normal" => PermLevel.Normal,
            "signature" or "signatureOrSystem" => PermLevel.Signature,
            "development" => PermLevel.Development,
            _ => PermLevel.Unknown,   // privileged / knownSigner / verbatim 等归入签名/系统更合理 → 见下
        };
    }

    /// <summary>分组构建：危险 → 签名/系统 → 普通 → 开发 → 未知；组内按名称排序</summary>
    private static List<PermissionGroup> BuildPermissionGroups(
        PkgDump pkg, Dictionary<string, (string Level, string Label)> levels)
    {
        var items = new List<PermissionItem>();
        foreach (var name in pkg.Requested.Distinct(StringComparer.Ordinal))
        {
            var granted = pkg.Install.TryGetValue(name, out var g1) && g1
                || pkg.Runtime.TryGetValue(name, out var g2) && g2;

            var (levelRaw, label) = levels.TryGetValue(name, out var lv) ? lv : (null, string.Empty);
            var level = MapLevel(levelRaw);
            // privileged/knownSigner 等系统级权限归入签名/系统块
            if (level == PermLevel.Unknown && levelRaw is not null)
                level = PermLevel.Signature;

            items.Add(new PermissionItem { Name = name, Label = label, Granted = granted, Level = level });
        }

        var order = new[] { PermLevel.Dangerous, PermLevel.Signature, PermLevel.Normal, PermLevel.Development, PermLevel.Unknown };
        var groups = new List<PermissionGroup>();
        foreach (var l in order)
        {
            var list = items.Where(i => i.Level == l).OrderBy(i => i.Name, StringComparer.Ordinal).ToList();
            if (list.Count > 0) groups.Add(new PermissionGroup(l, list));   // 只创建非空分组
        }
        return groups;
    }

    // ────────── 存储 ──────────

    /// <summary>解析 dumpsys diskstats 平行数组（Package Names / App Sizes / App Data Sizes / Cache Sizes）</summary>
    private static (long App, long Data, long Cache)? ParseDiskStats(string text, string packageName)
    {
        var names = ParseStringArray(FindArrayLine(text, "Package Names:"));
        var app = ParseLongArray(FindArrayLine(text, "App Sizes:"));
        var data = ParseLongArray(FindArrayLine(text, "App Data Sizes:"));
        var cache = ParseLongArray(FindArrayLine(text, "Cache Sizes:"));
        var idx = names.FindIndex(n => n.Equals(packageName, StringComparison.OrdinalIgnoreCase));
        if (idx < 0 || app.Count <= idx) return null;
        return (app[idx], idx < data.Count ? data[idx] : 0, idx < cache.Count ? cache[idx] : 0);
    }

    private static string FindArrayLine(string text, string key)
        => text.Split('\n').FirstOrDefault(l => l.TrimStart().StartsWith(key)) ?? string.Empty;

    private static List<string> ParseStringArray(string line)
    {
        var s = line[(line.IndexOf('[') + 1)..].TrimEnd(']', '\r');
        return s.Split(',').Select(x => x.Trim().Trim('"')).ToList();
    }

    private static List<long> ParseLongArray(string line)
    {
        var s = line[(line.IndexOf('[') + 1)..].TrimEnd(']', '\r');
        return s.Split(',').Select(x => long.TryParse(x.Trim(), out var v) ? v : 0).ToList();
    }

    // ────────── 流量 ──────────

    /// <summary>解析 dumpsys netstats 的 mAppUidStatsMap（BPF 统计，开机以来累计，含 WLAN 与移动数据）；
    /// 多用户/分身应用按 uid 候选（appId 与 user*100000+appId）合并</summary>
    private static (long Rx, long Tx, bool Ok) ParseNetStats(string text, int appId, List<int> users)
    {
        if (appId <= 0) return (0, 0, false);
        var candidates = new HashSet<int> { appId };
        foreach (var u in users) if (u > 0) candidates.Add(u * 100000 + appId);

        long rx = 0, tx = 0;
        bool inMap = false, hit = false;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            // 注意：前部有状态行 "mAppUidStatsMap: OK"，必须精确匹配数据表头
            if (line == "mAppUidStatsMap:") { inMap = true; continue; }
            if (!inMap) continue;
            if (line.StartsWith("mStatsMap") || line.Length == 0) break;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5 || !int.TryParse(parts[0], out var uid)) continue;
            if (!candidates.Contains(uid)) continue;

            hit = true;
            rx += long.Parse(parts[1]);
            tx += long.Parse(parts[3]);
        }
        return (rx, tx, hit);
    }

    // ────────── 启动控制 ──────────

    /// <summary>解析 appops get 输出为标准模式名（allow/ignore/deny/foreground/default…）</summary>
    private static string ParseAppOpMode(string output, string opName)
    {
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith(opName + ":"))
                return line[(opName.Length + 1)..].Split(';')[0].Trim();
        }
        if (output.Contains("Default mode:", StringComparison.OrdinalIgnoreCase))
        {
            var m = Regex.Match(output, @"Default mode:\s*(\w+)");
            if (m.Success) return $"default ({m.Groups[1].Value})";
            return "default";
        }
        return "unknown";
    }

    private static List<StartupControlItem> BuildStartupControls(
        PackageItem item, bool bootRequested, bool bootGranted,
        string opBgOut, string opBgAnyOut, bool dozeWhitelisted)
    {
        var list = new List<StartupControlItem>
        {
            // 自启动：Android 层可读的是"开机自启能力"（RECEIVE_BOOT_COMPLETED）
            new()
            {
                Title = "开机自启（BOOT_COMPLETED）",
                ValueText = !bootRequested ? "未申请开机广播"
                          : bootGranted ? "已具备自启能力" : "已申请但未授予",
                Tone = bootRequested && bootGranted ? "warn" : "muted",
                Note = "MagicOS「自启动」开关由系统私有存储管理，ADB 无法读取（设置→应用→应用启动管理 查看）",
            },
        };

        // 后台运行：appops RUN_IN_BACKGROUND / RUN_ANY_IN_BACKGROUND
        var modeBg = ParseAppOpMode(opBgOut, "RUN_IN_BACKGROUND");
        var modeBgAny = opBgAnyOut.Contains("Unknown operation", StringComparison.OrdinalIgnoreCase)
            ? null : ParseAppOpMode(opBgAnyOut, "RUN_ANY_IN_BACKGROUND");
        var (bgText, bgTone) = DescribeBgMode(modeBg, modeBgAny);
        list.Add(new StartupControlItem
        {
            Title = "后台运行（appops）",
            ValueText = bgText,
            Tone = bgTone,
            Note = "RUN_IN_BACKGROUND" + (modeBgAny is null ? "" : " / RUN_ANY_IN_BACKGROUND"),
        });

        // 电池优化白名单 = 后台不受 Doze 限制
        list.Add(new StartupControlItem
        {
            Title = "电池优化白名单",
            ValueText = dozeWhitelisted ? "在白名单内（后台不受 Doze 限制）" : "不在白名单",
            Tone = dozeWhitelisted ? "warn" : "muted",
            Note = "dumpsys deviceidle whitelist",
        });

        // 关联启动：MagicOS 私有
        list.Add(new StartupControlItem
        {
            Title = "关联启动（被其他应用拉起）",
            ValueText = "MagicOS 私有开关，ADB 不可读",
            Tone = "muted",
            Note = "请在手机 设置→应用→应用启动管理 中查看/调整",
        });

        return list;
    }

    private static (string Text, string Tone) DescribeBgMode(string mode, string? modeAny)
    {
        var m = mode.StartsWith("default") ? "allow" : mode;
        var tone = m switch
        {
            "allow" => "warn",      // 允许后台 = 更强后台能力，提示性橙
            "ignore" or "foreground" => "muted",
            "deny" => "danger",
            _ => "muted",
        };
        var text = m switch
        {
            "allow" => "允许（默认）",
            "ignore" => "受限（系统忽略后台启动）",
            "foreground" => "受限（仅前台可用）",
            "deny" => "禁止",
            _ => mode,
        };
        if (modeAny is not null)
        {
            var m2 = modeAny.StartsWith("default") ? "allow" : modeAny;
            if (m2 == "deny") { text += "；RUN_ANY_IN_BACKGROUND=禁止"; tone = "danger"; }
            else if (m2 == "ignore") text += "；RUN_ANY_IN_BACKGROUND=受限";
        }
        return (text, tone);
    }

    // ────────── 通用 ──────────

    public static string FormatSize(long bytes) => bytes switch
    {
        >= 1L << 30 => $"{bytes / (double)(1L << 30):0.##} GB",
        >= 1L << 20 => $"{bytes / (double)(1L << 20):0.##} MB",
        >= 1L << 10 => $"{bytes / (double)(1L << 10):0.##} KB",
        > 0 => $"{bytes} B",
        _ => "0 B",
    };
}
