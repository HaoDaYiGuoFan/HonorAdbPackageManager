# HonorAdbPackageManager v1.4.0-r16

**免 ROOT 管理荣耀 MagicOS 手机的应用包** —— 读取全部应用包名，自动匹配内置中文知识库（名称/功能/风险），系统包一键停用还原、第三方包卸载、核心组件智能拦截、APK 一键备份；内置「快速工具箱」批量操作页与「动画调节 / 功能」快捷设置页。

> 本次为 **无线 ADB 配对闭环 + 弹窗风格统一 + 日志性能修复** 的修订版（r15 → r16）：无线 ADB 补齐 Android 11+ 必需的「配对码配对」流程并内置连接失败智能诊断；全部提示弹窗统一为与确认弹窗一致的 Aero 风格；修复拉取应用包时日志窗口卡死（未响应）的问题。

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet)](#) [![WPF](https://img.shields.io/badge/UI-WPF%20%7C%20MVVM-blue)](#) [![Platform](https://img.shields.io/badge/OS-Windows%2010%2F11%20x64-success)](#) [![ADB](https://img.shields.io/badge/ADB-platform--tools%2037.0.1-orange)](#) [![Knowledge](https://img.shields.io/badge/知识库-1139%20条-informational)](#)

> ⚠️ 本工具为 ADB 调试辅助，**非 ROOT 工具**。系统包仅停用不删除；操作前建议备份手机数据。

---

## 📥 下载

| 文件 | 说明 |
| --- | --- |
| `HonorAdbPackageManager-v1.4.0-r16.zip` (67.25 MB) | 自包含发行包：内置 .NET 10 运行时 + ADB 37.0.1 + 知识库 1178 条，**解压即用，目标机无需安装任何依赖** |

- **大小**：70519587 字节（67.25 MB），253 个条目
- **SHA256**：`5A905FB72CBB4226C0E11209980B5854C11D62460944FFDE83F5B4386547D4F8`

```powershell
Get-FileHash .\HonorAdbPackageManager-v1.4.0-r16.zip -Algorithm SHA256
```

系统要求：Windows 10 (1709+) / Windows 11 x64。

> 发行包内 `Data\settings.json` 已重置为空配置，不含任何本机无线 ADB 地址。

---

## 🔄 本次更新（r15 → r16）

### 📶 无线 ADB：配对闭环 + 智能诊断

此前版本只有 `adb connect`，而 Android 11+ 无线调试基于 TLS，**连接端口只接受已配对客户端**——电脑从未配对（或密钥丢失）时必然 `failed to connect`，且旧提示「检查 IP/端口/同一网络」指向了错误方向。r16 补齐完整链路：

| 新增 | 说明 |
| --- | --- |
| `adb pair` 配对流程 | 新增 `PairAsync` 与配对弹窗：输入手机「无线调试 → 使用配对码配对设备」弹窗显示的一次性端口与 6 位码，配对成功后**自动重连**原地址；配对失败可原地重试（提示区分端口填错 / 码超时 / 弹窗已关） |
| 连接失败智能诊断 | `DiagnoseConnectFailure` 先探测 TCP 可达性，再区分三种场景给出可操作提示：① 网络不可达（查 Wi-Fi / AP 隔离 / 端口）；② TCP 通但握手被拒 + 本机无密钥 → **引导配对**；③ 密钥失效 / 端口变更 → 指引重新配对或确认新端口 |
| 密钥检测 | 检测 `%USERPROFILE%\.adbkey` 是否存在（C 盘清理工具误删密钥是「前几天还能连、今天连不上」的常见根因） |

主界面「连接无线 ADB」与启动页「无线 ADB 模式」均已接入同一套诊断 + 配对链路。**一次配对长期有效**，之后 connect 自动认证。

### 🎨 提示弹窗风格统一

新增 `AppMessageDialog`，`MsgBox.Show` 整体路由到新弹窗（40+ 处调用零修改自动换肤），视觉与「确认停用系统组件」弹窗完全一致：

- Aero 浅蓝渐变底 + 语义色横幅条（成功绿 / 危险红 / 警告橙 / 信息蓝）+ 圆形色底图标 + 圆角说明块 + 右下按钮组
- 语义色按标题自动映射：「成功 / 完成」→ 绿，「失败 / 错误 / 拦截」→ 红，「确认 / 警告」→ 橙，其余 → 蓝
- 支持 OK / YesNo / YesNoCancel，主按钮跟随语义色（错误类用 DangerButton）
- 最终确认、停用成功、备份完成、配对失败等所有提示弹窗观感统一

### ⚡ 日志窗口卡死修复

拉取 400+ 包时每个包的 `dumpsys` 输出（几十 KB）全部进日志，每条触发一次 `AppendText + ScrollToEnd`，上万次 UI 追加把 UI 线程打满，日志窗口白屏「未响应」。三层修复：

| 层 | 修复 |
| --- | --- |
| 源头截断（LogService） | 单条日志超 4000 字符自动截断并标注「已截断 N 字符」 |
| 批量刷新（LogWindow） | 日志先进缓冲，250ms 定时器合并一次追加；缓冲超 20 万字符丢弃最旧部分 |
| 显示上限 | 文本框超 40 万字符裁掉最旧一半；**导出 TXT 仍含完整内容** |

### 📚 其他

- README 全面更新：10 张新版界面截图（多设备选择 / 拉包进度 / 新版弹窗 / 动画调节 / 功能设置等），新增「动画调节与系统设置」章节与无线配对说明
- 程序集版本号修正为 1.4.0

### 🔧 主要改动文件

| 文件 | 改动 |
| --- | --- |
| `Services/AdbService.cs` | 新增 `PairAsync` / `HasAdbKey` / `DiagnoseConnectFailure` |
| `Views/PairingDialog.xaml(.cs)` | 新增：配对码配对弹窗（地址 + 6 位码，格式校验） |
| `Views/AppMessageDialog.xaml(.cs)` | 新增：统一风格消息弹窗（语义横幅 / OK / YesNo / YesNoCancel） |
| `Views/MsgBox.cs` | 重写为路由到 AppMessageDialog，签名不变 |
| `Views/devicepickerwindow.xaml.cs` / `ViewModels/MainViewModel.cs` | 接入诊断 + 配对重连链路（`PairAndReconnectAsync`） |
| `Services/LogService.cs` | 单条日志截断 |
| `Views/LogWindow.xaml.cs` | 批量刷新 + 缓冲/显示上限 |
| `README.md` | 截图与章节更新 |

---

## ⬇️ 从 r15 升级

直接解压覆盖，或删除旧目录后解压新版。`Data\settings.json`（无线地址记忆）与 `Data\HonorPackageDb.json`（自定义知识库）如需保留，从旧目录拷回。

> 升级后首次使用无线 ADB 时，若提示需要配对，按工具引导完成一次配对即可（手机端：无线调试 → 使用配对码配对设备）。

---

## ✨ 功能总览（自 r15 继承）

### 📱 应用包管理
- **全量包读取**：`pm list packages` 四路合并 + `dumpsys` 4 线程并发解析，400+ 包约 30 秒
- **三类包分级**：第三方应用（可卸载）/ 系统可停用包（`disable-user`）/ 核心系统包（禁止操作，按钮灰化）
- **停用 / 还原 / 卸载 / APK 备份**（`adb pull` 到 `Backup\{设备序列号}\`）
- **搜索 / 筛选 / 排序**：包名/名称/描述实时过滤 + 类型筛选 + 列头排序

### 🧰 快速工具箱
- 分类卡片批量操作 +「一键停用冗余服务」+「还原本会话所有停用」，全部双重确认

### ⚙️ 快捷设置
- 动画缩放、亮屏时间（默认 5 分钟）、刷新率范围、状态栏图标精简、开发者快捷开关
- 全部 `settings` 命令写入 + 回读校验 + 操作前确认 + 设备上线自动读取当前值

### 🔌 连接能力
- USB 多设备选择；无线 ADB：地址记忆 + **配对码配对闭环** + 失败智能诊断
- 实时 ADB 命令日志（可导出 TXT）、高 DPI（PerMonitorV2）、自包含免安装、ADB 内置

---

## 🚀 快速开始

1. 解压发行包到任意目录（免安装、无需 .NET）
2. 手机开启 USB 调试并连接，允许 RSA 授权；或点「无线 ADB 模式」按指引配对连接
3. 双击 `HonorAdbPackageManager.exe` → 【重新检测】→ 【进入主界面】
4. 【拉取全部应用包】→ 在「应用管理」或「快速工具箱」中操作，按提示双重确认
5. 需要调节系统体验时，切换到「动画调节」/「功能」选项卡

> 💡 推荐流程：**先备份 APK → 停用观察 1-2 天 → 无异常保持**。停用可随时一键还原。

---

## ❓ FAQ（新增两条）

**Q：无线 ADB 提示「TCP 端口可达，但 ADB 握手被设备拒绝」？**
本机从未配对或密钥丢失（常见于 C 盘清理工具误删 `%USERPROFILE%\.adbkey`）。点工具弹窗中的「现在进行配对」，按手机「无线调试 → 使用配对码配对设备」弹窗的一次性端口与 6 位码完成配对即可；建议将 `.adbkey` / `.adbkey.pub` 加入清理工具白名单防止复发。

**Q：配对端口和连接端口有什么区别？**
连接端口显示在「无线调试」主页面（如 37453），开关切换或重启后会变化；配对端口显示在「使用配对码配对设备」弹窗中，**每次打开弹窗都会变化**，且与连接端口不同。两者不要混用。

其余 FAQ（SPC 保护、停用还原、亮屏时间、状态栏图标不生效等）见 README.md。

---

## ⚠️ 免责声明

本工具仅供个人学习与设备管理使用，免费、无广告、无追踪；不含 ROOT、解锁、绕过系统限制功能；操作系统应用前请自行备份数据，因使用本工具导致的数据丢失或设备异常由使用者自行承担。

---

**上一版本**：`HonorAdbPackageManager-v1.4.0-r15.zip` · SHA256 `942EA1523A67BA0B4C577F8565671716D7D71661D0A0BF0E10503CD038FBBB5B`

**Full Changelog**: https://github.com/HaoDaYiGuoFan/HonorAdbPackageManager/commits/master