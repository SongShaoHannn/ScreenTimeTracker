# ⏱ 屏幕时间管家 (ScreenTimeTracker)

> 一款 Apple 风格设计的 Windows 桌面应用，自动追踪每个软件的使用时长，支持自定义限额与超时提醒。

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet" alt=".NET 9.0">
  <img src="https://img.shields.io/badge/WPF-Windows-blue?logo=windows" alt="WPF">
  <img src="https://img.shields.io/badge/SQLite-ORM-green?logo=sqlite" alt="SQLite">
  <img src="https://img.shields.io/badge/license-MIT-orange" alt="License MIT">
</p>

---

## ✨ 功能特点

| 功能 | 说明 |
|------|------|
| 🔍 **自动追踪** | 1 秒轮询检测前台窗口，自动记录每个应用的使用时间 |
| 📊 **数据可视化** | 仪表盘展示今日使用时长、热门应用排行、最近 7 天趋势图 |
| ⏰ **时间限额** | 为任意应用设置每日使用时长上限 |
| 🔔 **超时提醒** | 达到 80% 警告、100% 限额时弹出 Windows Toast 通知 |
| 📋 **使用记录** | 查看每个应用每日的详细使用片段 |
| 📤 **数据导出** | 支持导出 CSV / JSON 格式的使用历史 |
| 🖥 **系统托盘** | 关闭窗口自动隐藏到托盘，后台静默记录 |
| 🚀 **开机自启** | 支持注册表 Run 项管理 |
| 🎨 **Apple 设计风格** | 毛玻璃面板、圆角卡片、柔和阴影、SF 风格字体 |

---

## 📸 界面预览

```
┌─────────────────────────────────────────────┐
│ ⏱ 屏幕时间                                   │
│                                             │
│  📊 仪表盘                                    │
│  ⚙️ 设置                                      │
│                                             │
│          今日活动                             │
│  ┌──────┐ ┌──────┐ ┌──────┐                │
│  │4h 32m│ │VS Code│ │8 / 25│                │
│  │总时间 │ │最常用 │ │应用数 │                │
│  └──────┘ └──────┘ └──────┘                │
│                                             │
│  今日热门应用                                │
│  📱 VS Code         2h 15m  [████████░░] 78%│
│  📱 Chrome          1h 05m  [████░░░░░░] 35%│
│  📱 WeChat          0h 52m  [███░░░░░░░] 20%│
│                                             │
│  最近 7 天                                   │
│  ▓ ▓ ▓ ▓ ▓ ▓ ▓                              │
│  一 二 三 四 五 六 日                        │
└─────────────────────────────────────────────┘
```

---

## 🛠 技术栈

| 类别 | 技术 |
|------|------|
| **框架** | .NET 9.0 + WPF |
| **架构** | MVVM（CommunityToolkit.Mvvm） |
| **数据库** | SQLite + Entity Framework Core |
| **依赖注入** | Microsoft.Extensions.DependencyInjection |
| **系统托盘** | Hardcodet.NotifyIcon.Wpf |
| **前台窗口追踪** | P/Invoke user32.dll（GetForegroundWindow） |

---

## 📁 项目结构

```
ScreenTimeTracker/
├── Models/              # 数据模型（TrackedApp, UsageSession, TimeLimit）
├── Data/                # EF Core DbContext
├── Services/
│   ├── Abstractions/    # 服务接口
│   └── Implementation/  # 核心服务实现
│       ├── WindowTrackerService.cs   # 前台窗口追踪（核心）
│       ├── DataService.cs            # 数据库 CRUD
│       ├── LimitCheckService.cs      # 限额检查引擎
│       ├── NotificationService.cs    # Toast 通知
│       └── SystemTrayService.cs      # 系统托盘管理
├── ViewModels/          # MVVM ViewModel 层
├── Views/
│   ├── Pages/           # 仪表盘、应用详情、设置页面
│   ├── Controls/        # 可复用控件（AppCard, SidebarNav等）
│   └── Converters/      # 值转换器
├── Themes/              # Apple 风格主题（颜色、字体、阴影）
└── Helpers/             # P/Invoke、进程名映射、图标提取
```

---

## 🚀 快速开始

### 环境要求
- Windows 10/11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### 运行

```bash
# 克隆仓库
git clone https://github.com/SongShaoHannn/ScreenTimeTracker.git

# 进入项目
cd ScreenTimeTracker/src/ScreenTimeTracker

# 运行
dotnet run
```

### 发布（生成独立 .exe）

```bash
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true -o bin/publish
```

---

## 🏗 架构设计

```
[Timer 1s] → WindowTrackerService
                ↓
         P/Invoke user32.dll
         GetForegroundWindow()
                ↓
         检测前台窗口变化
                ↓
         ┌──────┴──────┐
    结束上一个会话    开始新会话
         ↓              ↓
    DataService ──→ SQLite 数据库

[Timer 60s] → LimitCheckService
                ↓
         查询当日累计使用时间
                ↓
         对比时间限额配置
                ↓
       ┌────┴────┐
    超限提醒    80%预警
       ↓         ↓
   NotificationService
   (Windows Toast)
```

---

## 🧠 核心实现

### 前台窗口追踪

```csharp
// 通过 P/Invoke 每 1 秒检测当前前台窗口
var hwnd = NativeMethods.GetForegroundWindow();
NativeMethods.GetWindowText(hwnd, sb, 256);
NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
var processName = Process.GetProcessById((int)pid).ProcessName;
```

### 智能过滤
- 自动过滤系统进程（svchost、RuntimeBroker 等）
- 过滤无窗口标题的后台进程
- 过滤自身进程，避免追踪记录干扰
- 内置 100+ 常见应用友好名称映射

---

## 📝 开发笔记

本项目由 **Claude Code** 辅助开发，从零到完成经历了：

| 阶段 | 内容 |
|------|------|
| 需求分析 | 确定技术栈（WPF vs Electron vs WebView2） |
| 架构设计 | MVVM + DI + EF Core |
| 代码生成 | 95% 代码由 AI 生成 |
| 调试修复 | 循环依赖、XAML 资源加载、Page vs UserControl |
| 中文本地化 | 全界面中文化 |

> 一个大学生 + AI 编程助手 = 完整桌面应用。这是 2025-2026 年最值得掌握的开发方式。

---

## 📄 License

MIT © [SongShaoHannn](https://github.com/SongShaoHannn)

---

⭐ 如果这个项目对你有帮助，请点个 Star！
