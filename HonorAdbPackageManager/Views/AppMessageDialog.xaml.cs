using System.Windows;
using System.Windows.Media;

namespace HonorAdbPackageManager.Views;

/// <summary>
/// 统一应用内消息弹窗（替代 Win32 MessageBox 风格）：
/// 视觉与 ConfirmDialog（确认停用系统组件）保持一致 —— Aero 浅蓝底、
/// 语义色横幅条（危险红 / 警告橙 / 成功绿 / 信息蓝）、圆角说明块、右下按钮组。
/// 支持 OK / YesNo（是=主色，否=Ghost）。
/// </summary>
public partial class AppMessageDialog : Window
{
    /// <summary>点击「是/确定」返回 true；「否/取消」或关闭窗口返回 false</summary>
    public bool Confirmed => DialogResult == true;

    private AppMessageDialog()
    {
        InitializeComponent();
        EnsureOwner();
    }

    /// <summary>构建并显示一个消息弹窗（同步阻塞，等价 MsgBox.Show 语义）</summary>
    public static bool Show(string text, string title = "",
        MessageBoxButton buttons = MessageBoxButton.OK,
        MsgKind kind = MsgKind.Info,
        MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        var dlg = Create(text, title, buttons, kind);
        var active = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (active is not null && active != dlg) dlg.Owner = active;
        else if (Application.Current.MainWindow is not null && Application.Current.MainWindow != dlg)
            dlg.Owner = Application.Current.MainWindow;

        dlg.ShowDialog();
        return dlg.DialogResult == true;
    }

    internal static AppMessageDialog Create(string text, string title,
        MessageBoxButton buttons, MsgKind kind,
        MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        var effectiveTitle = string.IsNullOrWhiteSpace(title) ? KindDefaultTitle(kind) : title;
        var dlg = new AppMessageDialog
        {
            Title = effectiveTitle,
            BannerText = { Text = effectiveTitle },
            BodyText = { Text = text },
        };
        dlg.ApplyKind(kind);
        dlg.ApplyButtons(buttons, defaultResult);
        return dlg;
    }

    private static string KindDefaultTitle(MsgKind kind) => kind switch
    {
        MsgKind.Success => "成功",
        MsgKind.Warning => "提示",
        MsgKind.Danger => "操作失败",
        _ => "提示",
    };

    private void ApplyKind(MsgKind kind)
    {
        string glyph = kind switch
        {
            MsgKind.Success => "✓",
            MsgKind.Warning => "!",
            MsgKind.Danger => "✕",
            _ => "i",
        };
        Color solid = kind switch
        {
            MsgKind.Success => ((SolidColorBrush)FindResource("SafeBrush")).Color,
            MsgKind.Warning => ((SolidColorBrush)FindResource("WarnBrush")).Color,
            MsgKind.Danger => ((SolidColorBrush)FindResource("DangerBrush")).Color,
            _ => ((SolidColorBrush)FindResource("AccentBrush")).Color,
        };
        Color light = kind switch
        {
            MsgKind.Success => ((SolidColorBrush)FindResource("SafeLightBrush")).Color,
            MsgKind.Warning => ((SolidColorBrush)FindResource("WarnLightBrush")).Color,
            MsgKind.Danger => ((SolidColorBrush)FindResource("DangerLightBrush")).Color,
            _ => ((SolidColorBrush)FindResource("InfoLightBrush")).Color,
        };

        Banner.Background = new SolidColorBrush(light);
        BannerText.Foreground = new SolidColorBrush(solid);
        IconCircle.Background = new SolidColorBrush(solid);

        if (kind == MsgKind.Success)
        {
            IconGlyph.Visibility = Visibility.Collapsed;
            CheckIcon.Visibility = Visibility.Visible;
        }
        else
        {
            IconGlyph.Text = glyph;
        }

        // 右侧确认按钮跟随语义色（Danger 用 DangerButton，其余用 PrimaryButton）
        RightButton.Style = (Style)FindResource(
            kind == MsgKind.Danger ? "DangerButton" : "PrimaryButton");
    }

    private void ApplyButtons(MessageBoxButton buttons, MessageBoxResult defaultResult)
    {
        switch (buttons)
        {
            case MessageBoxButton.OK:
                LeftButton.Visibility = Visibility.Collapsed;
                MidButton.Visibility = Visibility.Collapsed;
                RightButton.Content = "确定";
                RightButton.IsDefault = true;
                break;

            case MessageBoxButton.YesNo:
            case MessageBoxButton.OKCancel:
                LeftButton.Content = buttons == MessageBoxButton.YesNo ? "否" : "取消";
                LeftButton.IsCancel = true;
                MidButton.Visibility = Visibility.Collapsed;
                RightButton.Content = buttons == MessageBoxButton.YesNo ? "是" : "确定";
                RightButton.IsDefault = defaultResult != MessageBoxResult.No && defaultResult != MessageBoxResult.Cancel;
                break;

            case MessageBoxButton.YesNoCancel:
                LeftButton.Content = "是";
                LeftButton.IsCancel = false;
                MidButton.Content = "否";
                MidButton.Visibility = Visibility.Visible;
                RightButton.Content = "取消";
                RightButton.IsDefault = defaultResult == MessageBoxResult.Cancel;
                break;
        }
    }

    // YesNoCancel: 是=Left, 否=Mid, 取消=Right
    private MessageBoxResult _yesNoCancelResult = MessageBoxResult.None;

    private void Left_Click(object sender, RoutedEventArgs e)
    {
        // OK 隐藏 Left；YesNoCancel 时 Left=是
        if (MidButton.Visibility == Visibility.Visible)
        {
            _yesNoCancelResult = MessageBoxResult.Yes;
            DialogResult = true;
            return;
        }
        DialogResult = false; // YesNo 的「否」/ OKCancel 的「取消」
    }

    private void Mid_Click(object sender, RoutedEventArgs e)
    {
        _yesNoCancelResult = MessageBoxResult.No;
        DialogResult = false;
    }

    private void Right_Click(object sender, RoutedEventArgs e)
    {
        if (MidButton.Visibility == Visibility.Visible)
        {
            _yesNoCancelResult = MessageBoxResult.Cancel;
            DialogResult = false;
            return;
        }
        DialogResult = true; // OK / YesNo 的「是」/ OKCancel 的「确定」
    }

    /// <summary>YesNoCancel 场景的具体结果；其余场景用 Confirmed 判断</summary>
    public MessageBoxResult Result => _yesNoCancelResult;

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);
        if (DialogResult is null) DialogResult = false;
    }

    /// <summary>无 Owner 时自动附加当前活动窗口，保证 CenterOwner 居中生效</summary>
    private void EnsureOwner()
    {
        if (Owner is null)
        {
            Owner = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.IsActive && w != this)
                ?? Application.Current.MainWindow;
            if (Owner == this) Owner = null;
        }
    }
}

/// <summary>消息语义类型：决定横幅与图标配色</summary>
public enum MsgKind
{
    /// <summary>信息（蓝）</summary>
    Info,
    /// <summary>成功（绿）</summary>
    Success,
    /// <summary>警告（橙）</summary>
    Warning,
    /// <summary>危险/错误（红）</summary>
    Danger,
}
