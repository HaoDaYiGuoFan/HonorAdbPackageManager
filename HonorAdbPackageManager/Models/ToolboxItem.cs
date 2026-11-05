using System.Windows;
using System.Windows.Media;

namespace HonorAdbPackageManager.Models;

/// <summary>快速工具箱单条目：绑定到已加载的 PackageItem，状态随操作联动</summary>
public class ToolboxItem : INotifyPropertyChanged
{
    private string _statusText = "未加载";
    private Visibility _actionVis = Visibility.Collapsed;
    private Visibility _restoreVis = Visibility.Collapsed;
    private Visibility _forbiddenVis = Visibility.Collapsed;
    private Visibility _notInstalledVis = Visibility.Visible;
    private string _actionText = "停用";

    /// <summary>显示名（如：荣耀钱包）</summary>
    public string Name { get; }

    /// <summary>主包名</summary>
    public string PackageName { get; }

    /// <summary>备选包名（不同固件版本可能用不同包名，按序探测）</summary>
    public string[] Alternates { get; }

    /// <summary>是否属于「一键停用冗余服务」安全清单</summary>
    public bool IsSafePreset { get; }

    /// <summary>绑定的包实例（来自主列表；null = 未安装/未加载）</summary>
    public PackageItem? Bound { get; private set; }

    public ToolboxItem(string name, string packageName, bool isSafePreset = false, params string[] alternates)
    {
        Name = name;
        PackageName = packageName;
        IsSafePreset = isSafePreset;
        Alternates = alternates;
    }

    public string StatusText
    {
        get => _statusText;
        private set { if (_statusText != value) { _statusText = value; Raise(nameof(StatusText)); } }
    }

    /// <summary>主操作按钮（停用/卸载）可见性</summary>
    public Visibility ActionVis
    {
        get => _actionVis;
        private set { if (_actionVis != value) { _actionVis = value; Raise(nameof(ActionVis)); } }
    }

    /// <summary>还原按钮可见性</summary>
    public Visibility RestoreVis
    {
        get => _restoreVis;
        private set { if (_restoreVis != value) { _restoreVis = value; Raise(nameof(RestoreVis)); } }
    }

    /// <summary>禁止操作提示可见性</summary>
    public Visibility ForbiddenVis
    {
        get => _forbiddenVis;
        private set { if (_forbiddenVis != value) { _forbiddenVis = value; Raise(nameof(ForbiddenVis)); } }
    }

    /// <summary>未安装/未加载提示可见性</summary>
    public Visibility NotInstalledVis
    {
        get => _notInstalledVis;
        private set { if (_notInstalledVis != value) { _notInstalledVis = value; Raise(nameof(NotInstalledVis)); } }
    }

    public string ActionText
    {
        get => _actionText;
        private set { if (_actionText != value) { _actionText = value; Raise(nameof(ActionText)); } }
    }

    /// <summary>绑定已加载的包实例并刷新状态</summary>
    public void Bind(PackageItem item)
    {
        Bound = item;
        UpdateFromBound();
    }

    /// <summary>标记为未安装</summary>
    public void MarkMissing()
    {
        Bound = null;
        StatusText = "未安装";
        SetStates(action: false, restore: false, forbidden: false, missing: true);
    }

    /// <summary>标记为未加载（包列表尚未拉取）</summary>
    public void MarkNotLoaded()
    {
        Bound = null;
        StatusText = "未加载";
        SetStates(action: false, restore: false, forbidden: false, missing: true);
    }

    /// <summary>从绑定的包实例同步状态（操作后调用）</summary>
    public void UpdateFromBound()
    {
        if (Bound is null)
        {
            MarkMissing();
            return;
        }

        if (!Bound.CanUninstall)
        {
            StatusText = "禁止操作";
            SetStates(action: false, restore: false, forbidden: true, missing: false);
            return;
        }

        StatusText = Bound.StatusDisplay;
        if (Bound.Status == PackageStatus.Enabled)
        {
            ActionText = Bound.PackageType == PackageType.ThirdParty ? "卸载" : "停用";
            SetStates(action: true, restore: false, forbidden: false, missing: false);
        }
        else if (Bound.Status == PackageStatus.Disabled)
        {
            SetStates(action: false, restore: true, forbidden: false, missing: false);
        }
        else // Uninstalled
        {
            StatusText = "已卸载";
            SetStates(action: false, restore: false, forbidden: false, missing: false);
        }
    }

    private void SetStates(bool action, bool restore, bool forbidden, bool missing)
    {
        ActionVis = action ? Visibility.Visible : Visibility.Collapsed;
        RestoreVis = restore ? Visibility.Visible : Visibility.Collapsed;
        ForbiddenVis = forbidden ? Visibility.Visible : Visibility.Collapsed;
        NotInstalledVis = missing ? Visibility.Visible : Visibility.Collapsed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>工具箱分类卡片（彩色标题 + 条目列表）</summary>
public class ToolboxCategory
{
    public string Name { get; }

    /// <summary>卡片标题底色（仿鸿蒙工具箱配色）</summary>
    public Brush HeaderBrush { get; }

    public ObservableCollection<ToolboxItem> Items { get; } = new();

    public ToolboxCategory(string name, string headerHex)
    {
        Name = name;
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(headerHex));
        brush.Freeze();
        HeaderBrush = brush;
    }
}
