using System.Collections.ObjectModel;
using System.Windows.Media;

namespace HonorAdbPackageManager.Models;

/// <summary>主列表每一行的数据源（知识库匹配后的完整包信息）</summary>
public class PackageItem : INotifyPropertyChanged
{
    private ImageSource? _icon;
    private string _appLabel = string.Empty;
    private string _description = string.Empty;
    private PackageStatus _status = PackageStatus.Enabled;
    private RiskLevel _risk = RiskLevel.Low;
    private PackageType _type = PackageType.ThirdParty;
    private bool _canUninstall = true;
    private string _remark = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public string AppLabel
    {
        get => _appLabel;
        set { if (_appLabel != value) { _appLabel = value; Raise(nameof(AppLabel)); } }
    }

    /// <summary>图标（从 APK 提取；null 时显示首字母占位）</summary>
    public ImageSource? Icon
    {
        get => _icon;
        set { if (!ReferenceEquals(_icon, value)) { _icon = value; Raise(nameof(Icon)); } }
    }

    public PackageType PackageType
    {
        get => _type;
        set { if (_type != value) { _type = value; Raise(nameof(PackageType)); Raise(nameof(ActionButtonText)); } }
    }

    public RiskLevel UninstallRisk
    {
        get => _risk;
        set { if (_risk != value) { _risk = value; Raise(nameof(UninstallRisk)); } }
    }

    public PackageStatus Status
    {
        get => _status;
        set { if (_status != value) { _status = value; Raise(nameof(Status)); Raise(nameof(ActionButtonText)); } }
    }

    /// <summary>是否允许 卸载/停用（核心包 false，按钮灰化）</summary>
    public bool CanUninstall
    {
        get => _canUninstall;
        set { if (_canUninstall != value) { _canUninstall = value; Raise(nameof(CanUninstall)); } }
    }

    public string Description
    {
        get => _description;
        set { if (_description != value) { _description = value; Raise(nameof(Description)); } }
    }

    public string VersionName { get; set; } = string.Empty;

    public string SourceApkPath { get; set; } = string.Empty;

    public string Remark
    {
        get => _remark;
        set { if (_remark != value) { _remark = value; Raise(nameof(Remark)); } }
    }

    /// <summary>未知包 = 知识库未收录（操作时强制二次确认）</summary>
    public bool IsUnknown { get; set; }

    /// <summary>会话内操作历史（时间 + 动作 + 结果）</summary>
    public ObservableCollection<string> HistoryView { get; } = new();

    public string TypeDisplay => PackageType switch
    {
        PackageType.ThirdParty => "第三方",
        PackageType.SystemNormal => "系统",
        PackageType.SystemCritical => "核心系统",
        _ => "?",
    };

    public string RiskDisplay => UninstallRisk switch
    {
        RiskLevel.Safe => "安全",
        RiskLevel.Low => "低风险",
        RiskLevel.High => "高风险",
        RiskLevel.Forbidden => "禁止操作",
        _ => "?",
    };

    public string StatusDisplay => Status switch
    {
        PackageStatus.Enabled => "已启用",
        PackageStatus.Disabled => "已停用",
        PackageStatus.Uninstalled => "已卸载",
        _ => "?",
    };

    /// <summary>操作按钮文案：第三方=卸载，系统=停用</summary>
    public string ActionButtonText => PackageType switch
    {
        PackageType.ThirdParty => "卸载",
        PackageType.SystemNormal => "停用",
        _ => "停用",
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void AddHistory(string action) =>
        HistoryView.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {action}");
}
