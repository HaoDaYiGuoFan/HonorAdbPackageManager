namespace HonorAdbPackageManager.Models;

/// <summary>权限保护级别（决定分块与颜色）</summary>
public enum PermLevel
{
    Dangerous,   // 危险（红）
    Normal,      // 普通（灰）
    Signature,   // 签名/系统（蓝）
    Development, // 开发（橙）
    Unknown      // 未知（浅灰）
}

/// <summary>单条权限显示项（Granted 可切换，支持 pm grant/revoke 与 appops 两种写入方式）</summary>
public class PermissionItem : INotifyPropertyChanged
{
    private bool _granted;

    public string Name { get; init; } = string.Empty;
    /// <summary>中文短名（内置中文权限库优先，其次设备 pm list permissions -g -f 的 label；无则空）</summary>
    public string Label { get; init; } = string.Empty;
    /// <summary>中文作用说明（内置中文权限库优先，其次设备 description；服务层兜底保证非空）</summary>
    public string Description { get; init; } = string.Empty;
    public PermLevel Level { get; init; }
    /// <summary>是否可通过 adb 切换（运行时权限 → pm；悬浮窗/修改设置 → appops）</summary>
    public bool CanToggle { get; init; }
    /// <summary>切换写入方式：pm / appop；不可切换为空</summary>
    public string ToggleMode { get; init; } = string.Empty;
    /// <summary>appop 方式对应的操作名（SYSTEM_ALERT_WINDOW / WRITE_SETTINGS）</summary>
    public string OpName { get; init; } = string.Empty;

    public bool Granted
    {
        get => _granted;
        set
        {
            if (_granted != value)
            {
                _granted = value;
                Raise();
                Raise(nameof(GrantedDisplay));
                Raise(nameof(GrantedTone));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise([System.Runtime.CompilerServices.CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>行显示文本：优先中文权限名，无则用技术名</summary>
    public string DisplayText => string.IsNullOrEmpty(Label) ? Name : Label;

    /// <summary>作用说明是否存在（驱动第二行显示）</summary>
    public bool HasDescription => !string.IsNullOrEmpty(Description);

    /// <summary>开关提示</summary>
    public string ToggleHint => CanToggle
        ? (ToggleMode == "appop"
            ? "特殊权限（悬浮窗/修改设置），通过 appops 切换；停用会弹确认"
            : "运行时权限，通过 pm grant/revoke 切换；停用会弹确认")
        : "安装时授予的普通/签名级权限，adb 无法更改";

    /// <summary>授予状态文案</summary>
    public string GrantedDisplay => Granted ? "✓ 已允许" : "✗ 已禁止";

    /// <summary>授予状态颜色键（对话框 DataTrigger 使用）</summary>
    public string GrantedTone => Granted ? "safe" : "danger";
}

/// <summary>权限分组块（按保护级别分块展示；标题计数随开关切换实时更新）</summary>
public class PermissionGroup : INotifyPropertyChanged
{
    /// <summary>级别键（dangerous/normal/signature/development/unknown，驱动配色）</summary>
    public string LevelKey { get; }
    public PermLevel Level { get; }
    public List<PermissionItem> Items { get; }

    public PermissionGroup(PermLevel level, List<PermissionItem> items)
    {
        Level = level;
        Items = items;
        LevelKey = level switch
        {
            PermLevel.Dangerous => "dangerous",
            PermLevel.Normal => "normal",
            PermLevel.Signature => "signature",
            PermLevel.Development => "development",
            _ => "unknown",
        };
        foreach (var i in items)
            i.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(PermissionItem.Granted)) Raise(nameof(Title)); };
    }

    private string LevelDisplay => Level switch
    {
        PermLevel.Dangerous => "危险",
        PermLevel.Normal => "普通",
        PermLevel.Signature => "签名/系统",
        PermLevel.Development => "开发",
        _ => "未知",
    };

    /// <summary>块标题（含允许/禁止计数）</summary>
    public string Title => Items.Count == 0
        ? string.Empty
        : $"{LevelDisplay}权限（{Items.Count(i => i.Granted)} 允许 / {Items.Count - Items.Count(i => i.Granted)} 禁止）";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise([System.Runtime.CompilerServices.CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>启动管理行（Android 标准层等效控制；MagicOS 私有开关经真机实测不可读写）</summary>
public class StartupControlItem : INotifyPropertyChanged
{
    private string _valueText = string.Empty;
    private bool _toggleOn;

    private string _tone = "muted";

    public string Title { get; init; } = string.Empty;
    public string ValueText { get => _valueText; set { if (_valueText != value) { _valueText = value; Raise(); } } }
    /// <summary>状态色调键（safe/warn/danger/muted）</summary>
    public string Tone { get => _tone; set { if (_tone != value) { _tone = value; Raise(); } } }
    public string Note { get; init; } = string.Empty;
    /// <summary>是否有可交互开关</summary>
    public bool HasToggle { get; init; }
    public bool ToggleOn { get => _toggleOn; set { if (_toggleOn != value) { _toggleOn = value; Raise(); } } }
    /// <summary>开关类型：appops_bg（后台活动）/ boot（开机广播接收器组件）</summary>
    public string ToggleKind { get; init; } = string.Empty;
    /// <summary>开关提示</summary>
    public string ToggleHint { get; init; } = string.Empty;
    /// <summary>开机广播接收器组件全名列表（pkg/class，boot 类型用）</summary>
    public List<string> Components { get; init; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise([System.Runtime.CompilerServices.CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>应用详情对话框数据源</summary>
public class PackageDetail
{
    public string AppLabel { get; init; } = string.Empty;
    public string PackageName { get; init; } = string.Empty;
    public string VersionName { get; init; } = string.Empty;
    public string StatusDisplay { get; init; } = string.Empty;

    public List<PermissionGroup> Groups { get; init; } = new();
    public int PermissionTotal => Groups.Sum(g => g.Items.Count);
    public bool HasPermissions => PermissionTotal > 0;

    // 存储空间
    public bool StorageAvailable { get; init; }
    public string AppSizeText { get; init; } = "—";
    public string DataSizeText { get; init; } = "—";
    public string CacheSizeText { get; init; } = "—";
    public string TotalSizeText { get; init; } = "—";

    // 流量使用
    public bool TrafficAvailable { get; init; }
    public string RxText { get; init; } = "—";
    public string TxText { get; init; } = "—";
    public string TotalTrafficText { get; init; } = "—";

    // 启动控制
    public List<StartupControlItem> StartupControls { get; init; } = new();
}
