using System.Diagnostics;
using System.Text.RegularExpressions;

namespace HonorAdbPackageManager.Services;

/// <summary>
/// 手机性能监测数据源（荣耀 Magic5 Pro / MagicOS 11 真机实测格式）：
///   dumpsys battery             → 电量/充电状态/电池温度(0.1°C)
///   dumpsys telephony.registry  → 双卡 mSignalStrength（取 primary 段的制式/dbm/格数）
///   dumpsys connectivity        → Active default network + NetworkAgentInfo（Wi-Fi/移动数据 NR/LTE）
///   dumpsys netstats Xt stats   → 按小时流量桶（metered=true=移动数据 / type=1=Wi-Fi），可累计本月
///   dumpsys thermalservice      → 各 CPU 核心温度（取峰值）
///   dumpsys SurfaceFlinger      → --list 图层名 + --latency 帧时间戳（每秒帧率与帧间隔抖动）
///   pidof + /proc/pid/stat      → 游戏进程 CPU 占用（utime+stime 差分）
/// </summary>
public static class PerformanceMonitorService
{
    /// <summary>单条 shell 轮询命令的组合分隔标记</summary>
    private const string Sep = "===T===";

    // ────────── 状态面板快照 ──────────

    /// <summary>获取状态面板快照（电量/信号/网络 + 可选的月流量统计，约 2~4 条 adb 命令）</summary>
    public static async Task<StatusSnapshot> GetStatusSnapshotAsync(string serial, DateTime monthStartUtc, bool includeTraffic)
    {
        var adb = App.Adb;

        // 注意：不 grep（Honor 真机实测 dumpsys battery 的 level/status/temperature 行无缩进，
        // 带 ^  锚点的过滤会全部滤掉导致电量 0%/未知），直接取全量由解析器匹配
        var bat = await adb.ShellAsync(serial, "dumpsys battery");
        var sig = await adb.ShellAsync(serial, "dumpsys telephony.registry | grep -E 'mSignalStrength|mDataConnectionState='", 20000);
        var conn = await adb.ShellAsync(serial, "dumpsys connectivity | grep -E 'Active default network|NetworkAgentInfo\\{'", 20000);

        var (level, status, temp) = ParseBattery(bat.StdOut);

        bool trafficOk = false; long mrx = 0, mtx = 0, wrx = 0, wtx = 0;
        if (includeTraffic)
        {
            var net = await adb.ShellAsync(serial, "dumpsys netstats", 30000);
            trafficOk = TryParseMonthlyTraffic(net.StdOut, monthStartUtc, out mrx, out mtx, out wrx, out wtx);
        }

        return new StatusSnapshot
        {
            BatteryLevel = level,
            BatteryStatusText = ParseBatteryStatus(status),
            BatteryTemp = temp,
            SignalText = ParseSignal(sig.StdOut),
            NetworkText = ParseNetwork(conn.StdOut),
            TrafficOk = trafficOk,
            MobileRx = mrx, MobileTx = mtx, WifiRx = wrx, WifiTx = wtx,
        };
    }

    /// <summary>获取当前前台应用包名</summary>
    public static async Task<string?> GetForegroundPackageAsync(string serial)
    {
        var r = await App.Adb.ShellAsync(serial, "dumpsys activity activities | grep ResumedActivity", 20000);
        var m = Regex.Match(r.StdOut, @"u0 ([\w.]+)/");
        return m.Success ? m.Groups[1].Value : null;
    }

    // ────────── 电池 ──────────

    private static (int Level, int Status, double Temp) ParseBattery(string text)
    {
        int level = 0, status = 0; double temp = 0;
        foreach (var m in Regex.Matches(text, @"^\s*(level|status|temperature):\s*(\d+)", RegexOptions.Multiline).Cast<Match>())
        {
            switch (m.Groups[1].Value)
            {
                case "level": level = int.Parse(m.Groups[2].Value); break;
                case "status": status = int.Parse(m.Groups[2].Value); break;
                case "temperature": temp = int.Parse(m.Groups[2].Value) / 10.0; break;
            }
        }
        return (level, status, temp);
    }

    private static string ParseBatteryStatus(int status) => status switch
    {
        2 => "充电中",
        3 => "放电中",
        4 => "未在充电",
        5 => "已充满",
        _ => "未知",
    };

    // ────────── 信号 ──────────

    private static readonly string[] DbmFields = ["ssRsrp", "rsrp", "rssi", "rscp", "cdmaDbm"];
    private const long InvalidDbm = 2147483647;

    /// <summary>解析双卡信号：定位每张卡的 primary 制式，再取其对应的 mLte=/mNr=… 段解析有效 dBm 与格数。
    /// 注意 primary=CellSignalStrengthXxx 只是制式标记（位于块尾），数值在块的 mXxx= 子段中</summary>
    private static string ParseSignal(string text)
    {
        var parts = text.Split("mSignalStrength=");
        var cards = new List<string>();
        foreach (var block in parts.Skip(1))
        {
            var primary = Regex.Match(block, @"primary=CellSignalStrength(\w+)");
            if (!primary.Success) continue;
            var techRaw = primary.Groups[1].Value;
            var tech = techRaw switch
            {
                "Nr" => "NR(5G)",
                "Lte" => "LTE(4G)",
                "Gsm" => "GSM(2G)",
                "Wcdma" => "WCDMA(3G)",
                "Cdma" => "CDMA",
                var t => t,
            };

            // 截取 primary 制式对应的 mXxx= 子段（到下一个 ",m" 或块尾）
            var start = block.IndexOf($"m{techRaw}=", StringComparison.Ordinal);
            if (start < 0) continue;
            var end = block.IndexOf(",m", start + techRaw.Length + 2);
            var seg = end > start ? block[start..end] : block[start..];

            double? dbm = null;
            foreach (var f in DbmFields)
            {
                var m = Regex.Match(seg, $@"{f}\s*=\s*(-?\d+)");
                if (m.Success && long.TryParse(m.Groups[1].Value, out var v) && v != InvalidDbm)
                {
                    dbm = v;
                    break;
                }
            }
            var lvl = Regex.Match(seg, @"level\s*=\s*(\d)");
            var phoneId = Regex.Match(block, @"mPhoneId=(\d+)");

            var id = phoneId.Success ? int.Parse(phoneId.Groups[1].Value) + 1 : cards.Count + 1;
            cards.Add($"卡{id}: {tech}{(dbm is null ? "" : $" {dbm:0}dBm")}（{(lvl.Success && int.TryParse(lvl.Groups[1].Value, out var l) ? Math.Min(l, 5) : 0)}格）");
        }
        return cards.Count == 0 ? "无信号数据" : string.Join("\n", cards);
    }

    // ────────── 网络 ──────────

    /// <summary>解析活动网络与连接状态（多行显示）</summary>
    private static string ParseNetwork(string text)
    {
        var activeId = Regex.Match(text, @"Active default network:\s*(\d+)");
        var lines = new List<string>();

        if (activeId.Success)
        {
            var id = activeId.Groups[1].Value;
            var isWifiActive = Regex.IsMatch(text, $@"network\{{{id}\}}[^\n]*ni\{{WIFI");
            var rat = Regex.Match(text, @"ni\{MOBILE\[(\w+)\]").Groups[1].Value;
            lines.Add(isWifiActive ? "当前上网：Wi-Fi" : $"当前上网：移动数据 {(rat == "NR" ? "NR(5G)" : rat)}");
        }

        if (Regex.IsMatch(text, @"ni\{WIFI\b"))
            lines.Add(Regex.IsMatch(text, @"ni\{WIFI\b[^}]*CONNECTED") ? "Wi-Fi：已连接" : "Wi-Fi：未连接");

        var cell = Regex.Match(text, @"ni\{MOBILE\[(\w+)\][^}]*CONNECTED");
        lines.Add(cell.Success
            ? $"移动数据：已连接（{cell.Groups[1].Value}{(cell.Groups[1].Value == "NR" ? "(5G)" : "")}）"
            : "移动数据：未连接");

        return lines.Count == 0 ? "—（解析失败）" : string.Join("\n", lines);
    }

    // ────────── 本月流量（Xt stats 小时桶累计） ──────────

    /// <summary>解析 dumpsys netstats 的 Xt stats：本月（monthStartUtc 起）移动数据（metered=true）与 Wi-Fi（type=1）收发合计</summary>
    private static bool TryParseMonthlyTraffic(string text, DateTime monthStartUtc, out long mobileRx, out long mobileTx, out long wifiRx, out long wifiTx)
    {
        mobileRx = mobileTx = wifiRx = wifiTx = 0;
        var monthStart = new DateTimeOffset(monthStartUtc).ToUnixTimeSeconds();

        // idents: "  ident=[{type=1, ..., metered=false, wifiNetworkKey=...}]" 之后跟 NetworkStatsHistory 行 st=.. rb=.. tb=..
        bool isWifi = false, isMetered = false;
        bool matched = false;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Contains("ident=[{"))
            {
                isWifi = line.Contains("type=1") || line.Contains("wifiNetworkKey=");
                isMetered = line.Contains("metered=true");
                continue;
            }
            var m = Regex.Match(line, @"^\s*st=(\d+)\s+rb=(\d+)\s+rp=(\d+)\s+tb=(\d+)\s+tp=(\d+)");
            if (!m.Success) continue;
            if (!long.TryParse(m.Groups[1].Value, out var st) || st < monthStart) continue;
            matched = true;
            var rb = long.Parse(m.Groups[2].Value);
            var tb = long.Parse(m.Groups[4].Value);
            if (isWifi) { wifiRx += rb; wifiTx += tb; }
            else if (isMetered) { mobileRx += rb; mobileTx += tb; }
        }
        return matched;
    }

    // ────────── 游戏采样 ──────────

    /// <summary>游戏性能采样器：维护帧数/jiffies 基线做差分，每秒调用一次</summary>
    public sealed class GameSampler
    {
        private readonly string _serial;
        private readonly string _pkg;
        
        private long _lastJiffies = -1;
        private DateTime _lastTime = DateTime.MinValue;
        private string? _lastLayer;
        private List<string> _candidates = new();
        private long _nowNs;   // 设备单调时钟当前值（/proc/uptime，用于帧时间戳新鲜度判断）

        public GameSampler(string serial, string pkg)
        {
            _serial = serial;
            _pkg = pkg;
        }

        /// <summary>执行一次采样（约 4 条 adb 命令）；帧率取 SurfaceFlinger 图层帧时间戳</summary>
        public async Task<PerformanceSample> SampleAsync(int second)
        {
            var adb = App.Adb;
            var now = DateTime.Now;

            // 1) 电量+温度+系统运行时长（合并为一次 shell 往返；uptime 用于帧时间戳新鲜度对齐）
            var bt = await adb.ShellAsync(_serial,
                "dumpsys battery; echo ===T===; dumpsys thermalservice | grep 'mName=CPU'; echo ===U===; cat /proc/uptime");
            var segs = bt.StdOut.Split(Sep);
            var (level, _, batTemp) = ParseBattery(segs.Length > 0 ? segs[0] : "");
            var cpuTemp = ParseMaxCpuTemp(segs.Length > 1 ? segs[1] : "");
            if (segs.Length > 2 && double.TryParse(segs[2].Trim().Split(' ')[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var upSec))
                _nowNs = (long)(upSec * 1e9);

            // 2) 帧率：图层定位 + 帧时间戳
            var (fps, jank) = await SampleFpsAsync();

            // 3) 游戏进程 CPU 占用（jiffies 差分）
            var proc = await adb.ShellAsync(_serial,
                $"for p in $(pidof {_pkg}); do awk '{{print $14+$15}}' /proc/$p/stat 2>/dev/null; done");
            long jiffies = 0;
            foreach (var m in Regex.Matches(proc.StdOut, @"\d+").Cast<Match>())
                jiffies += long.Parse(m.Value);
            double cpuPct = 0;
            if (_lastJiffies >= 0 && _lastTime > DateTime.MinValue)
            {
                var dt = (now - _lastTime).TotalSeconds;
                if (dt > 0.2)
                    cpuPct = Math.Max(0, (jiffies - _lastJiffies) / 100.0 / dt * 100);
            }
            _lastJiffies = jiffies;
            _lastTime = now;

            return new PerformanceSample
            {
                Time = now,
                Second = second,
                Fps = fps,
                BatteryTemp = batTemp,
                CpuTemp = cpuTemp,
                CpuPercent = cpuPct,
                JankPercent = jank,
                BatteryLevel = level,
            };
        }

        /// <summary>定位目标应用图层并统计上一秒的帧率与卡顿帧占比。
        /// 真机踩坑记录：--list 每行形如 "RequestedLayerState{图层名 parentId=..}"，
        /// 必须剥离包裹与元数据得到真实图层名，否则 --latency 按名匹配失败恒返回表头（FPS=0）；
        /// 且 "Background for ... SurfaceView" 占位层排在渲染层之前，不排除会一直选中它</summary>
        private async Task<(int Fps, double JankPercent)> SampleFpsAsync()
        {
            var adb = App.Adb;

            if (_lastLayer is null)
            {
                var list = await adb.ShellAsync(_serial, "dumpsys SurfaceFlinger --list", 20000);
                var names = list.StdOut.Split('\n')
                    .Select(ExtractLayerName)
                    .Where(n => n is not null)
                    .Cast<string>()
                    .Where(n => !n.StartsWith("Background for")
                                && !n.StartsWith("Bounds for")
                                && !n.Contains("InputSink"))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                // 候选按优先级排序：SurfaceView[..](BLAST) 游戏渲染面 → VRI 视图根 → leash 层 → ActivityRecord 容器。
                // 注意：每个类别只取最优先的一个——真机实测抖音类应用有 3 个 SurfaceView 变体（多数无帧），
                // 若逐类全量入列再截断，VRI（帧数据所在层）会被挤出候选名单导致 FPS 恒 0
                var candidates = new List<string>();
                void AddBest(Func<string, bool> pred)
                {
                    var b = names.FirstOrDefault(pred);
                    if (b is not null) candidates.Add(b);
                }
                AddBest(l => l.Contains($"SurfaceView[{_pkg}") && l.Contains("(BLAST)"));   // 游戏渲染面
                AddBest(l => l.Contains($"VRI-{_pkg}"));                                     // 视图根（应用 UI 逐帧层）
                AddBest(l => l.Contains($"{_pkg}/") && !l.StartsWith("ActivityRecord{"));    // leash 层
                AddBest(l => l.StartsWith("ActivityRecord{") && l.Contains($" u0 {_pkg}/")); // 容器层
                AddBest(l => l.Contains($"SurfaceView[{_pkg}"));                             // 其余 SurfaceView
                candidates = candidates.Distinct(StringComparer.Ordinal).Take(6).ToList();
                if (candidates.Count == 0) return (0, 0);
                App.Log.Info($"[性能监测] {_pkg} 图层候选({candidates.Count})：{candidates[0]}");
                _candidates = candidates;
            }

            // 先试缓存层；取不到帧时在同一轮里依次尝试其余候选，命中即缓存
            var tryList = new List<string>();
            if (_lastLayer is not null) tryList.Add(_lastLayer);
            tryList.AddRange(_candidates.Where(c => c != _lastLayer));

            foreach (var cand in tryList)
            {
                var r = await adb.ShellAsync(_serial, $"dumpsys SurfaceFlinger --latency '{cand}'", 20000);
                var lines2 = r.StdOut.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
                if (lines2.Count < 2) continue;

                long refresh = 16666667;
                _ = long.TryParse(lines2[0], out refresh);
                if (refresh <= 0) refresh = 16666667;

                var presents = new List<long>();
                foreach (var t in lines2.Skip(1))
                {
                    // 行分隔符为 TAB 或空格（真机验证：\t 分隔三列 pending/present/posted）
                    var vals = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(v => long.TryParse(v, out var x) ? x : 0).ToArray();
                    // 取第 2/3 列中较晚的非零值作为呈现时刻
                    long present = 0;
                    if (vals.Length >= 3) present = Math.Max(vals[1], vals[2]);
                    else if (vals.Length == 2) present = vals[1];
                    if (present > 0) presents.Add(present);
                }
                if (presents.Count == 0) continue;

                _lastLayer = cand;
                presents.Sort();

                // 帧率 = (帧数-1) / 缓冲时间跨度。
                // 注意不能按"最近1秒窗口数帧"：真机实测 --latency 帧缓冲仅约 63 帧深度，
                // 120fps 时缓冲只覆盖 ~0.53 秒，按 1 秒窗口统计会把 120 帧低估成 ~63。
                // 另需新鲜度检测：应用停止渲染后缓冲冻结在旧值，跨度法会一直报旧帧率
                var newest = presents[^1];
                if (_nowNs > 0 && newest < _nowNs - 1_500_000_000)
                {
                    // 最新帧已是 1.5 秒前的旧数据 → 当前无渲染
                    _lastLayer = null;
                    return (0, 0);
                }
                var oldest = presents[0];
                var span = (newest - oldest) / 1_000_000_000.0;
                var fps = span > 0.001 ? (presents.Count - 1) / span : 0;

                double jankPct = 0;
                if (presents.Count >= 2)
                {
                    var intervals = new List<double>();
                    for (int i = 1; i < presents.Count; i++)
                        intervals.Add((presents[i] - presents[i - 1]) / 1_000_000.0);
                    var threshold = Math.Max(refresh / 1_000_000.0 * 1.5, 20);
                    jankPct = intervals.Count(iv => iv > threshold) * 100.0 / intervals.Count;
                }

                if (fps < 1)
                {
                    // 缓冲全是旧帧（游戏换了渲染图层/已停止渲染）：丢弃缓存下轮重选
                    _lastLayer = null;
                }
                return ((int)Math.Min(fps, 240), jankPct);
            }

            _lastLayer = null;   // 本轮全部候选都无帧，下一轮重新 --list
            return (0, 0);
        }

        /// <summary>剥离 --list 行的 RequestedLayerState{ } 包裹与 parentId/z 等元数据，得到可传给 --latency 的真实图层名</summary>
        private static string? ExtractLayerName(string line)
        {
            var t = line.Trim();
            if (t.Length == 0) return null;
            if (t.StartsWith("RequestedLayerState{"))
            {
                t = t["RequestedLayerState{".Length..];
                var close = t.LastIndexOf('}');
                if (close >= 0) t = t[..close];
                var pid = t.IndexOf(" parentId=", StringComparison.Ordinal);
                if (pid > 0) t = t[..pid];
            }
            t = t.Trim();
            return t.Length > 0 ? t : null;
        }
    }

    /// <summary>解析 thermalservice 输出，取所有 CPU 核心的峰值温度</summary>
    private static double ParseMaxCpuTemp(string text)
    {
        double max = 0;
        foreach (var m in Regex.Matches(text, @"mValue=([\d.]+),\s*mType=\d+,\s*mName=CPU").Cast<Match>())
            max = Math.Max(max, double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
        return max;
    }
}
