# HonorAdbPackageManager v1.4.0-r17

**免 ROOT 管理荣耀 MagicOS 手机的应用包** —— 读取全部应用包名，自动匹配内置中文知识库（名称/功能/风险），系统包一键停用还原、第三方包卸载、核心组件智能拦截、APK 一键备份；内置「快速工具箱」批量操作页与「动画调节 / 功能」快捷设置页。

> 本次为 **无线 ADB 配对闭环 + 全局 120Hz 锁定（真机验证）+ 弹窗风格统一 + 日志全面升级 + 知识库扩容至 1178 条** 的修订版（r16 → r17）。

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet)](#) [![WPF](https://img.shields.io/badge/UI-WPF%20%7C%20MVVM-blue)](#) [![Platform](https://img.shields.io/badge/OS-Windows%2010%2F11%20x64-success)](#) [![ADB](https://img.shields.io/badge/ADB-platform--tools%2037.0.1-orange)](#) [![Knowledge](https://img.shields.io/badge/知识库-1178%20条-informational)](#)

> ⚠️ 本工具为 ADB 调试辅助，**非 ROOT 工具**。系统包仅停用不删除；操作前建议备份手机数据。

---

## 📥 下载

| 文件 | 说明 |
| --- | --- |
| `HonorAdbPackageManager-v1.4.0-r17.zip` (71.77 MB) | 自包含发行包：内置 .NET 10 运行时 + ADB 37.0.1 + 知识库 1178 条，**解压即用，目标机无需安装任何依赖** |

- **大小**：75,350,212 字节（71.85 MB），269 个条目
- **SHA256**：`45ADF0DE29A47069C7D062099E41D66BAE3EDD5B48458C8C7E50E6EA7A4CEA55`

```powershell
Get-FileHash .\HonorAdbPackageManager-v1.4.0-r17.zip -Algorithm SHA256
```

系统要求：Windows 10 (1709+) / Windows 11 x64。

> 发行包内 `Data\settings.json` 已重置为空配置（不含任何本机无线 ADB 地址），`Logs\` 为空目录。

---

## 🔄 本次更新（r16 → r17）

### 📶 无线 ADB：配对闭环 + 智能诊断

Android 11+ 无线调试基于 TLS，**连接端口只接受已配对客户端**——电脑从未配对（或密钥被 C 盘清理工具误删）时必然 `failed to connect`。r17 补齐完整链路：

| 能力 | 说明 |
| --- | --- |
| `adb pair` 配对流程 | 配对弹窗：输入手机「无线调试 → 使用配对码配对设备」显示的一次性端口与 6 位码，配对成功**自动重连**；失败可原地重试（提示区分端口填错 / 码超时 / 弹窗已关） |
| 连接失败智能诊断 | 先探测 TCP 可达性，区分三种场景给出可操作提示：网络不可达（查 Wi-Fi / AP 隔离 / 端口）→ TCP 通但握手被拒 + 本机无密钥 → **引导配对** → 密钥失效 / 端口变更 → 指引重配 |
| 密钥检测 | 检测 `%USERPROFILE%\.adbkey` 是否存在（「前几天还能连、今天连不上」的常见根因） |

**一次配对长期有效**，之后 connect 自动认证。主界面与启动页均已接入同一套链路。

### ⚡ 全局锁定 120Hz（荣耀真机双向验证）

「功能」页刷新率区块新增一键项，这是在荣耀 Magic5 Pro（MagicOS 11）上双向实测有效的方案：

- **锁定全局 120Hz**：写荣耀档位键 `secure/hw_screen_freq=1` + `hw_screen_high_freq=120`，叠加 AOSP `min/peak_refresh_rate=120` 双保险 → **锁屏再点亮一次生效**（硬件显示模式切换在屏幕重建时应用）
- **恢复智能调度**：`hw_screen_freq=0` + 删除 AOSP 键 → 同样锁屏点亮生效
- 效果：所有应用全局 120Hz（桌面、滚动、动画全程），耗电相应增加；游戏可能受游戏工具箱独立帧率策略影响

> 为什么之前的 min/peak 方案「回读一致但无效」：MagicOS 的「标准/高」档位走独立通道（displayengine HAL 在 SurfaceFlinger 层锁帧），AOSP 键单独写不影响实际输出——真机 dumpsys SurfaceFlinger 实测确认，r17 已修正为双键方案。

### 🎨 提示弹窗风格统一

新增 `AppMessageDialog`，`MsgBox.Show` 整体路由（40+ 处调用零修改自动换肤），与「确认停用系统组件」弹窗完全一致：

- Aero 浅蓝渐变底 + 语义色横幅条（成功绿 / 危险红 / 警告橙 / 信息蓝）+ 圆角说明块 + 右下按钮组
- 语义自动映射：成功/完成→绿，失败/错误→红，确认/警告→橙；支持 OK / YesNo / YesNoCancel

### ⚡ 日志全面升级

| 能力 | 说明 |
| --- | --- |
| 会话日志文件 | **启动即创建** `Logs\log_*_session.txt`，每条日志实时落盘——反馈问题直接发文件，无需复现 |
| 拉包防卡顿 | 单条日志截断（dumpsys 大输出）+ 250ms 批量刷新 + 显示框上限裁剪，拉 400+ 包日志窗口不再「未响应」；导出 TXT 仍完整 |
| 检测过程全量记录 | adb 路径、`adb devices` 输出、每台设备状态、失败原因全部进日志 |
| 启动页日志入口 | 新增【查看日志】按钮 |

### 🖥️ 界面与交互优化

- **加载期收纳**：拉包时中央全屏遮罩取消，进度条收纳至**底部状态栏**实时显示，界面全程可操作
- **Tab 分级可用**：加载期间「快速工具箱 / 动画调节 / 功能」三页正常使用（可先调 120Hz、动画、状态栏），「应用管理」待解析完成自动放开
- **动画自定义倍数**：三个动画项新增数字输入框（输入即过滤非数字），0~10 合法性校验，倍数 >5 弹「慢动作」过大提醒
- 工具箱条目在包加载中点击会提示「加载中请稍候」而非静默无反应

### 📚 知识库扩容：1139 → 1178 条

- **真机差集校验**：Magic5 Pro（MagicOS 11 / Android 17）全量 433 包（含隐藏 APEX 组件）对比知识库，**零「未收录」**
- 新增 39 条：Magic6/7 系新机预置（手机克隆 / 教育桌面 / 打印助手 / 荣耀亲选 / 荣耀阅读）、系统底层（android 框架 / APEX 扩展 / 高通 IMS·IWLAN·cacert，按 dumpsys flags 标注）、huawei→hihonor 映射缺口（服务框架 / OTA / WebView / 权限控制器等 12 条）、常见第三方（银行 / 个税 / B 站 / 迅雷 / Legado / GKD / Link to Windows 等）
- 修正 `com.larus.nova`（原「某音海外」）为「豆包」

### 🔧 主要改动文件

| 文件 | 改动 |
| --- | --- |
| `Services/AdbService.cs` | `PairAsync` / `HasAdbKey` / `DiagnoseConnectFailure`；devices 检测开启日志 |
| `Services/SystemSettingsService.cs` | 新增 `DeleteAsync`（settings delete + 回读验证） |
| `Views/PairingDialog.xaml(.cs)` | 新增：配对码配对弹窗 |
| `Views/AppMessageDialog.xaml(.cs)` | 新增：统一风格消息弹窗 |
| `Views/MsgBox.cs` | 重写为路由到 AppMessageDialog |
| `Services/LogService.cs` | 会话日志文件 + 单条截断 |
| `Views/LogWindow.xaml.cs` | 批量刷新 + 缓冲/显示上限 |
| `Views/devicepickerwindow.xaml(.cs)` | 诊断 + 配对链路；【查看日志】按钮 |
| `ViewModels/MainViewModel.cs` | 加载期 Tab 分级、进度收纳状态栏 |
| `ViewModels/SystemTweaksViewModel.cs` | Lock120 / RestoreRefresh（真机验证方案）、自定义动画倍数 |
| `ViewModels/ToolboxViewModel.cs` | 加载期点击友好提示 |
| `Views/MainWindow.xaml` | 状态栏进度、Tab 分级、移除全屏遮罩 |
| `Data/HonorPackageDb.json` | 1178 条 |

---

## ⬇️ 升级

- **从 r16**：直接解压覆盖。首次无线连接若提示配对，按引导完成一次即可；`Data\settings.json` / 自定义知识库如需保留从旧目录拷回。
- **从 r15 及更早**：同上，另可体验统一弹窗、启动页日志入口、1178 条知识库零未收录。

---

## 🚀 快速开始

1. 解压到任意目录（免安装、无需 .NET）
2. 手机开启 USB 调试并连接、允许 RSA 授权；或「无线 ADB 模式」按指引配对
3. 双击 `HonorAdbPackageManager.exe` → 【重新检测】→ 【进入主界面】
4. 自动拉取应用包（底部状态栏显示进度，期间可先调「动画调节 / 功能」页）
5. 加载完成后在「应用管理」或「快速工具箱」操作，双重确认

> 💡 推荐流程：**先备份 APK → 停用观察 1-2 天 → 无异常保持**。停用可随时一键还原。

---

## ❓ FAQ

**Q：无线 ADB 提示「TCP 端口可达，但 ADB 握手被设备拒绝」？**
本机从未配对或密钥丢失（C 盘清理工具误删 `%USERPROFILE%\.adbkey` 是常见原因）。按弹窗引导完成配对即可；建议把 `.adbkey` / `.adbkey.pub` 加入清理工具白名单。

**Q：配对端口和连接端口的区别？**
连接端口在「无线调试」主页面（开关切换/重启会变）；配对端口在「使用配对码配对设备」弹窗里，**每次打开都不同**，两者不要混用。

**Q：锁定全局 120Hz 后为什么手机设置页还显示「标准」？**
荣耀设置页的档位高亮读的是它自己的状态，工具写入后 UI 可能不刷新——以工具状态栏显示的写入结果为准，实际渲染已是 120Hz（可用开发者选项帧率浮窗验证）。想设置页对齐可手动点一下「高」档。

**Q：USB/无线都检测不到设备，怎么反馈？**
启动页点【查看日志】→【导出 TXT】发给作者；`Logs\` 下最新 `*_session.txt` 同内容。日志含 adb 路径、命令输出与失败原因。

**Q：停用失败提示 Permission Denial？**
荣耀 SPC 系统保护，属正常限制，工具不绕过。

**Q：动画倍数设多大合适？**
0 = 关闭动画（省电、极速）；1 = 系统默认；1.5 = 推荐微快；>5 会是明显慢动作，工具会弹确认。

其余 FAQ 见 README.md。

---

## ⚠️ 免责声明

本工具仅供个人学习与设备管理使用，免费、无广告、无追踪；不含 ROOT、解锁、绕过系统限制功能；操作系统应用前请自行备份数据，因使用本工具导致的数据丢失或设备异常由使用者自行承担。

---

**上一版本**：`HonorAdbPackageManager-v1.4.0-r16.zip` · SHA256 `5A905FB72CBB4226C0E11209980B5854C11D62460944FFDE83F5B4386547D4F8`

**Full Changelog**: https://github.com/HaoDaYiGuoFan/HonorAdbPackageManager/commits/master
