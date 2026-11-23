namespace HonorAdbPackageManager.Services;

/// <summary>
/// 内置 Android 权限中文说明库（约 120 条常用标准权限 + 前台服务族规则 + 分类兜底）。
/// 用途：权限列表「中文备注」的可靠来源——当设备 pm list permissions -g -f 输出缺失
/// label/description、或设备系统语言非中文时，仍保证每一行权限都有中文名称与作用说明。
/// 内容整理自 AOSP 权限文档与真机（荣耀 Magic5 Pro / MagicOS 11）实测输出。
/// </summary>
public static class PermissionDescriptions
{
    /// <summary>权限名 →（中文短名，中文作用说明）</summary>
    private static readonly Dictionary<string, (string Label, string Desc)> Map = new(StringComparer.Ordinal)
    {
        // ── 网络与连接 ──
        ["android.permission.INTERNET"] = ("访问网络", "允许应用创建网络连接（联网基础权限，几乎所有应用都需要）"),
        ["android.permission.ACCESS_NETWORK_STATE"] = ("查看网络状态", "获取当前网络类型与连接状态"),
        ["android.permission.ACCESS_WIFI_STATE"] = ("查看 WLAN 状态", "获取已连接 Wi-Fi 的信息"),
        ["android.permission.CHANGE_WIFI_STATE"] = ("更改 WLAN 状态", "连接/断开 Wi-Fi、切换网络"),
        ["android.permission.CHANGE_WIFI_MULTICAST_STATE"] = ("更改 WLAN 多播", "接收局域网多播数据（如设备发现）"),
        ["android.permission.CHANGE_NETWORK_STATE"] = ("更改网络连接", "更改网络连接状态"),
        ["android.permission.NEARBY_WIFI_DEVICES"] = ("附近设备", "查找并连接附近设备（热点、投屏等；Android 13+）"),

        // ── 位置 ──
        ["android.permission.ACCESS_FINE_LOCATION"] = ("精确位置", "通过 GPS/北斗获取精确位置"),
        ["android.permission.ACCESS_COARSE_LOCATION"] = ("大致位置", "通过网络/基站获取大致位置"),
        ["android.permission.ACCESS_BACKGROUND_LOCATION"] = ("后台定位", "应用在后台持续获取位置（耗电且敏感）"),
        ["android.permission.ACCESS_LOCATION_EXTRA_COMMANDS"] = ("附加定位命令", "向定位服务发送附加命令"),
        ["android.permission.LOCATION_HARDWARE"] = ("硬件定位", "使用低延迟的硬件级定位能力"),

        // ── 蓝牙 / NFC / 近场 ──
        ["android.permission.BLUETOOTH_SCAN"] = ("蓝牙扫描", "扫描附近蓝牙设备（可能间接推断位置；Android 12+）"),
        ["android.permission.BLUETOOTH_CONNECT"] = ("连接蓝牙设备", "与已配对的蓝牙设备通信（传输数据）"),
        ["android.permission.BLUETOOTH_ADVERTISE"] = ("蓝牙广播", "让附近设备发现本机（可被用于定位）"),
        ["android.permission.BLUETOOTH"] = ("使用蓝牙（旧版）", "连接蓝牙设备（Android 11 及以下）"),
        ["android.permission.BLUETOOTH_ADMIN"] = ("蓝牙管理（旧版）", "搜索、配对蓝牙设备（Android 11 及以下）"),
        ["android.permission.BLUETOOTH_PRIVILEGED"] = ("蓝牙特权", "免用户确认完成蓝牙配对（系统级）"),
        ["android.permission.UWB_RANGING"] = ("超宽带测距", "精确测量与附近设备的距离（UWB）"),
        ["android.permission.NFC"] = ("使用 NFC", "读取 NFC 标签、使用 NFC 功能"),

        // ── 相机 / 麦克风 / 媒体 ──
        ["android.permission.CAMERA"] = ("使用相机", "拍摄照片与视频"),
        ["android.permission.RECORD_AUDIO"] = ("录制音频", "使用麦克风录音（语音输入、通话录音）"),
        ["android.permission.MODIFY_AUDIO_SETTINGS"] = ("调整音频设置", "修改音量、音频路由等设置"),
        ["android.permission.READ_MEDIA_IMAGES"] = ("读取图片", "访问共享存储中的照片（Android 13+）"),
        ["android.permission.READ_MEDIA_VIDEO"] = ("读取视频", "访问共享存储中的视频（Android 13+）"),
        ["android.permission.READ_MEDIA_AUDIO"] = ("读取音频", "访问共享存储中的音乐等音频（Android 13+）"),
        ["android.permission.READ_MEDIA_VISUAL_USER_SELECTED"] = ("访问选定照片视频", "仅访问用户在照片选择器中选定的项目"),
        ["android.permission.READ_EXTERNAL_STORAGE"] = ("读取存储空间", "读取共享存储文件（Android 12-；13+ 仅限媒体）"),
        ["android.permission.WRITE_EXTERNAL_STORAGE"] = ("写入存储空间", "在共享存储中创建/修改文件（Android 11-）"),
        ["android.permission.MANAGE_EXTERNAL_STORAGE"] = ("管理所有文件", "「所有文件访问权限」：读写整个共享存储"),
        ["android.permission.MANAGE_MEDIA"] = ("管理媒体文件", "不经确认批量修改媒体（移动、删除等）"),
        ["android.permission.ACCESS_MEDIA_LOCATION"] = ("读取媒体位置", "从照片/视频读取拍摄地点（EXIF）"),

        // ── 联系人 / 账户 ──
        ["android.permission.READ_CONTACTS"] = ("读取联系人", "读取通讯录中的联系人数据"),
        ["android.permission.WRITE_CONTACTS"] = ("修改联系人", "添加、修改、删除联系人"),
        ["android.permission.GET_ACCOUNTS"] = ("获取设备账户", "读取设备上的账户列表"),

        // ── 电话 / 短信 ──
        ["android.permission.READ_CALL_LOG"] = ("读取通话记录", "查看来电与去电记录"),
        ["android.permission.WRITE_CALL_LOG"] = ("修改通话记录", "写入通话记录"),
        ["android.permission.CALL_PHONE"] = ("拨打电话", "无需确认直接拨号（可能产生资费）"),
        ["android.permission.CALL_PRIVILEGED"] = ("特权拨号", "拨打任意号码，含紧急号码（系统级）"),
        ["android.permission.ANSWER_PHONE_CALLS"] = ("接听来电", "通过应用界面接听来电"),
        ["android.permission.ACCEPT_HANDOVER"] = ("通话移交", "把来电移交给其他应用继续通话"),
        ["android.permission.READ_PHONE_STATE"] = ("读取手机状态", "读取设备标识、电话状态与基站信息"),
        ["android.permission.READ_PHONE_NUMBERS"] = ("读取本机号码", "读取设备的电话号码"),
        ["android.permission.READ_PRIVILEGED_PHONE_STATE"] = ("特权手机状态", "读取更多电话状态信息（系统级）"),
        ["android.permission.ADD_VOICEMAIL"] = ("添加语音信箱", "向系统语音信箱添加消息"),
        ["android.permission.USE_SIP"] = ("网络电话", "使用 SIP 协议进行 VoIP 通话"),
        ["android.permission.PROCESS_OUTGOING_CALLS"] = ("处理去电（旧版）", "拦截或重定向拨出的电话（已废弃）"),
        ["android.permission.READ_SMS"] = ("读取短信", "读取短信内容（含验证码，敏感）"),
        ["android.permission.SEND_SMS"] = ("发送短信", "发送短信（可能产生资费）"),
        ["android.permission.RECEIVE_SMS"] = ("接收短信", "收到短信时读取其内容"),
        ["android.permission.RECEIVE_MMS"] = ("接收彩信", "收到彩信时读取其内容"),
        ["android.permission.RECEIVE_WAP_PUSH"] = ("接收 WAP 推送", "接收 WAP 推送消息"),

        // ── 日历 ──
        ["android.permission.READ_CALENDAR"] = ("读取日历", "读取日程事件"),
        ["android.permission.WRITE_CALENDAR"] = ("修改日历", "添加、修改、删除日程"),

        // ── 身体传感 / 生物识别 ──
        ["android.permission.BODY_SENSORS"] = ("身体传感器", "读取心率等身体数据（佩戴设备）"),
        ["android.permission.BODY_SENSORS_BACKGROUND"] = ("后台身体传感", "在后台持续读取身体数据"),
        ["android.permission.ACTIVITY_RECOGNITION"] = ("活动识别", "识别步行/跑步/骑行等身体活动（计步）"),
        ["android.permission.HIGH_SAMPLING_RATE_SENSORS"] = ("高频传感器", "以高于 200Hz 的频率读取传感器"),
        ["android.permission.USE_BIOMETRIC"] = ("生物识别", "使用指纹/人脸等生物特征验证"),
        ["android.permission.USE_FINGERPRINT"] = ("使用指纹（旧版）", "旧版指纹识别接口（已由生物识别取代）"),

        // ── 通知 / 提醒 / 常驻 ──
        ["android.permission.POST_NOTIFICATIONS"] = ("发送通知", "发布应用通知（Android 13+）"),
        ["android.permission.USE_FULL_SCREEN_INTENT"] = ("全屏通知", "让来电等通知全屏显示"),
        ["android.permission.SET_ALARM"] = ("设置闹钟", "在系统闹钟中设置提醒"),
        ["android.permission.SCHEDULE_EXACT_ALARM"] = ("精确闹钟", "在精确时间触发任务（需在设置中授权）"),
        ["android.permission.USE_EXACT_ALARM"] = ("精确闹钟（豁免）", "闹钟/日历类应用可用的精确触发"),
        ["android.permission.FOREGROUND_SERVICE"] = ("前台服务", "启动前台服务（后台常驻运行并显示通知）"),
        ["android.permission.RECEIVE_BOOT_COMPLETED"] = ("开机自启", "开机完成后自动运行"),
        ["android.permission.WAKE_LOCK"] = ("保持唤醒", "防止设备休眠（保持 CPU/屏幕唤醒）"),
        ["android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS"] = ("忽略电池优化", "申请加入电池优化白名单（后台不受限）"),
        ["android.permission.BATTERY_STATS"] = ("电池统计", "读取电池使用统计"),
        ["android.permission.VIBRATE"] = ("震动", "控制震动马达"),

        // ── 窗口 / 系统设置 ──
        ["android.permission.SYSTEM_ALERT_WINDOW"] = ("显示悬浮窗", "在其他应用上层显示内容（「悬浮窗」开关）"),
        ["android.permission.WRITE_SETTINGS"] = ("修改系统设置", "修改系统设置项（「修改系统设置」开关）"),
        ["android.permission.WRITE_SECURE_SETTINGS"] = ("修改安全设置", "修改安全级系统设置（仅系统/ADB）"),
        ["android.permission.WRITE_APN_SETTINGS"] = ("修改接入点", "更改网络 APN/接入点设置"),
        ["android.permission.SET_TIME"] = ("修改系统时间", "更改设备时钟"),
        ["android.permission.SET_TIME_ZONE"] = ("修改时区", "更改设备时区"),
        ["android.permission.SET_WALLPAPER"] = ("设置壁纸", "更换系统壁纸"),
        ["android.permission.SET_WALLPAPER_HINTS"] = ("壁纸建议", "获取壁纸尺寸提示"),
        ["android.permission.STATUS_BAR"] = ("状态栏控制", "禁用或修改系统状态栏（系统级）"),
        ["android.permission.EXPAND_STATUS_BAR"] = ("展开状态栏", "展开/收起通知栏（系统级）"),

        // ── 应用管理 / 任务 ──
        ["android.permission.REQUEST_INSTALL_PACKAGES"] = ("安装应用", "请求安装未知来源应用（「允许安装」开关）"),
        ["android.permission.REQUEST_DELETE_PACKAGES"] = ("卸载应用", "请求卸载应用（会弹系统确认）"),
        ["android.permission.PACKAGE_USAGE_STATS"] = ("应用使用情况", "读取应用使用统计（需在设置中授权）"),
        ["android.permission.QUERY_ALL_PACKAGES"] = ("查询所有应用", "获取设备上全部应用列表"),
        ["android.permission.GET_PACKAGE_SIZE"] = ("计算应用空间", "计算应用占用的存储空间"),
        ["android.permission.KILL_BACKGROUND_PROCESSES"] = ("结束后台进程", "结束其他应用的后台进程"),
        ["android.permission.RESTART_PACKAGES"] = ("重启应用（旧版）", "强制结束其他应用（已废弃）"),
        ["android.permission.REORDER_TASKS"] = ("调整任务顺序", "调整应用任务的前后顺序"),
        ["android.permission.GET_TASKS"] = ("获取任务（旧版）", "读取正在运行的应用（已废弃）"),
        ["android.permission.REQUEST_PASSWORD_COMPLEXITY"] = ("密码强度", "查询锁屏密码的安全等级"),
        ["android.permission.BROADCAST_STICKY"] = ("粘性广播", "发送系统常驻的粘性广播"),

        // ── 系统级 / 开发调试 ──
        ["android.permission.DUMP"] = ("系统转储", "获取 dumpsys 诊断信息（开发调试）"),
        ["android.permission.READ_LOGS"] = ("读取系统日志", "读取系统日志文件（开发/ADB）"),
        ["android.permission.READ_FRAME_BUFFER"] = ("读取屏幕", "抓取当前屏幕画面（系统级）"),
        ["android.permission.CAPTURE_AUDIO_OUTPUT"] = ("捕获音频输出", "录制系统正在播放的声音（系统级）"),
        ["android.permission.MEDIA_CONTENT_CONTROL"] = ("媒体控制", "控制全局媒体播放（系统级）"),
        ["android.permission.MODIFY_PHONE_STATE"] = ("修改电话状态", "更改通话模式等电话状态（系统级）"),
        ["android.permission.MOUNT_UNMOUNT_FILESYSTEMS"] = ("挂载文件系统", "挂载/卸载外部存储（系统级）"),
        ["android.permission.REBOOT"] = ("重启设备", "重启手机（系统级）"),
        ["android.permission.MASTER_CLEAR"] = ("恢复出厂设置", "抹掉设备全部数据（系统级）"),

        // ── 特殊服务绑定（多由系统授予） ──
        ["android.permission.BIND_NOTIFICATION_LISTENER_SERVICE"] = ("通知使用权", "读取所有通知内容（需在设置中授权）"),
        ["android.permission.BIND_ACCESSIBILITY_SERVICE"] = ("无障碍服务", "作为无障碍服务读取屏幕内容（需在设置中授权）"),
        ["android.permission.BIND_DEVICE_ADMIN"] = ("设备管理器", "作为设备管理器执行锁定/擦除等策略"),
        ["android.permission.BIND_VPN_SERVICE"] = ("VPN 服务", "作为 VPN 接管全部网络流量"),
        ["android.permission.BIND_INPUT_METHOD"] = ("输入法服务", "作为输入法接收全部按键输入"),
        ["android.permission.BIND_AUTOFILL_SERVICE"] = ("自动填充服务", "自动填充表单与密码（需在设置中授权）"),
        ["android.permission.BIND_PRINT_SERVICE"] = ("打印服务", "作为打印服务处理打印任务"),
        ["android.permission.BIND_APPWIDGET"] = ("桌面小部件", "向桌面添加应用小部件（系统绑定）"),

        // ── 账户同步 / 浏览器（旧版） ──
        ["android.permission.READ_SYNC_SETTINGS"] = ("读取同步设置", "读取账户的同步开关"),
        ["android.permission.WRITE_SYNC_SETTINGS"] = ("修改同步设置", "开启/关闭账户同步"),
        ["android.permission.READ_SYNC_STATS"] = ("读取同步状态", "查看账户同步情况"),
        ["android.permission.MANAGE_ACCOUNTS"] = ("管理账户（旧版）", "添加/删除账户（已废弃）"),
        ["android.permission.AUTHENTICATE_ACCOUNTS"] = ("账户验证（旧版）", "作为账户验证器（已废弃）"),
        ["android.permission.USE_CREDENTIALS"] = ("使用凭证（旧版）", "请求账户授权令牌（已废弃）"),
        ["android.permission.READ_HISTORY_BOOKMARKS"] = ("读取书签历史（旧版）", "读取浏览器书签与历史（已废弃）"),
        ["android.permission.WRITE_HISTORY_BOOKMARKS"] = ("修改书签历史（旧版）", "修改浏览器书签与历史（已废弃）"),
    };

    /// <summary>前台服务子类型 → 中文（android.permission.FOREGROUND_SERVICE_xxx 族兜底）</summary>
    private static readonly Dictionary<string, string> FgsTypes = new(StringComparer.Ordinal)
    {
        ["CAMERA"] = "相机",
        ["LOCATION"] = "定位",
        ["MICROPHONE"] = "麦克风",
        ["MEDIA_PLAYBACK"] = "媒体播放",
        ["MEDIA_PROJECTION"] = "屏幕采集（录屏/投屏）",
        ["PHONE_CALL"] = "通话",
        ["DATA_SYNC"] = "数据同步",
        ["HEALTH"] = "健康运动",
        ["REMOTE_MESSAGING"] = "即时消息",
        ["FILE_MANAGEMENT"] = "文件管理",
        ["MEDIA_PROCESSING"] = "媒体处理",
        ["SPECIAL_USE"] = "特殊用途",
        ["SYSTEM_EXEMPTED"] = "系统豁免",
        ["GAME"] = "游戏",
        ["SHORT_SERVICE"] = "短时任务",
    };

    /// <summary>
    /// 精确/规则查找中文说明。返回 true 表示内置库收录（中文名或作用说明可用）；
    /// 未收录返回 false，由 Fallback 提供分类兜底文案。
    /// </summary>
    public static bool TryGet(string permission, out string label, out string desc)
    {
        label = string.Empty;
        desc = string.Empty;
        if (string.IsNullOrEmpty(permission)) return false;

        if (Map.TryGetValue(permission, out var hit))
        {
            label = hit.Label;
            desc = hit.Desc;
            return true;
        }

        // 前台服务族：android.permission.FOREGROUND_SERVICE_<TYPE>
        const string FgsPrefix = "android.permission.FOREGROUND_SERVICE_";
        if (permission.StartsWith(FgsPrefix, StringComparison.Ordinal))
        {
            var type = permission[FgsPrefix.Length..];
            var typeCn = FgsTypes.TryGetValue(type, out var t) ? t : type;
            desc = $"允许应用启动「{typeCn}」类前台服务（后台持续运行并显示常驻通知）";
            return true;
        }

        return false;
    }

    /// <summary>未收录权限的分类兜底说明（保证权限列表每一行都有中文备注）</summary>
    public static string Fallback(string permission)
    {
        if (string.IsNullOrEmpty(permission)) return "权限说明未收录";
        if (permission.StartsWith("com.hihonor.", StringComparison.Ordinal)
            || permission.StartsWith("com.honor.", StringComparison.Ordinal))
            return "荣耀系统自定义权限（非 AOSP 标准），作用未收录";
        if (permission.StartsWith("com.huawei.", StringComparison.Ordinal))
            return "华为系统自定义权限（非 AOSP 标准），作用未收录";
        if (permission.StartsWith("android.permission.", StringComparison.Ordinal))
            return "AOSP 标准权限，内置库未收录详细说明";
        return "应用/厂商自定义权限，作用未收录";
    }
}
