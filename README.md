# Button-Open-File-Explorer

DIAVision DMV-IVS 运行模式插件（DiaControl Plugin）—— 一个按钮 + 自定义路径输入框，点击即可在 Windows 资源管理器中打开指定文件夹。

## 功能

- 输入框支持手动输入或粘贴文件夹路径
- 点击"打开路径"按钮，调用系统资源管理器打开
- 空路径 / 路径不存在时弹窗提示

## 项目结构

```
├── RunModePathOpener.csproj    # .NET 8 项目文件
├── RunModePathOpener.json      # DIAVision 插件清单
├── RunModePathOpener.xaml      # Avalonia 界面布局
├── RunModePathOpener.xaml.cs   # 界面逻辑
├── nuget.config                # NuGet 本地包源配置
└── Learning.MD                 # DIAVision 插件开发学习笔记
```

## 环境要求

- .NET 8 SDK
- DIAVision DMV-IVS（含 `references` 目录中的框架 DLL）

## 编译

```bash
dotnet build
```

## 部署

将以下文件复制到 DIAVision 插件目录：

```
目标路径：C:\Users\Public\Documents\DMV-IVS\Plugin\RunModePathOpener\

- RunModePathOpener.dll
- RunModePathOpener.json
```

重启 DIAVision 即可在运行模式中加载。

## 文档

详见 [Learning.MD](./Learning.MD) —— DIAVision 编辑模式与运行模式插件开发通用笔记。
