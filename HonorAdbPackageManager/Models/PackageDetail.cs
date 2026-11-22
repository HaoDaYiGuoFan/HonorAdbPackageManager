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

/// <summary>单条权限显示项</summary>
public class PermissionItem
{
    public string Name { get; init; } = string.Empty;
    /// <summary>系统中文名（pm list permissions -g -f 的 label；无则空）</summary>
    public string Label { get; init; } = string.Empty;
    public bool Granted { get; init; }
    public PermLevel Level { get; init; }

    /// <summary>行显示文本：优先中文权限名，无则用技术名</summary>
    public string DisplayText => string.IsNullOrEmpty(Label) ? Name : Label;

    /// <summary>级别显示名</summary>
    public string LevelDisplay => Level switch
    {
        PermLevel.Dangerous => "危险",
        PermLevel.Normal => "普通",
        PermLevel.Signature => "签名/系统",
        PermLevel.Development => "开发",
        _ => "未知",
    };

    /// <summary>授予状态文案</summary>
    public string GrantedDisplay => Granted ? "✓ 已允许" : "✗ 已禁止";

    /// <summary>授予状态颜色键（对话框 DataTrigger 使用）</summary>
    public string GrantedTone => Granted ? "safe" : "danger";
}

/// <summary>权限分组块（按保护级别分块展示）</summary>
public class PermissionGroup
{
    /// <summary>级别键（dangerous/normal/signature/development/unknown，驱动配色）</summary>
    public string LevelKey { get; }
    /// <summary>块标题（含允许/禁止计数）</summary>
    public string Title { get; }
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
        Title = items.Count == 0
            ? string.Empty
            : $"{items[0].LevelDisplay}权限（{items.Count(i => i.Granted)} 允许 / {items.Count - items.Count(i => i.Granted)} 禁止）";
    }
}

/// <summary>启动控制单行显示项</summary>
public class StartupControlItem
{
    public string Title { get; init; } = string.Empty;
    public string ValueText { get; init; } = string.Empty;
    /// <summary>状态色调键（safe/warn/danger/muted）</summary>
    public string Tone { get; init; } = "muted";
    public string Note { get; init; } = string.Empty;
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
