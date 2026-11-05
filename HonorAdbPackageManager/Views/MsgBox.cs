using System.Windows;

namespace HonorAdbPackageManager.Views;

/// <summary>
/// 统一弹窗帮助类：自动附加 Owner（当前活动窗口或主窗口），
/// 使 MessageBox 与子窗口始终居中于父窗口，而不是屏幕左上角。
/// </summary>
public static class MsgBox
{
    private static Window? OwnerWindow
    {
        get
        {
            var active = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.IsActive);
            if (active is not null) return active;
            return Application.Current.MainWindow;
        }
    }

    public static MessageBoxResult Show(string text, string title = "",
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        var owner = OwnerWindow;
        if (owner is not null)
            return MessageBox.Show(owner, text, title, buttons, icon, defaultResult);
        return MessageBox.Show(text, title, buttons, icon, defaultResult);
    }
}
