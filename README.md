# MacroRecorder - 鼠标键盘行为录制器

一个用于Windows平台的鼠标键盘行为录制和回放工具。

## 功能特性

- 🎥 **录制功能** - 记录鼠标移动、点击、滚动和键盘输入
- ▶️ **回放功能** - 支持单次播放、次数循环（1-999次）
- 📋 **列表管理** - 查看、重命名和删除录制内容
- 💾 **持久化存储** - 录制数据保存在本地JSON文件
- 🌙 **深色模式** - 自动适配系统深色主题
- ⚙️ **纯净实现** - 纯C#实现，无需额外依赖

## 系统要求

- Windows 10 或更高版本
- .NET 8.0 Runtime

## 安装方法

### 简单版本（推荐）

1. 进入 `MacroRecorder` 目录
2. 双击 `启动宏录制器.bat` 或直接运行 `python macro_recorder.py`

### C# 版本

```bash
# 克隆仓库
git clone https://github.com/cxcboss/MacroRecorder_win.git
cd MacroRecorder_win

# 编译并发布
cd MacroRecorder
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained false -o ../publish_simple

# 发布文件位于 publish_simple 目录
# 运行 SimpleRecorder.exe
```

## 使用说明

### Python 版本
1. 点击"开始录制"按钮开始录制操作
2. 执行想要录制的鼠标和键盘操作
3. 点击"停止录制"结束录制
4. 在列表中选择录制的项目
5. 点击"播放"按钮回放操作

### C# 版本（SimpleRecorder）
1. 点击"开始录制"按钮开始录制操作
2. 执行想要录制的鼠标和键盘操作
3. 点击"停止录制"结束录制
4. 在列表中选择录制的项目
5. 设置播放次数（1-999次）
6. 点击"播放"按钮回放操作
7. 可对录制项目进行重命名或删除

## 项目结构

```
MacroRecorder/
├── icon.png                    # 应用图标
├── README.md                   # 说明文档
├── MacroRecorder.sln           # 解决方案文件
├── 启动宏录制器.bat             # Python版本启动脚本
├── requirements.txt            # Python依赖
├── macro_recorder.py           # Python版本主程序
├── MacroRecorder/              # C#主项目
│   ├── SimpleRecorder.cs       # 纯C#简化版实现
│   ├── SimpleRecorder.csproj   # 项目文件
│   └── publish_simple/         # 发布目录
└── Recordings/                 # 录制文件存储目录（运行时生成）
```

## 技术栈

### Python 版本
- Python 3.8+
- pynput（鼠标键盘钩子）
- tkinter（界面）
- JSON（数据存储）

### C# 版本
- C# 10
- .NET 8.0 Windows
- Windows Forms
- Windows API Hooks（全局鼠标键盘钩子）

## 许可证

MIT License
