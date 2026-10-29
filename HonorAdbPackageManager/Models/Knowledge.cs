namespace HonorAdbPackageManager.Models;

/// <summary>内置知识库中单个包的说明条目</summary>
public class KnowledgeEntry
{
    /// <summary>中文显示名称（如：荣耀浏览器）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>功能描述</summary>
    public string Desc { get; set; } = string.Empty;

    /// <summary>风险等级字符串：安全/低风险/高风险/禁止操作</summary>
    public string Risk { get; set; } = "低风险";

    /// <summary>是否允许停用</summary>
    public bool CanDisable { get; set; } = true;

    /// <summary>补充备注（如 MagicOS 组件说明）</summary>
    public string Remark { get; set; } = string.Empty;
}

/// <summary>荣耀 MagicOS 包名知识库根对象</summary>
public class KnowledgeDatabase
{
    public string SchemaVersion { get; set; } = "1";
    public string UpdatedAt { get; set; } = string.Empty;
    public Dictionary<string, KnowledgeEntry> Packages { get; set; } = new();
}
