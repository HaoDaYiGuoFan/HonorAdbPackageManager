using System.Diagnostics;

namespace HonorAdbPackageManager.Services;

/// <summary>ADB 命令执行结果</summary>
public class AdbResult
{
    public int ExitCode { get; init; }
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
    public bool Ok => ExitCode == 0;
    public string Combined => string.Join(Environment.NewLine,
        new[] { StdOut, StdErr }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>
/// ADB 命令执行服务。所有命令均走 ExeLocator 解析出的 adb.exe 绝对路径，
/// 统一超时、编码与日志；不依赖系统 PATH。
/// </summary>
public class AdbService
{
    private readonly LogService _log;

    public AdbService(LogService log) => _log = log;

    /// <summary>是否已找到可用的 adb.exe</summary>
    public bool IsReady => !string.IsNullOrEmpty(ExeLocator.AdbPath);

    /// <summary>当前 adb.exe 路径</summary>
    public string AdbPath => ExeLocator.AdbPath ?? string.Empty;

    /// <summary>针对指定设备的 shell 前缀（多设备时必带 -s）</summary>
    public static string DevicePrefix(string? serial) =>
        string.IsNullOrWhiteSpace(serial) ? string.Empty : $"-s {serial} ";

    // ---------------- 基础执行 ----------------

    /// <summary>执行 adb 命令（任意参数串），带超时与日志</summary>
    public async Task<AdbResult> RunAsync(string arguments, int timeoutMs = 15000, bool logCommand = true)
    {
        if (!IsReady)
        {
            _log.Error("ADB 不可用：未找到 adb.exe");
            return new AdbResult { ExitCode = -1, StdErr = "adb.exe 不存在" };
        }

        if (logCommand)
            _log.Command($"adb {arguments}");

        var psi = new ProcessStartInfo
        {
            FileName = AdbPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var process = new Process { StartInfo = psi };

        // 必须先 Start 再访问 StandardOutput/Error（流在 Start 时才创建）
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            _log.Error($"ADB 启动失败：{ex.Message}");
            return new AdbResult { ExitCode = -1, StdErr = ex.Message };
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { /* 尽力终止 */ }
            _log.Error($"ADB 命令超时({timeoutMs}ms)：adb {arguments}");
            return new AdbResult { ExitCode = -2, StdErr = $"命令超时（{timeoutMs}ms）" };
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (!string.IsNullOrWhiteSpace(stdout)) _log.Output(stdout.TrimEnd());
        if (!string.IsNullOrWhiteSpace(stderr)) _log.Output(stderr.TrimEnd());

        return new AdbResult { ExitCode = process.ExitCode, StdOut = stdout, StdErr = stderr };
    }

    /// <summary>在指定设备上执行 shell 命令</summary>
    public Task<AdbResult> ShellAsync(string serial, string shellCommand, int timeoutMs = 15000) =>
        RunAsync($"{DevicePrefix(serial)}shell {shellCommand}", timeoutMs);

    // ---------------- 设备管理 ----------------

    /// <summary>解析 adb devices 输出（跳过标题行与守护进程提示）</summary>
    public async Task<List<DeviceInfo>> ListDevicesAsync()
    {
        var result = await RunAsync("devices -l", 10000, logCommand: false);
        var devices = new List<DeviceInfo>();

        foreach (var raw in result.StdOut.Split('\n'))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("* ", StringComparison.Ordinal)) continue;          // daemon 提示
            if (line.StartsWith("adb.exe:", StringComparison.OrdinalIgnoreCase)) continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            if (parts[0].Equals("adb", StringComparison.OrdinalIgnoreCase)) continue;

            devices.Add(new DeviceInfo { Serial = parts[0], State = parts[1] });
        }
        return devices;
    }

    /// <summary>读取设备属性并填充 MagicOS 版本</summary>
    public async Task<DeviceInfo> GetDeviceInfoAsync(string serial)
    {
        var info = new DeviceInfo { Serial = serial, State = "device" };

        var props = await RunAsync($"{DevicePrefix(serial)}shell getprop", 15000);
        foreach (var raw in props.StdOut.Split('\n'))
        {
            var line = raw.Trim();
            var idx = line.IndexOf(": [", StringComparison.Ordinal);
            if (idx <= 0) continue;

            var key = line[..idx].Trim();
            var val = line[(idx + 3)..].TrimEnd(']').TrimEnd('[').Trim();

            switch (key)
            {
                case "ro.product.model": info.Model = val; break;
                case "ro.product.brand": info.Brand = val; break;
                case "ro.build.version.release": info.AndroidVersion = val; break;
                case "ro.build.version.sdk": info.SdkVersion = val; break;
                case "ro.build.version.magic":
                    info.MagicOsVersion = $"MagicOS {val}";
                    break;
                case "ro.build.version.emui":
                    if (info.MagicOsVersion == "未知") info.MagicOsVersion = val;
                    break;
                case "ro.config.hw_magicsize":
                    if (info.MagicOsVersion == "未知") info.MagicOsVersion = val;
                    break;
            }
        }
        return info;
    }

    /// <summary>无线连接 adb connect ip:port</summary>
    public Task<AdbResult> ConnectAsync(string hostPort) =>
        RunAsync($"connect {hostPort}", 15000);

    /// <summary>断开无线连接</summary>
    public Task<AdbResult> DisconnectAsync(string? hostPort = null) =>
        string.IsNullOrWhiteSpace(hostPort) ? RunAsync("disconnect", 10000) : RunAsync($"disconnect {hostPort}", 10000);

    // ---------------- 包操作 ----------------

    /// <summary>pm list packages；system=true 系统包(-s)，third=true 第三方包(-3)</summary>
    public async Task<List<PackageEntry>> ListPackagesAsync(string serial, bool system, bool third)
    {
        var flags = new List<string>();
        if (system) flags.Add("-s");
        if (third) flags.Add("-3");
        if (flags.Count == 0) flags.Add("-f");

        var result = await ShellAsync(serial, $"pm list packages {string.Join(' ', flags)}", 30000);
        var list = new List<PackageEntry>();

        foreach (var raw in result.StdOut.Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith("package:", StringComparison.Ordinal)) continue;

            // -f 输出: package:/data/app/xx/base.apk=com.foo  或  package:com.foo
            var body = line["package:".Length..];
            string apkPath = string.Empty;
            string name = body;

            var eq = body.IndexOf('=');
            if (eq > 0)
            {
                apkPath = body[..eq];
                name = body[(eq + 1)..];
            }

            if (!string.IsNullOrEmpty(name))
                list.Add(new PackageEntry { PackageName = name, ApkPath = apkPath });
        }
        return list;
    }

    /// <summary>pm list packages -d（已停用包名集合）</summary>
    public async Task<HashSet<string>> ListDisabledPackagesAsync(string serial)
    {
        var result = await ShellAsync(serial, "pm list packages -d", 20000);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in result.StdOut.Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith("package:", StringComparison.Ordinal)) continue;
            set.Add(line["package:".Length..].Trim());
        }
        return set;
    }

    /// <summary>dumpsys package 单包详情原始文本</summary>
    public Task<AdbResult> DumpPackageAsync(string serial, string packageName) =>
        ShellAsync(serial, $"dumpsys package {packageName}", 20000);

    /// <summary>pm uninstall -k --user 0</summary>
    public Task<AdbResult> UninstallAsync(string serial, string packageName) =>
        ShellAsync(serial, $"pm uninstall -k --user 0 {packageName}", 30000);

    /// <summary>pm disable-user --user 0</summary>
    public Task<AdbResult> DisableAsync(string serial, string packageName) =>
        ShellAsync(serial, $"pm disable-user --user 0 {packageName}", 30000);

    /// <summary>pm enable</summary>
    public Task<AdbResult> EnableAsync(string serial, string packageName) =>
        ShellAsync(serial, $"pm enable {packageName}", 30000);

    /// <summary>adb pull（APK 备份）</summary>
    public Task<AdbResult> PullAsync(string serial, string devicePath, string localDir)
    {
        Directory.CreateDirectory(localDir);
        return RunAsync($"{DevicePrefix(serial)}pull \"{devicePath}\" \"{localDir}\"", 300000);
    }

    /// <summary>adb install -r</summary>
    public Task<AdbResult> InstallAsync(string serial, string localApk) =>
        RunAsync($"{DevicePrefix(serial)}install -r \"{localApk}\"", 600000);
}
