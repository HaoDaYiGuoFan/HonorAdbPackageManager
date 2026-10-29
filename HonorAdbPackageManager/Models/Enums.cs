namespace HonorAdbPackageManager.Models;

/// <summary>包类型：第三方 / 普通系统 / 核心系统</summary>
public enum PackageType
{
    ThirdParty,
    SystemNormal,
    SystemCritical,
}

/// <summary>风险等级</summary>
public enum RiskLevel
{
    Safe,
    Low,
    High,
    Forbidden,
}

/// <summary>包当前状态</summary>
public enum PackageStatus
{
    Enabled,
    Disabled,
    Uninstalled,
}
