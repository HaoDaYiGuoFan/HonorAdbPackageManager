# HonorAdbPackageManager v1.4.0-r15

**免 ROOT 管理荣耀 MagicOS 手机的应用包** —— 读取全部应用包名，自动匹配内置中文知识库（名称/功能/风险），系统包一键停用还原、第三方包卸载、核心组件智能拦截、APK 一键备份；内置「快速工具箱」批量操作页与「动画调节 / 功能」快捷设置页。

> 本次为 **界面全面重做 + 快捷设置功能修复** 的修订版（r14 → r15）：整体换为**浅蓝小清新的 Windows Aero 玻璃风格**，修复字体发虚、黑底、搜索框文字截断等问题，并修好开发者快捷开关与状态栏图标精简（含实时网速）不生效的功能缺陷。功能范围与知识库保持不变。

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet)](#) [![WPF](https://img.shields.io/badge/UI-WPF%20%7C%20MVVM-blue)](#) [![Platform](https://img.shields.io/badge/OS-Windows%2010%2F11%20x64-success)](#) [![ADB](https://img.shields.io/badge/ADB-platform--tools%2037.0.1-orange)](#) [![Knowledge](https://img.shields.io/badge/知识库-1139%20条-informational)](#)

> ⚠️ 本工具为 ADB 调试辅助，**非 ROOT 工具**。系统包仅停用不删除；操作前建议备份手机数据。

---

## 📥 下载

| 文件 | 说明 |
| --- | --- |
| `HonorAdbPackageManager-v1.4.0-r15.zip` (67.2 MB) | 自包含发行包：内置 .NET 10 运行时 + ADB 37.0.1 + 知识库 1139 条，**解压即用，目标机无需安装任何依赖** |

- **大小**：70,495,569 字节（67.23 MB），268 个条目
- **SHA256**：`942EA1523A67BA0B4C577F8565671716D7D71661D0A0BF0E10503CD038FBBB5B`

```powershell
Get-FileHash .\HonorAdbPackageManager-v1.4.0-r15.zip -Algorithm SHA256
```

系统要求：Windows 10 (1709+) / Windows 11 x64。

> 发行包内 `Data\settings.json` 已重置为空配置，不含任何本机无线 ADB 地址。

---

## 🆕 本次更新（r14 → r15）

### 🎨 全新浅蓝小清新 · Windows Aero 玻璃界面

界面整体重做：由原先偏灰暗的深色材质改为**明亮通透的浅蓝白玻璃**，保留 Aero 的标志性元素——渐变高光、白色反光描边、柔和光斑、圆角与细蓝边。

| 元素 | 处理方式 |
| --- | --- |
| 窗口底色 | **不透明**浅蓝渐变 `#F9FCFF → #EAF4FF → #DDEEFF`，杜绝黑底与半透明发灰 |
| 玻璃质感 | WPF 自绘：白色高光渐变、上白下浅蓝的描边、角落柔和模糊光斑、顶部高光线 |
| 主色 | 亮蓝 `#2E7CF6`，深蓝灰文字 `#1B2B45`，浅蓝边框 `#D6E6F7` |
| 标题栏 | DWM 浅蓝标题栏 `#CDE4FF` + 深蓝灰标题文字，Win11 圆角窗口与同色细边框 |
| 提示条 | 极浅粉玻璃风险提示，不再是深红/暗灰块 |
| 日志窗口 | 深色控制台改为浅色玻璃 + 深蓝文字，解决深底深字看不清 |

统一了全部控件的玻璃样式：**按钮**（渐变 + 上半部反光 + hover 光泽 + 按压缩放）、**输入框 / 下拉框 / 复选框**、**选项卡**（白色胶囊选中态 + 蓝色指示条）、**进度条**（圆角渐变 + 呼吸动画）、**滚动条**（细窄浅蓝）、**列表 / 提示气泡**、**DataGrid 表头与行**、**玻璃卡片 / 统计小卡**。主窗口的磨砂工具栏、状态栏、加载遮罩磨砂卡片，以及启动窗口、确认框、设备选择框、输入框、日志窗口全部同步换肤。

> **设计取舍**：早期版本尝试让窗口背景交给 DWM Mica / Acrylic 系统材质，但部分环境下材质未绘制会露出黑底，且透明窗口会让 WPF 关闭 ClearType 导致字体发虚。r15 改为**不透明底 + WPF 自绘玻璃**，视觉上是 Aero 风格，显示效果在所有 Windows 版本都稳定一致。

### 🔤 字体清晰度修复

| 原因 | 修复 |
| --- | --- |
| 透明窗口背景下 WPF 改用灰度抗锯齿，文字发虚、带重影 | 窗口统一使用不透明浅蓝底，恢复 ClearType |
| 含文字的卡片 / 按钮 / 弹窗使用 `DropShadowEffect`，整个子树被光栅化 | 移除所有含文字容器上的 `Effect`，层次感改用高光描边与渐变表现；模糊只保留在不含文字的背景光斑上 |
| `TextFormattingMode="Ideal"` 小字偏细 | 全局改为 `Display` + `ClearType` + `SnapsToDevicePixels`，像素对齐 |

### 🐛 关键修复

1. **搜索框文字被上下截断**：外层固定 32px 高，全局 `TextBox` 的 `Padding="10,6"` 又占掉 12px，文字视口不足行高。已把搜索框设为 `Padding="0"`、容器加高到 34px，并降低全局输入框/下拉框垂直内边距，避免其它固定高度输入框出现同类裁切。

2. **亮屏时间默认值**：文本框默认值由 30 改为 **5 分钟**，并加 `FallbackValue=5` 兜底；说明文案改为「默认 5 分钟，可设置 5 ~ 600 分钟」，不再把「上限约 30 分钟」误读成默认值。

3. **开发者快捷开关勾选后不生效**（指针位置 / 显示触摸操作 / 严格模式等）：
   - 根因是 XAML 的 `CheckBox` **只绑定了 `IsChecked`，从未绑定 `ToggleQuickCommand`** —— 勾选只改界面布尔值，ADB 命令一次都没下发。
   - 已补 `Command` + `CommandParameter` 绑定，并按「Command 触发时 `IsChecked` 已是新值」的时序重写写入逻辑（原代码按点击前状态取反，会把开关写反）；取消确认、只读项、写入失败均回滚勾选状态。
   - 状态栏显示实际写入的 `space/key=value`；`stay_on_while_plugged_in` 的位掩码不再误判为关闭。

4. **状态栏图标精简 / 网速指示器设置后无效**：
   - 根因是原先写入 `global/icon_nfc` 等**自造键**。`settings put` 允许创建任意键且回读一致，工具因此误报「✓ 已生效」，但 SystemUI 根本不读这些键。
   - 改为 AOSP 标准的 `secure/icon_blacklist`（先读现有值、增删词条再写回，**保留系统默认隐藏的其它图标**），新增 `PutIconBlacklistAsync` 处理清空时的空参数。
   - 通过**真机读取 + 前后截屏对照**，确认荣耀 MagicOS 实际使用的键与词条，逐项修正映射；每项支持写入多个厂商私有键。
   - 「读取设备信息」会回读黑名单与主键，把复选框同步为设备真实状态；「还原」清掉本工具管理的词条而非逐项写无效键；状态提示不再假报「已生效」，改为显示实际黑名单内容与私有键写入数量。

#### 状态栏项 → 实际设置键映射（荣耀 MagicOS 实测）

| 界面项 | `icon_blacklist` 词条 | 厂商私有键 |
| --- | --- | --- |
| NFC 图标 | `nfc` | `global/icon_nfc` |
| 定位图标 | `location` | `global/icon_location` |
| 省电模式图标 | `powersavingmode` | `global/icon_battery_saver` |
| 闹钟图标 | `alarm_clock` | `global/icon_alarm_clock` |
| HD 高清通话 | `volte` | `global/icon_hd` |
| 电池百分比 | — | `system/battery_percent_switch`、`system/battery_percent_switch_in` |
| 护眼模式图标 | `eyes_protect` | `global/icon_eyecare` |
| 网速指示器 | — | **`system/show_network_speed_enabled`**（主）、`system/show_network_speed`（兼容） |
| 双卡信号图标 | — | `global/icon_signal_2` |

真机验证要点：

| 实测项 | 结果 |
| --- | --- |
| `system/show_network_speed` | 已为 `0`，但状态栏仍显示 `25.4 K/s` → **无效键** |
| `system/show_network_speed_enabled` | 写 `0` 后截屏对比，状态栏网速**完全消失** → 真正开关 |
| `system/status_bar_show_battery_percent` | `null`（设备上不存在该键）→ 改用 `battery_percent_switch` |
| `secure/icon_blacklist` | 含 `powersavingmode`、`eyes_protect`、`hires`、`network_sharing` 等厂商词条 → 确认 SystemUI 读取该键 |

### 📋 主要改动文件

| 文件 | 改动 |
| --- | --- |
| `Services/AeroEffects.cs` | 新增：窗口级不透明浅蓝底、ClearType/Display 文本渲染、DWM 圆角与浅蓝标题栏 |
| `Views/Styles.xaml` | 重写：浅蓝玻璃调色板 + 全部控件隐式样式，移除含文字容器的 Effect |
| `Views/MainWindow.xaml` | 玻璃背景光斑、磨砂工具栏/状态栏、加载遮罩、`Display` 文本模式、搜索框内边距 |
| 各对话框 / `ToolboxView` / `SystemTweaksView` | 统一玻璃背景、卡片与描边 |
| `ViewModels/SystemTweaksViewModel.cs` | 默认值 5、快捷开关命令时序、状态栏黑名单读写与多私有键、回读同步 |
| `Services/SystemSettingsService.cs` | 新增 `PutIconBlacklistAsync` |
| `App.xaml.cs` | 注册全局窗口外观处理 |

---

## 🧠 内置中文知识库（1139 条）

| 分类 | 条数 | 覆盖 |
| --- | --- | --- |
| 荣耀 hihonor | 351 | MagicOS 8/9/10/11 系统组件 + 智慧服务 |
| 华为遗留 huawei | 296 | 双品牌过渡机型（Magic5 Pro 等实测） |
| 荣耀旧前缀 honor | 68 | 旧版系统兼容 |
| AOSP 系统 + Google + 高通 | 171 | 系统框架 / APEX / GMS / 高通底层 |
| 第三方及其他 | 253 | 国内主流 + 国际 Top100 |

- **真机实测**：荣耀 Magic5 Pro（MagicOS 11）412 个包 100% 匹配显示
- 支持编辑 `Data\HonorPackageDb.json` 或界面【导入外部知识库】合并（同键覆盖）

---

## ✨ 功能总览

### 📦 应用包管理
- **全量包读取**：`pm list packages` 四路合并 + `dumpsys` 4 线程并发解析，400+ 包约 30 秒
- **三类包分级**：第三方应用（可卸载）/ 系统可停用包（`disable-user`）/ 核心系统包（禁止操作，按钮灰化）
- **停用 / 还原 / 卸载 / APK 备份**（`adb pull` 到 `Backup\{设备序列号}\`）
- **搜索 / 筛选 / 排序**：包名/名称/描述实时过滤 + 类型筛选 + 列头排序

### 🧰 快速工具箱
- 分类卡片批量操作 + 「还原本会话所有停用」，全部双重确认

### ⚙️ 快捷设置
- 动画缩放、亮屏时间（默认 5 分钟）、刷新率范围、状态栏图标精简、开发者快捷开关
- 全部 `settings` 命令写入 + 回读校验 + 操作前确认 + 设备上线自动读取当前值

### 🛠️ 工具能力
- 实时 ADB 命令日志（stdout/stderr，可导出 TXT）、无线 ADB（地址记忆 + 启动页引导）、多设备选择
- 高 DPI（PerMonitorV2）、自包含免安装、ADB 内置

---

## ⌨️ ADB 命令映射

| 界面操作 | 实际命令 | 可否恢复 |
| --- | --- | --- |
| 卸载（第三方） | `pm uninstall -k --user 0 <包名>` | 需原 APK 重装 |
| 停用（系统包） | `pm disable-user --user 0 <包名>` | ✅ `pm enable` |
| 还原启用 | `pm enable <包名>` | — |
| 备份 APK | `adb pull <apk路径>` | — |
| 动画缩放 | `settings put global <三项动画 scale> <值>` | ✅ 改回 1 |
| 亮屏时间 | `settings put system screen_off_timeout <毫秒>` | ✅ 本页或系统设置 |
| 刷新率范围 | `settings put system min_refresh_rate / peak_refresh_rate` | ✅ 本页或系统设置 |
| 状态栏图标 | `settings put secure icon_blacklist <逗号分隔词条>` + 厂商私有键 | ✅ 「还原（默认显示）」 |
| 实时网速 | `settings put system show_network_speed_enabled 0/1` | ✅ 勾选框或系统设置 |
| 开发者开关 | `settings put system/global pointer_location / show_touches / strict_mode / stay_on_while_plugged_in 0/1` | ✅ 取消勾选 |

荣耀新版 MagicOS 的 SPC 系统保护会拒绝部分系统包的停用请求，工具如实反馈失败原因，**不做任何绕过**。

---

## ⚠️ 已知限制

- 部分深度定制 ROM 的 SystemUI 会忽略 `icon_blacklist` 词条；此时工具会如实提示「已写入但状态栏无变化，该 ROM 未响应」，而不是假报成功。状态栏相关项仍可在手机「设置 → 显示和亮度 / 通知和状态栏」中手动操作。
- 电池百分比、护眼、双卡信号等纯厂商项依赖私有键，不同 MagicOS 版本键名可能不同；每个复选框的 ToolTip 会标明该项实际走哪个键。
- 刷新率等项目可能被 MagicOS 智能调度接管，写入后回读不一致即判定失败。
- 本次界面采用 WPF 自绘玻璃，不依赖系统透明效果/主题，在高对比度、远程桌面等环境下表现一致。

---

## 🚀 快速开始

1. 解压发行包到任意目录（免安装、无需 .NET）
2. 手机开启 USB 调试并连接，允许 RSA 授权；或点「无线 ADB 模式」按指引连接
3. 双击 `HonorAdbPackageManager.exe` → 【重新检测】→ 【进入主界面】
4. 【拉取全部应用包】→ 在「应用管理」或「快速工具箱」中操作，按提示双重确认
5. 需要调节系统体验时，切换到「动画调节」/「功能」选项卡

> 💡 推荐流程：**先备份 APK → 停用观察 1-2 天 → 无异常保持**。停用可随时一键还原。

---

## 🛠️ 从源码构建

```bash
git clone https://github.com/HaoDaYiGuoFan/HonorAdbPackageManager.git
cd HonorAdbPackageManager
dotnet publish HonorAdbPackageManager/HonorAdbPackageManager.csproj \
  -c Release -r win-x64 --self-contained true -o publish
```

发行包组装：以 `publish` 输出为基础，补充 `adb\`、`Assets\`、`Data\HonorPackageDb.json`、`zh-Hans\`、`README.md` 与 `Cache\icons`、`Backup`、`Logs` 空目录，排除 `.pdb`，将 `Data\settings.json` 重置为空配置后压缩。

技术栈：.NET 10 / WPF / MVVM / 全异步 ADB / PerMonitorV2 高 DPI / DWM 窗口外观互操作

---

## ❓ FAQ

**Q：字体还有点糊 / 有重影？**
r15 已恢复 ClearType 并移除文字容器上的阴影效果。若仍异常，请确认未被远程桌面或第三方字体渲染工具强制灰度抗锯齿。

**Q：状态栏图标点「执行」后状态栏没变化？**
先在「功能」页点【读取设备信息】看复选框是否被同步回来；若写入成功但图标不变，说明该 ROM 的 SystemUI 未响应 `icon_blacklist`，属系统侧限制，请在手机系统设置中操作。

**Q：网速指示器怎么关？**
取消勾选「网速指示器」→【执行（应用勾选）】，工具写入 `system/show_network_speed_enabled 0`，该键已在荣耀真机上通过前后截屏验证可立即隐藏实时网速。

**Q：开发者快捷开关勾选后没反应？**
r15 已修复命令漏绑。勾选/取消会弹出确认框，确认后写入并回读校验，右侧状态会显示实际写入的键值；取消确认会自动回滚勾选。

**Q：亮屏时间能设多少？**
默认 5 分钟，可设置 5 ~ 600 分钟，按 `分钟 × 60000ms` 写入 `system/screen_off_timeout`。

**Q：停用失败提示 Permission Denial？**
荣耀 SPC 系统保护，属正常限制，工具不绕过。

**Q：修改重启后会丢吗？**
不会。写入的是系统 settings 数据库，重启保留；可随时在本页改回，或在手机系统设置中恢复。

**Q：支持其他品牌手机吗？**
支持任何开启 USB 调试的安卓设备；非荣耀/华为品牌的中文说明与状态栏私有键覆盖有限。

---

## 📄 免责声明

本工具仅供个人学习与设备管理使用，免费、无广告、无追踪；不含 ROOT、解锁、绕过系统限制功能；操作系统应用前请自行备份数据，因使用本工具导致的数据丢失或设备异常由使用者自行承担。

---

**上一版本**：`HonorAdbPackageManager-v1.4.0-r14.zip` · SHA256 `1F4E1B7AA8C05BCB5882E6ED292370979B492CEA4AF07249643602C80386BAA6`

**Full Changelog**: https://github.com/HaoDaYiGuoFan/HonorAdbPackageManager/commits/master
