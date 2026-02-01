# MacroRecorder - 鼠标键盘行为录制器

一个用于Windows平台的鼠标键盘行为录制和回放工具。

## 功能特性

- 🎥 **录制功能** - 记录鼠标移动、点击、滚动和键盘输入
- ▶️ **回放功能** - 支持单次播放、次数循环和无限有限循环
- 📋 **列表管理** - 查看、重命名和删除录制内容
- 🌙 **深色模式** - 自动适配系统深色主题
- 🖼️ **自定义图标** - 使用icon.png作为应用图标

## 系统要求

- Windows 10 或更高版本
- .NET 8.0 Runtime

## 安装方法

### 方法一：直接运行

1. 下载发布版本
2. 解压到任意目录
3. 运行 `MacroRecorder.exe`

### 方法二：从源码编译

```bash
# 克隆仓库
git clone https://your-repo-url/MacroRecorder.git
cd MacroRecorder

# 编译并发布
cd MacroRecorder
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained false

# 发布文件位于 publish 目录
```

## 使用说明

1. 点击"开始录制"按钮开始录制操作
2. 执行想要录制的鼠标和键盘操作
3. 点击"停止录制"结束录制
4. 在左侧列表中选择录制的项目
5. 设置播放次数（1次或无限循环）
6. 点击"播放"按钮回放操作
7. 右键点击项目可进行重命名或删除

## 项目结构

```
MacroRecorder/
├── icon.png                    # 应用图标
├── MacroRecorder.sln          # 解决方案文件
└── MacroRecorder/             # 主项目
    ├── App.xaml              # 应用入口
    ├── MainWindow.xaml       # 主窗口界面
    ├── Models/               # 数据模型
    ├── Services/             # 服务层（钩子、录制、播放）
    ├── ViewModels/           # 视图模型
    └── Themes/               # 深色主题资源
```

## 技术栈

- C# 10
- .NET 8.0 Windows
- WPF (Windows Presentation Foundation)
- Windows API Hooks

## 许可证

MIT License
