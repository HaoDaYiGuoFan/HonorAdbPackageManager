# HonorAdbPackageManager v1.4.0-r17

**免 ROOT 管理荣耀 MagicOS 手机的应用包** —— 读取全部应用包名，自动匹配内置中文知识库（名称/功能/风险），系统包一键停用还原、第三方包卸载、核心组件智能拦截、APK 一键备份；内置「快速工具箱」批量操作页与「动画调节 / 功能」快捷设置页。

> 本次为 **无线 ADB 配对闭环 + 弹窗风格统一 + 日志全面升级 + 知识库扩容** 的修订版（r16 → r17）：无线 ADB 补齐 Android 11+ 必需的「配对码配对」流程并内置连接失败智能诊断；全部提示弹窗统一为与确认弹窗一致的 Aero 风格；日志防卡顿并新增启动即写的会话日志文件（问题反馈必备）；知识库扩容至 1178 条（覆盖 Magic5/6/7 系真机 433 包零未收录）。

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet)](#) [![WPF](https://img.shields.io/badge/UI-WPF%20%7C%20MVVM-blue)](#) [![Platform](https://img.shields.io/badge/OS-Windows%2010%2F11%20x64-success)](#) [![ADB](https://img.shields.io/badge/ADB-platform--tools%2037.0.1-orange)](#) [![Knowledge](https://img.shields.io/badge/知识库-1178%20条-informational)](#)

> ⚠️ 本工具为 ADB 调试辅助，**非 ROOT 工具**。系统包仅停用不删除；操作前建议备份手机数据。

---

## 📥 下载

| 文件 | 说明 |
| --- | --- |
| `HonorAdbPackageManager-v1.4.0-r17.zip` (71.13 MB) | 自包含发行包：内置 .NET 10 运行时 + ADB 37.0.1 + 知识库 1178 条，**解压即用，目标机无需安装任何依赖** |

- **大小**：74589965 字节（71.13 MB），269 个条目
- **SHA256**：`7C49B78C3BE80AA2119911DFD442F7E8A8117C0B48ACDDF78CFB79EFAA73AD61`

```powershell
Get-FileHash .\HonorAdbPackageManager-v1.4.0-r17.zip -Algorithm SHA256
```

系统要求：Windows 10 (1709+) / Windows 11 x64。

> 发行包内 `Data\settings.json` 已重置为空配置，不含任何本机无线 ADB 地址；`Logs\` 为空目录。

---

## 🔄 本次更新（r16 → r17）

### 📶 无线 ADB：配对闭环 + 智能诊断

此前版本只有 `adb connect`，而 Android 11+ 无线调试基于 TLS，**连接端口只接受已配对客户端**——电脑从未配对（或密钥丢失）时必然 `failed to connect`，且旧提示「检查 IP/端口/同一网络」指向了错误方向。r17 补齐完整链路：

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

### ⚡ 日志全面升级

| 层 | 修复 |
| --- | --- |
| 会话日志文件 | **启动即创建** `Logs\log_*_session.txt`，每条日志实时落盘（线程安全）；反馈问题直接发文件即可，无需复现 |
| 拉包防卡顿 | 单条日志截断 4000 字符（dumpsys 大输出）+ 250ms 批量合并刷新 + 显示框 40 万字符裁剪——拉取 400+ 包时日志窗口不再「未响应」；导出 TXT 仍含完整内容 |
| 检测过程全量记录 | adb 路径、`adb devices -l` 命令与原始输出、每台设备状态、失败常见原因全部进日志 |
| 启动页日志入口 | 启动页新增【查看日志】按钮，一键打开日志窗口导出 |

### ⚡ 新增「全局锁定 120Hz」快捷设置

「功能」页屏幕刷新率区块下新增一键项：**锁定全局 120Hz**（min_refresh_rate = peak_refresh_rate = 120，弹确认后写入并回读验证，写入成功自动同步上方范围输入框为 120/120）与**恢复智能调度**（settings delete 两个键，回到 90/120 智能均衡）。配套 SystemSettingsService.DeleteAsync（settings delete + 回读确认删除成功）。

### 📚 知识库扩容：1139 → 1178 条

- **真机差集校验**：Magic5 Pro（MagicOS 11 / Android 17）真机全量 433 包（含隐藏 APEX 组件）对比知识库，**hihonor 系 164 包 100% 覆盖、全部 433 包零「未收录」**
- 新增 39 条：Magic6/7 系新机预置（手机克隆 / 教育桌面 / 打印助手 / 荣耀亲选 / 荣耀阅读）、系统底层（android 框架 / APEX 扩展 / 高通 IMS·IWLAN·cacert，均按 dumpsys flags 标注 PERSISTENT / 禁止操作）、huawei→hihonor 映射缺口（服务框架 / OTA 老路径 / WebView / 权限控制器等 12 条）、常见第三方（银行 / 个税 / B 站 / 迅雷 / Legado / GKD / Link to Windows 等 15 条）
- 修正 `com.larus.nova`（原「某音海外/Larus Nova」）为「豆包」

### 🔧 主要改动文件

| 文件 | 改动 |
| --- | --- |
| `Services/AdbService.cs` | 新增 `PairAsync` / `HasAdbKey` / `DiagnoseConnectFailure`；devices 检测开启命令日志 |
| `Views/PairingDialog.xaml(.cs)` | 新增：配对码配对弹窗（地址 + 6 位码，格式校验） |
| `Views/AppMessageDialog.xaml(.cs)` | 新增：统一风格消息弹窗（语义横幅 / OK / YesNo / YesNoCancel） |
| `Views/MsgBox.cs` | 重写为路由到 AppMessageDialog，签名不变 |
| `Views/devicepickerwindow.xaml(.cs)` | 接入诊断 + 配对重连链路；新增【查看日志】按钮；检测过程记录 |
| `ViewModels/MainViewModel.cs` | 主界面接入同一套诊断 + 配对链路 |
| `Services/LogService.cs` | 会话日志文件（启动即写、实时追加）+ 单条截断 |
| `Views/LogWindow.xaml.cs` | 250ms 批量刷新 + 缓冲/显示上限 |
| `Data/HonorPackageDb.json` | 1178 条（+39 新机型/系统组件/第三方，豆包修正） |

---

## ⬇️ 从 r16 升级

直接解压覆盖，或删除旧目录后解压新版。`Data\settings.json`（无线地址记忆）与 `Data\HonorPackageDb.json`（自定义知识库）如需保留，从旧目录拷回。

> 升级后首次使用无线 ADB 时，若提示需要配对，按工具引导完成一次配对即可（手机端：无线调试 → 使用配对码配对设备）。

## ⬇️ 从 r15 及更早升级

除上述外，重点体验：无线 ADB 配对闭环、统一弹窗、启动页【查看日志】、知识库 1178 条零未收录。

---

## ✨ 功能总览

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

### 🔌 连接与可观测性
- USB 多设备选择；无线 ADB：地址记忆 + **配对码配对闭环** + 失败智能诊断
- 会话日志全程落盘 + 启动页日志入口；实时命令日志（可导出 TXT）
- 高 DPI（PerMonitorV2）、自包含免安装、ADB 内置

---

## 🚀 快速开始

1. 解压发行包到任意目录（免安装、无需 .NET）
2. 手机开启 USB 调试并连接，允许 RSA 授权；或点「无线 ADB 模式」按指引配对连接
3. 双击 `HonorAdbPackageManager.exe` → 【重新检测】→ 【进入主界面】
4. 【拉取全部应用包】→ 在「应用管理」或「快速工具箱」中操作，按提示双重确认
5. 需要调节系统体验时，切换到「动画调节」/「功能」选项卡

> 💡 推荐流程：**先备份 APK → 停用观察 1-2 天 → 无异常保持**。停用可随时一键还原。

---

## ❓ FAQ

**Q：无线 ADB 提示「TCP 端口可达，但 ADB 握手被设备拒绝」？**
本机从未配对或密钥丢失（常见于 C 盘清理工具误删 `%USERPROFILE%\.adbkey`）。点工具弹窗中的「现在进行配对」，按手机「无线调试 → 使用配对码配对设备」弹窗的一次性端口与 6 位码完成配对即可；建议将 `.adbkey` / `.adbkey.pub` 加入清理工具白名单防止复发。

**Q：配对端口和连接端口有什么区别？**
连接端口显示在「无线调试」主页面（如 37453），开关切换或重启后会变化；配对端口显示在「使用配对码配对设备」弹窗中，**每次打开弹窗都会变化**，且与连接端口不同。两者不要混用。

**Q：USB/无线都检测不到设备，怎么反馈？**
启动页点【查看日志】→【导出 TXT】，把文件发给作者；`Logs\` 目录下最新的 `*_session.txt` 也是同一份内容。日志里有 adb 路径、命令输出与失败原因，能直接定位卡在哪一步。

**Q：停用失败提示 Permission Denial？**
荣耀 SPC 系统保护，属正常限制，工具不绕过。

**Q：修改重启后会丢吗？**
不会。写入的是系统 settings 数据库，重启保留；可随时改回或在手机系统设置中恢复。

其余 FAQ 见 README.md。

---

## ⚠️ 免责声明

本工具仅供个人学习与设备管理使用，免费、无广告、无追踪；不含 ROOT、解锁、绕过系统限制功能；操作系统应用前请自行备份数据，因使用本工具导致的数据丢失或设备异常由使用者自行承担。

---

**上一版本**：`HonorAdbPackageManager-v1.4.0-r16.zip` · SHA256 `5A905FB72CBB4226C0E11209980B5854C11D62460944FFDE83F5B4386547D4F8`

**Full Changelog**: https://github.com/HaoDaYiGuoFan/HonorAdbPackageManager/commits/master
