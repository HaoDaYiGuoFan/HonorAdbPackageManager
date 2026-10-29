# HonorAdbPackageManager - 荣耀 ADB 应用包管理工具

基于 .NET 10 + WPF 的荣耀 MagicOS 应用包管理工具（免 ROOT）。

## 使用方法

1. 双击 HonorAdbPackageManager.exe 启动
2. 手机开启 开发者选项 → USB 调试，插 USB 线
3. 手机弹出『允许 USB 调试』点允许，点【重新检测】
4. 点【进入主界面】→ 自动拉取全部应用包

## 功能

- 读取全部应用包（第三方 / 系统 / 已停用），自动匹配内置知识库（109 个荣耀/安卓包说明）
- 系统包：停用（pm disable-user）/ 还原（pm enable）
- 第三方包：卸载（pm uninstall -k --user 0）
- 核心包：禁止操作（按钮灰化）
- APK 备份（adb pull 到 Backup/序列号/）
- 无线 ADB 连接
- 实时命令日志（可导出 TXT）
- 搜索 + 类型筛选

## 目录说明

- adb/        ADB 组件（adb.exe 等），缺失时程序会提示
- Data/       知识库 JSON（可自行编辑后重启，或界面导入覆盖）
- Cache/      图标缓存（可随时清空）
- Backup/     APK 备份存放
- Logs/       导出的日志文件

## 注意

- 非 ROOT 工具：系统包仅停用不删除，卸载系统分区文件不可行
- 荣耀新版 MagicOS 的 SPC 保护会拒绝部分包的 disable，属系统限制
- 禁用核心包（电话/桌面/相机等）可能导致设备异常，工具已默认拦截