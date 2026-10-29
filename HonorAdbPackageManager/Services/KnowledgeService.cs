using System.Text.Json;

namespace HonorAdbPackageManager.Services;

/// <summary>
/// 荣耀包名知识库：程序目录 Data\HonorPackageDb.json。
/// 未收录包按【未知应用】处理（描述空 / 低风险 / 允许操作但二次确认）。
/// </summary>
public class KnowledgeService
{
    private const string EmbeddedResourceName = "HonorAdbPackageManager.Data.HonorPackageDb.json";

    private readonly LogService _log;
    private Dictionary<string, Models.KnowledgeEntry> _map =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>条目数（界面显示）</summary>
    public int Count => _map.Count;

    /// <summary>当前加载来源描述</summary>
    public string Source { get; private set; } = "内置";

    public KnowledgeService(LogService log) => _log = log;

    /// <summary>启动加载：优先外部文件（允许用户手改），失败回退内嵌资源</summary>
    public void Load()
    {
        var external = Path.Combine(AppContext.BaseDirectory, "Data", "HonorPackageDb.json");
        if (File.Exists(external))
        {
            try
            {
                LoadFromFile(external);
                Source = $"外部({Count}条)";
                return;
            }
            catch (Exception ex)
            {
                _log.Error($"外部知识库解析失败，回退内置：{ex.Message}");
            }
        }
        LoadEmbedded();
    }

    private void LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var db = JsonSerializer.Deserialize<Models.KnowledgeDatabase>(json)
                 ?? throw new InvalidDataException("JSON 结构无效");
        _map = new Dictionary<string, KnowledgeEntry>(db.Packages, StringComparer.OrdinalIgnoreCase);
        _log.Info($"知识库已加载：{path}（{Count} 条）");
    }

    private void LoadEmbedded()
    {
        var asm = typeof(KnowledgeService).Assembly;
        using var stream = asm.GetManifestResourceStream(EmbeddedResourceName);
        if (stream is null)
        {
            _log.Error($"内嵌知识库资源缺失：{EmbeddedResourceName}");
            _map = new(StringComparer.OrdinalIgnoreCase);
            return;
        }
        var db = JsonSerializer.Deserialize<Models.KnowledgeDatabase>(stream)
                 ?? new Models.KnowledgeDatabase();
        _map = new Dictionary<string, KnowledgeEntry>(db.Packages, StringComparer.OrdinalIgnoreCase);
        Source = $"内置({Count}条)";
        _log.Info($"内嵌知识库已加载（{Count} 条）");
    }

    /// <summary>导入外部知识库（【导入外部知识库】按钮）</summary>
    public void Import(string path)
    {
        var json = File.ReadAllText(path);
        var db = JsonSerializer.Deserialize<Models.KnowledgeDatabase>(json)
                 ?? throw new InvalidDataException("JSON 结构无效（需要 Packages 字典）");

        // 合并导入：外部条目覆盖同名内置
        foreach (var kv in db.Packages)
            _map[kv.Key] = kv.Value;

        Source = $"导入合并({Count}条)";
        _log.Info($"知识库导入合并完成：{path}，当前共 {Count} 条");
    }

    /// <summary>匹配知识库条目；未收录返回 null</summary>
    public Models.KnowledgeEntry? Match(string packageName) =>
        _map.TryGetValue(packageName, out var e) ? e : null;
}
