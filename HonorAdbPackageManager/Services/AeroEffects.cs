using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace HonorAdbPackageManager.Services;

/// <summary>
/// 窗口级 Aero 浅蓝玻璃外观统一处理。
///
/// 关键点：窗口使用「不透明」的浅蓝渐变作为底色，玻璃质感由 WPF 自绘
/// （高光 / 渐变 / 描边 / 柔和光斑）。这样既不会出现 DWM 材质未绘制时的黑底，
/// 也不会因为窗口透明而丢失 ClearType 导致字体发虚、半透明画刷发灰。
/// DWM 只负责窗口圆角与浅蓝标题栏。
/// </summary>
public static class AeroEffects
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
    private static extern int SetInt(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
    private static extern int SetUInt(IntPtr hwnd, int attribute, ref uint value, int size);

    private static bool _registered;

    /// <summary>在 App.OnStartup 中调用一次，作用于所有窗口（含派生窗口与后续对话框）</summary>
    public static void Register()
    {
        if (_registered) return;
        _registered = true;
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded));
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Window win) return;

        // 不透明浅蓝玻璃底：保证 ClearType 生效、文字锐利、背景明亮不发灰
        win.Background = win.TryFindResource("AeroWindowBrush") as Brush ?? Brushes.White;

        // 屏幕优化的文本度量 + ClearType + 像素对齐，解决小字发虚
        TextOptions.SetTextFormattingMode(win, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(win, TextRenderingMode.ClearType);
        win.SnapsToDevicePixels = true;

        var hwnd = new WindowInteropHelper(win).Handle;
        if (hwnd != IntPtr.Zero) ApplyAeroChrome(hwnd);
    }

    /// <summary>亮色模式、Win11 圆角、浅蓝标题栏 + 深蓝灰标题文字</summary>
    private static void ApplyAeroChrome(IntPtr hwnd)
    {
        try
        {
            int light = 0;
            SetInt(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref light, sizeof(int));

            int round = DWMWCP_ROUND;
            SetInt(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));

            // 窗口细边框也统一为浅蓝
            uint border = 0x00FFEBDC;   // #DCEBFF
            SetUInt(hwnd, DWMWA_BORDER_COLOR, ref border, sizeof(uint));

            // COLORREF = 0x00BBGGRR。仅当标题栏颜色确实生效时才改文字颜色。
            uint caption = 0x00FFE4CD;   // #CDE4FF 浅蓝
            if (SetUInt(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(uint)) == 0)
            {
                uint titleText = 0x00452B1B;   // #1B2B45 深蓝灰
                SetUInt(hwnd, DWMWA_TEXT_COLOR, ref titleText, sizeof(uint));
            }
        }
        catch { /* 不支持的系统直接忽略 */ }
    }
}
