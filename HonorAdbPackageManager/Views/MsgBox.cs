using System.Windows;

namespace HonorAdbPackageManager.Views;

/// <summary>
/// 统一弹窗帮助类：自动附加 Owner（当前活动窗口或主窗口）。
/// 视觉统一走 AppMessageDialog（Aero 浅蓝、语义横幅、圆角说明块、右下按钮组），
/// 与 ConfirmDialog（确认停用系统组件）风格一致；不再使用 Win32 MessageBox。
/// </summary>
public static class MsgBox
{
    /// <summary>根据标题推断消息语义（决定横幅配色）</summary>
    private static MsgKind KindFromTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return MsgKind.Info;
        if (title.Contains("成功") || title.Contains("完成") || title.Contains("已还原")) return MsgKind.Success;
        if (title.Contains("失败") || title.Contains("错误") || title.Contains("拦截")) return MsgKind.Danger;
        if (title.Contains("确认") || title.Contains("警告") || title.Contains("注意")) return MsgKind.Warning;
        return MsgKind.Info;
    }

    public static MessageBoxResult Show(string text, string title = "",
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        var kind = KindFromTitle(title);
        if (icon == MessageBoxImage.Error) kind = MsgKind.Danger;
        else if (icon == MessageBoxImage.Warning) kind = MsgKind.Warning;
        else if (icon == MessageBoxImage.Question) kind = MsgKind.Warning;

        var dlg = AppMessageDialog.Create(text, title, buttons, kind);

        // 附加 Owner：优先当前活动窗口，其次主窗口
        var active = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.IsActive && !(w is AppMessageDialog));
        if (active is not null) dlg.Owner = active;
        else if (Application.Current.MainWindow is not null
                 && Application.Current.MainWindow is not AppMessageDialog)
            dlg.Owner = Application.Current.MainWindow;

        dlg.ShowDialog();

        return buttons switch
        {
            MessageBoxButton.YesNo => dlg.Confirmed ? MessageBoxResult.Yes : MessageBoxResult.No,
            MessageBoxButton.OKCancel => dlg.Confirmed ? MessageBoxResult.OK : MessageBoxResult.Cancel,
            MessageBoxButton.YesNoCancel => dlg.Result,
            _ => MessageBoxResult.OK,
        };
    }
}