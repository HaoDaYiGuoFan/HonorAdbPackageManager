using System.Windows;

namespace HonorAdbPackageManager;

/// <summary>应用入口：组合根，创建服务并注入 MainWindow</summary>
public partial class App : Application
{
    public static Services.LogService Log { get; private set; } = new();
    public static Services.AdbService Adb { get; private set; } = null!;
    public static Services.KnowledgeService Knowledge { get; private set; } = null!;
    public static Services.IconCacheService Icons { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 全局异常兜底
        DispatcherUnhandledException += (s, args) =>
        {
            Log.Error($"界面异常：{args.Exception.Message}");
            MessageBox.Show(
                $"发生未处理异常：\n{args.Exception.Message}",
                "HonorAdbPackageManager",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        // 程序目录布局初始化
        TryDir("adb");
        TryDir("Data");
        TryDir("Cache");
        TryDir("Backup");
        TryDir("Logs");

        Knowledge = new Services.KnowledgeService(Log);
        Knowledge.Load();

        Adb = new Services.AdbService(Log);
        Icons = new Services.IconCacheService(Log);

        if (!Services.ExeLocator.IsAvailable)
        {
            MessageBox.Show(
                "未找到 adb.exe。\n\n请将 adb.exe / AdbWinApi.dll / AdbWinUsbApi.dll 放入程序目录 adb\\ 文件夹后重新启动。",
                "缺少 ADB 组件",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        else
        {
            Log.Info($"ADB 已就绪：{Services.ExeLocator.AdbPath}");
        }

        var win = new Views.DevicePickerWindow();
        MainWindow = win;
        win.Show();
    }

    private static void TryDir(string name)
    {
        try { Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, name)); }
        catch { /* 只读目录等情况忽略 */ }
    }
}
