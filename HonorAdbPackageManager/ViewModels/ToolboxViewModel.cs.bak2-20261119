using System.Collections.ObjectModel;
using HonorAdbPackageManager.Commands;

namespace HonorAdbPackageManager.ViewModels;

/// <summary>
/// 快速工具箱 ViewModel：仿鸿蒙工具箱的分类卡片布局。
/// 条目按包名绑定到主列表 PackageItem；所有操作复用主列表的双重确认逻辑。
/// </summary>
public class ToolboxViewModel : ViewModelBase
{
    private readonly MainViewModel _main;

    public ObservableCollection<ToolboxCategory> Categories { get; } = new();

    /// <summary>包名 → ToolboxItem 索引（绑定时用，含备选包名）</summary>
    private readonly Dictionary<string, ToolboxItem> _index = new(StringComparer.OrdinalIgnoreCase);

    public RelayCommand ToolboxActionCommand { get; }
    public RelayCommand ToolboxRestoreCommand { get; }
    public RelayCommand SafePresetCommand { get; }
    public RelayCommand RestoreAllCommand { get; }

    /// <summary>本会话通过工具箱停用过的条目（还原本会话用）</summary>
    private readonly List<ToolboxItem> _sessionDisabled = new();

    public ToolboxViewModel(MainViewModel main)
    {
        _main = main;
        BuildCategories();

        ToolboxActionCommand = new RelayCommand(async p => await DoActionAsync(p as ToolboxItem));
        ToolboxRestoreCommand = new RelayCommand(async p => await DoRestoreAsync(p as ToolboxItem));
        SafePresetCommand = new RelayCommand(async _ => await DoSafePresetAsync());
        RestoreAllCommand = new RelayCommand(async _ => await DoRestoreAllAsync());

        // 主列表加载完成/条目变化 → 重新绑定
        _main.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsLoadingPackages) && !_main.IsLoadingPackages)
                RebindAll();
        };
        _main.Items.CollectionChanged += (_, _) => { /* 增量绑定在加载完成后统一做 */ };
    }

    private void AddCategory(ToolboxCategory c) => Categories.Add(c);

    private ToolboxItem AddItem(ToolboxCategory c, string name, string pkg, bool safe = false, params string[] alts)
    {
        var item = new ToolboxItem(name, pkg, safe, alts);
        c.Items.Add(item);
        _index[item.PackageName] = item;
        foreach (var a in item.Alternates)
            if (!_index.ContainsKey(a)) _index[a] = item;
        return item;
    }

    /// <summary>构建分类（仿鸿蒙工具箱布局，内容替换为荣耀包）</summary>
    private void BuildCategories()
    {
        // ── 卡片1：荣耀全家桶 ──
        var c1 = new ToolboxCategory("荣耀全家桶", "#1AAE9F");
        AddItem(c1, "荣耀钱包", "com.hihonor.wallet", false, "com.huawei.wallet");
        AddItem(c1, "荣耀视频", "com.hihonor.hnvideoplayer", false, "com.huawei.himovie");
        AddItem(c1, "荣耀音乐", "com.hihonor.music", false, "com.huawei.music");
        AddItem(c1, "荣耀阅读", "com.hihonor.hnreader", false, "com.huawei.hwireader");
        AddItem(c1, "荣耀浏览器", "com.honor.browser", false, "com.hihonor.browser");
        AddItem(c1, "荣耀智慧搜索", "com.hihonor.searchservice", false, "com.huawei.search");
        AddItem(c1, "荣耀应用市场", "com.hihonor.appmarket", false, "com.hihonor.market");
        AddItem(c1, "运动健康", "com.hihonor.health", false, "com.huawei.health");
        AddItem(c1, "游戏中心", "com.hihonor.honorcool", false, "com.huawei.gamebox");
        AddItem(c1, "荣耀商城", "com.hihonor.vmall");
        AddItem(c1, "我的荣耀", "com.hihonor.memberservice");
        AddCategory(c1);

        // ── 卡片2：智慧增值服务 ──
        var c2 = new ToolboxCategory("智慧增值服务", "#1AAE9F");
        AddItem(c2, "智慧搜索", "com.hihonor.search", false, "com.huawei.hwsearch");
        AddItem(c2, "智慧助手", "com.hihonor.assistant", false, "com.huawei.intelligent");
        AddItem(c2, "智慧识屏", "com.hihonor.pengine", false, "com.huawei.hitouch");
        AddItem(c2, "YOYO 语音", "com.hihonor.vassistant", false, "com.huawei.vassistant");
        AddItem(c2, "智慧视觉(扫一扫)", "com.hihonor.scanner", false, "com.huawei.scanner");
        AddItem(c2, "负一屏", "com.hihonor.hiboard", false, "com.huawei.recsys");
        AddItem(c2, "快应用中心", "com.hihonor.fastapp", false, "com.huawei.fastapp");
        AddItem(c2, "服务卡片", "com.hihonor.sceneservice", false, "com.huawei.hicard");
        AddItem(c2, "智慧推荐", "com.hihonor.recsys", false, "com.huawei.recsys");
        AddItem(c2, "智慧建议", "com.hihonor.suggestion", false, "com.huawei.suggestion");
        AddCategory(c2);

        // ── 卡片3：系统功能（可停用） ──
        var c3 = new ToolboxCategory("系统功能", "#8C87C2");
        AddItem(c3, "主题", "com.hihonor.thememanager", false, "com.huawei.android.thememanager");
        AddItem(c3, "天气", "com.hihonor.android.totemweather", false, "com.huawei.android.totemweather");
        AddItem(c3, "畅连", "com.hihonor.hiskytone", false, "com.huawei.hiskytone");
        AddItem(c3, "备份", "com.hihonor.kobackup", false, "com.huawei.KoBackup");
        AddItem(c3, "彩信服务", "com.hihonor.mms.service", false, "com.android.mms.service");
        AddItem(c3, "旅行助手", "com.hihonor.scenepack", false, "com.huawei.scenepack");
        AddItem(c3, "花瓣剪辑", "com.hihonor.videoeditor");
        AddItem(c3, "杂志锁屏", "com.hihonor.magazine", false, "com.huawei.magazine");
        AddItem(c3, "系统更新", "com.hihonor.ouc", false, "com.huawei.android.hwouc");
        AddItem(c3, "查找设备", "com.hihonor.findmydevice", false, "com.hihonor.android.findmyphone", "com.huawei.android.findmyphone");
        AddCategory(c3);

        // ── 卡片4：专项功能（高频精简对象） ──
        var c4 = new ToolboxCategory("专项功能", "#8C87C2");
        AddItem(c4, "云空间", "com.hihonor.hncloud", false, "com.hihonor.cloud", "com.huawei.hicloud");
        AddItem(c4, "天际通", "com.hihonor.hiskytone.data", false, "com.huawei.skytone");
        AddItem(c4, "讯飞引擎", "com.hihonor.voiceengine");
        AddItem(c4, "工程菜单", "com.hihonor.android.projectmenu", true, "com.android.huawei.projectmenu");
        AddItem(c4, "多屏协同", "com.hihonor.dmsdp", false, "com.huawei.dmsdp");
        AddItem(c4, "紧急信息", "com.hihonor.emergency", false, "com.android.emergency");
        AddItem(c4, "广告服务", "com.hihonor.adsservice", true);
        AddItem(c4, "智慧推荐广告", "com.hihonor.intelligent", true, "com.huawei.intelligent");
        AddCategory(c4);

        // ── 卡片5：冗余服务（安全预设，支持一键停用） ──
        var c5 = new ToolboxCategory("冗余服务（可一键停用）", "#1AAE9F");
        AddItem(c5, "杂志锁屏订阅", "com.hihonor.magazine", true, "com.huawei.magazine");
        AddItem(c5, "广告服务", "com.hihonor.adsservice", true);
        AddItem(c5, "智慧推荐", "com.hihonor.recsys", true);
        AddItem(c5, "智慧建议", "com.hihonor.suggestion", true);
        AddItem(c5, "智慧推荐引擎", "com.hihonor.pengine", true);
        AddItem(c5, "用户体验分析", "com.hihonor.bd", true, "com.huawei.bd");
        AddItem(c5, "后台查杀", "com.hihonor.powergenie", true, "com.huawei.powergenie");
        AddItem(c5, "日志上报", "com.hihonor.android.chr", true, "com.huawei.android.chr");
        AddItem(c5, "问题反馈", "com.hihonor.feedback", true);
        AddItem(c5, "使用技巧", "com.hihonor.tips", true, "com.huawei.android.tips");
        AddItem(c5, "玩机技巧推送", "com.hihonor.tipsowe", true, "com.huawei.tipsove");
        AddItem(c5, "快应用中心", "com.hihonor.fastapp", true);
        AddCategory(c5);
    }

    /// <summary>包列表加载完成后重新绑定全部条目</summary>
    public void RebindAll()
    {
        foreach (var cat in Categories)
        {
            foreach (var item in cat.Items)
            {
                // 主包名优先，其次备选包名；都找不到则视为未安装
                PackageItem? found = null;
                foreach (var it in _main.Items)
                {
                    if (it.PackageName.Equals(item.PackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = it;
                        break;
                    }
                    foreach (var a in item.Alternates)
                    {
                        if (it.PackageName.Equals(a, StringComparison.OrdinalIgnoreCase))
                        {
                            found = it;
                            break;
                        }
                    }
                    if (found is not null) break;
                }

                if (found is null)
                    item.MarkMissing();
                else
                    item.Bind(found);
            }
        }
    }

    // ================= 操作（全部走主 VM 的双重确认） =================

    private async Task DoActionAsync(ToolboxItem? item)
    {
        if (item?.Bound is null) return;
        // 复用主列表操作：内部含禁止拦截 + 勾选确认 + 最终确认
        await _main.ExecutePackageActionPublicAsync(item.Bound);
        item.UpdateFromBound();
        if (item.Bound.Status == PackageStatus.Disabled && !_sessionDisabled.Contains(item))
            _sessionDisabled.Add(item);
    }

    private async Task DoRestoreAsync(ToolboxItem? item)
    {
        if (item?.Bound is null) return;
        await _main.ExecutePackageActionPublicAsync(item.Bound);
        item.UpdateFromBound();
        if (item.Bound.Status == PackageStatus.Enabled)
            _sessionDisabled.Remove(item);
    }

    /// <summary>一键停用冗余服务：逐个确认（每个都弹双重确认），不停用禁止项</summary>
    private async Task DoSafePresetAsync()
    {
        var targets = new List<ToolboxItem>();
        foreach (var cat in Categories)
            foreach (var it in cat.Items)
                if (it.IsSafePreset && it.Bound is not null && it.Bound.Status == PackageStatus.Enabled && it.Bound.CanUninstall)
                    targets.Add(it);

        if (targets.Count == 0)
        {
            MsgBox.Show("没有可停用的冗余服务（可能已全部停用或未加载包列表）。",
                "一键停用", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 列出具体要停用的冗余服务，用户确认前心里有数
        var listText = string.Join(Environment.NewLine,
            targets.Select(t => $"  • {t.Name}（{t.Bound!.PackageName}）- {t.Bound.StatusDisplay}"));
        var proceed = MsgBox.Show(
            $"将逐个双重确认后停用以下 {targets.Count} 个冗余服务：\n\n{listText}\n\n每个包都会弹出确认弹窗，你可以在弹窗中逐个取消。\n\n继续吗？",
            "一键停用冗余服务", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (proceed != MessageBoxResult.Yes) return;

        foreach (var t in targets)
        {
            if (t.Bound is null || t.Bound.Status != PackageStatus.Enabled) continue;
            await DoActionAsync(t);
        }
        MsgBox.Show("冗余服务处理完成。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>还原本会话所有停用（粉色卡：还原所有操作）</summary>
    private async Task DoRestoreAllAsync()
    {
        if (_sessionDisabled.Count == 0)
        {
            MsgBox.Show("本次会话没有通过工具箱停用过任何包。", "还原所有操作",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var restoreList = string.Join(Environment.NewLine,
            _sessionDisabled.Where(x => x.Bound is not null).Select(t => $"  • {t.Name}（{t.Bound!.PackageName}）"));
        var proceed = MsgBox.Show(
            $"将还原本次会话通过工具箱停用的 {_sessionDisabled.Count} 个包（逐个双重确认）：\n\n{restoreList}\n\n继续吗？",
            "还原所有操作", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (proceed != MessageBoxResult.Yes) return;

        foreach (var t in _sessionDisabled.ToList())
        {
            if (t.Bound is null || t.Bound.Status != PackageStatus.Disabled) continue;
            await DoRestoreAsync(t);
        }
        MsgBox.Show("还原完成。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
