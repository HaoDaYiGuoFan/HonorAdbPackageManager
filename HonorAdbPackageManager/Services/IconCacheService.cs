using System.IO.Compression;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HonorAdbPackageManager.Services;

/// <summary>
/// 应用图标缓存：按需从设备 pull APK → 解压内嵌 PNG 图标 → 缓存本地。
/// 只在用户选中条目时提取（避免批量下载几百个 APK），同一 APK 只提取一次。
/// 提取失败回退首字母占位（不阻塞任何流程）。
/// </summary>
public class IconCacheService
{
    private readonly LogService _log;
    private readonly string _cacheDir;
    private readonly HashSet<string> _failed = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public IconCacheService(LogService log)
    {
        _log = log;
        _cacheDir = Path.Combine(AppContext.BaseDirectory, "Cache", "icons");
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>获取图标（后台线程调用）；失败返回 null</summary>
    public ImageSource? GetIcon(string serial, PackageItem item)
    {
        var apk = item.SourceApkPath;
        if (string.IsNullOrEmpty(apk) || !apk.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            return null;

        // cache key：设备路径哈希，避免路径特殊字符
        var key = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(apk)))[..24];
        var cacheFile = Path.Combine(_cacheDir, key + ".png");
        if (File.Exists(cacheFile))
            return LoadPng(cacheFile);

        lock (_lock)
        {
            if (File.Exists(cacheFile))
                return LoadPng(cacheFile);
            if (_failed.Contains(apk)) return null;

            var tmpApk = Path.Combine(_cacheDir, key + ".apk");
            try
            {
                // 1) pull APK 到缓存目录
                if (!File.Exists(tmpApk))
                {
                    var r = App.Adb.PullAsync(serial, apk, _cacheDir).GetAwaiter().GetResult();
                    if (!r.Ok)
                    {
                        _log.Error($"图标提取失败（pull）：{item.PackageName}");
                        _failed.Add(apk);
                        return null;
                    }
                    // adb pull 保存为 APK 原名 → 移动为统一临时名
                    var pulled = Directory.GetFiles(_cacheDir, "*.apk")
                        .Where(f => !f.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) || true)
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .FirstOrDefault(f => Path.GetFileName(f) != Path.GetFileName(tmpApk) || !File.Exists(tmpApk));
                    if (pulled is null)
                    {
                        _failed.Add(apk);
                        return null;
                    }
                    if (!string.Equals(pulled, tmpApk, StringComparison.OrdinalIgnoreCase))
                        File.Move(pulled, tmpApk, true);
                }

                // 2) 解压提取图标 PNG
                using var archive = ZipFile.OpenRead(tmpApk);
                byte[]? best = null;

                foreach (var entry in archive.Entries)
                {
                    var fn = entry.FullName;
                    if (!fn.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                    var lower = fn.ToLowerInvariant();

                    // 只要启动图标：ic_launcher / icon，排除圆角/前景/背景/单色层
                    if (!(lower.Contains("ic_launcher") || lower.Contains("icon"))) continue;
                    if (lower.Contains("round") || lower.Contains("foreground") ||
                        lower.Contains("background") || lower.Contains("monochrome")) continue;

                    using var es = entry.Open();
                    using var buf = new MemoryStream();
                    es.CopyTo(buf);
                    var data = buf.ToArray();
                    if (data.Length > 0 && (best is null || data.Length > best.Length))
                        best = data;
                }

                if (best is not null)
                {
                    File.WriteAllBytes(cacheFile, best);
                    return LoadPng(cacheFile);
                }

                _failed.Add(apk);
                return null;
            }
            catch (Exception ex)
            {
                _log.Error($"图标提取失败 {item.PackageName}: {ex.Message}");
                _failed.Add(apk);
                return null;
            }
            finally
            {
                try { if (File.Exists(tmpApk)) File.Delete(tmpApk); } catch { }
            }
        }
    }

    private ImageSource? LoadPng(string file)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(file, UriKind.Absolute);
            bmp.DecodePixelWidth = 96;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}
