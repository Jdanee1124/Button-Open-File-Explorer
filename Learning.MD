# DIAVision 插件开发学习笔记

> 通俗讲解 DIAVision DMV-IVS 插件开发全过程，涵盖 **编辑模式 (FlowElement)** 与 **运行模式 (DiaControl)** 两种插件类型。

---

## 1. DIAVision 有哪两种插件？

| 类型 | 用途 | 基类 | 出现时机 |
|------|------|------|----------|
| **FlowElement** | 流程图里的"处理节点"（如 Blob、找圆） | `FlowElement` | 编辑模式 |
| **DiaControl** | 运行界面上的自定义控件（如状态灯、控制面板） | `DiaControlPlugin` | 运行模式 |

**通俗理解：**
- FlowElement = 流水线上的一个"工位"，负责干活（图像处理、算法等）。
- DiaControl = 车间大屏上的一个"小部件"，负责展示和交互。

本项目做的是 **DiaControl 插件**。

---

## 2. 插件长什么样？—— 文件结构

```
RunModePathOpener/
├── RunModePathOpener.csproj   # 项目文件（告诉 .NET 怎么编译）
├── RunModePathOpener.json     # 插件清单（告诉 DIAVision 怎么加载）
├── RunModePathOpener.xaml     # 界面布局（Avalonia XAML）
├── RunModePathOpener.xaml.cs  # 界面逻辑（C# 代码）
└── nuget.config               # NuGet 配置（指向框架 DLL 的本地源）
```

**通俗理解：**
- `.json` 相当于"身份证"——告诉主程序"我是谁、我的 DLL 在哪"。
- `.xaml` 相当于"图纸"——描述界面长什么样。
- `.xaml.cs` 相当于"操作手册"——描述按钮按下去会发生什么。

---

## 3. 插件清单 JSON —— "身份证"

```json
{
    "typeNames": ["PluginSamples.RunModePathOpener"],
    "assemblyUiFileNames": ["RunModePathOpener.dll"],
    "IsSupportedWinX64": true,
    "IsSupportedLinuxX64": true,
    "IsSupportedLinuxArm64": true
}
```

| 字段 | 含义 |
|------|------|
| `typeNames` | 插件类的完整名称（命名空间 + 类名） |
| `assemblyUiFileNames` | 包含界面的 DLL 文件名 |
| `IsSupported*` | 支持哪些平台 |

**注意：** DiaControl 插件用 `assemblyUiFileNames`（不是 `assemblyFileNames`），因为它只有界面，没有后台算法。

**部署位置：** 编译后的 DLL 和 JSON 要放到
```
C:\Users\Public\Documents\DMV-IVS\Plugin\RunModePathOpener\
```

---

## 4. 项目文件 .csproj —— "编译说明书"

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <!-- Avalonia UI 框架 -->
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.2" />
    <PackageReference Include="Avalonia.Diagnostics" Version="11.3.2" />
    <PackageReference Include="Avalonia.ReactiveUI" Version="11.3.2" />
  </ItemGroup>
  <!-- DIAVision 框架引用 -->
  <ItemGroup>
    <Reference Include="DIAVision.Core">
      <HintPath>...references\DIAVision.Core.dll</HintPath>
      <Private>false</Private>
      <ExcludeAssets>runtime</ExcludeAssets>
    </Reference>
    <!-- DIAVision.UI.Base 和 DIAVision.UI.Unified 同理 -->
  </ItemGroup>
  <!-- Avalonia XAML 资源绑定 -->
  <ItemGroup>
    <Compile Update="**\*.xaml.cs">
      <DependentUpon>%(Filename)</DependentUpon>
    </Compile>
    <AvaloniaResource Include="**\*.xaml">
      <SubType>Designer</SubType>
    </AvaloniaResource>
  </ItemGroup>
</Project>
```

**关键点解释：**

| 配置 | 为什么 |
|------|--------|
| `OutputType: Library` | 插件是 DLL 类库，不是可执行程序 |
| `TargetFramework: net8.0` | DIAVision 基于 .NET 8 |
| `Private: false` | 框架 DLL 不要复制到输出目录（主程序已经自带） |
| `ExcludeAssets: runtime` | 运行时不加载这些引用（避免重复加载） |
| `DependentUpon` | 让 `.xaml.cs` 在 IDE 里折叠到 `.xaml` 下方 |

---

## 5. XAML 界面 —— "图纸"

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="PluginSamples.RunModePathOpener">
  <StackPanel Orientation="Horizontal" Spacing="8">
    <TextBlock Text="路径：" VerticalAlignment="Center"/>
    <TextBox x:Name="TxtPath" Width="360"
             Watermark="输入自定义文件夹路径，例如 C:\Images"/>
    <Button Content="打开路径" Click="OnOpenPath"/>
  </StackPanel>
</UserControl>
```

**通俗理解：**
- 这是一个水平排列的面板：标签 + 输入框 + 按钮。
- `x:Name="TxtPath"` 给输入框取名，代码里通过这个名字找到它。
- `Click="OnOpenPath"` 表示点击按钮时调用 `OnOpenPath` 方法。
- 用的是 [Avalonia](https://avaloniaui.net/) 框架，语法和 WPF 的 XAML 几乎一样。

---

## 6. C# 代码 —— "操作手册"

```csharp
public class RunModePathOpener : DiaControlPlugin  // 继承插件基类
{
    public RunModePathOpener()
    {
        InitializeComponent();  // 加载 XAML 界面
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);  // Avalonia 的 XAML 加载器
    }

    private async void OnOpenPath(object sender, RoutedEventArgs e)
    {
        // 1. 获取输入框内容
        var txtPath = this.FindControl<TextBox>("TxtPath");
        var path = txtPath?.Text?.Trim();

        // 2. 空路径检查
        if (string.IsNullOrEmpty(path)) { 弹窗提示; return; }

        // 3. 路径存在性检查
        if (!Directory.Exists(path)) { 弹窗提示; return; }

        // 4. 用系统资源管理器打开文件夹
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true   // 关键：用系统默认方式打开
        });
    }
}
```

**核心逻辑：**
1. 通过 `FindControl<TextBox>("TxtPath")` 拿到 XAML 中的输入框。
2. 用 `Directory.Exists()` 检查路径是否存在。
3. 用 `Process.Start()` + `UseShellExecute = true` 调用系统资源管理器打开文件夹。

---

## 7. 框架引用的三个 DLL

| DLL | 职责 |
|-----|------|
| `DIAVision.Core` | 核心类型（FlowElement、属性标记等） |
| `DIAVision.UI.Base` | UI 基础（MessageBox、PageEditor 等工具） |
| `DIAVision.UI.Unified` | 统一 UI（DiaControlPlugin 基类、运行模式支持） |

**通俗理解：**
- Core = 地基（所有插件都要用）
- Base = 工具箱（弹窗、编辑器等通用 UI 工具）
- Unified = 运行模式专用功能（本插件的核心依赖）

---

## 8. nuget.config —— "本地包源"

```xml
<configuration>
  <packageSources>
    <add key="local-packages"
         value="C:\Program Files\...\PluginSamples\references" />
  </packageSources>
</configuration>
```

因为 `DIAVision.Core.dll` 等不是 NuGet 上的公开包，而是随软件安装在本地的，所以需要告诉 NuGet 去哪找它们。

---

## 9. 完整开发流程

**DiaControl（运行模式）插件：**
```
1. 创建项目目录
2. 编写 .csproj（引用 DIAVision.Core + UI.Base + UI.Unified + Avalonia）
3. 编写 nuget.config（指向本地 DLL 目录）
4. 编写 .json 插件清单（assemblyUiFileNames）
5. 编写 .xaml 界面布局
6. 编写 .xaml.cs（继承 DiaControlPlugin）
7. dotnet build 编译
8. 将 DLL + JSON 复制到 Plugin 目录
9. 启动 DIAVision 验证
```

**FlowElement（编辑模式）插件（带 UI）：**
```
1. 创建项目目录，建立 sln
2. Core 项目：.csproj（引用 DIAVision.Core）→ 编写 FlowElement 子类
3. UI 项目：.csproj（引用 Core + DIAVision.Core + UI.Base）→ 编写 PageEditor + ViewModel
4. 编写 nuget.config
5. 编写 .json 插件清单（assemblyFileNames + assemblyUiFileNames）
6. dotnet build 编译
7. 将 DLL + JSON 复制到 Plugin 目录
8. 启动 DIAVision 验证
```

---

## 10. 常见坑

| 问题 | 原因 | 解决 |
|------|------|------|
| `找不到 DiaControlPlugin` | 缺少 `using DIAVision.UI.Unified.DiaControl` | 添加 using |
| `找不到 VisualTreeAttachmentEventArgs` | 缺少 `using Avalonia.Controls` | 添加 using |
| `找不到 PageEditor` | 缺少 `using DIAVision.UI.Base` | 添加 using |
| `找不到 FlowElement` | 缺少 `using DIAVision.Core.FlowElements` | 添加 using |
| DLL 引用报错 | HintPath 路径不对 | 用绝对路径或配 nuget.config |
| 插件不显示 | JSON 中 `typeNames` 写错或 DLL 没放到 Plugin 目录 | 检查全限定类名和部署路径 |
| `Tools.MessageBox` 找不到 | 需要用完全限定名 `DIAVision.UI.Base.Tools.MessageBox` | 补全命名空间 |
| UI 项目引用 Core 项目报错 | 忘记加 `ProjectReference` | 在 UI csproj 中添加对 Core csproj 的引用 |
| 编辑界面不显示 | `[UIEditor]` 中的全限定类名写错 | 核对命名空间 + 类名 |
| `[Input]` 属性不生效 | 属性不是 public 或缺少 getter/setter | 确保 `public` 且有读写 |

---

## 11. 编辑模式插件 (FlowElement) —— 通用要点

### 11.1 核心结构

FlowElement 插件有两种形态：

**无 UI 的简单插件：** 一个 .csproj 就够
```
MyPlugin/
├── MyPlugin.csproj     # 引用 DIAVision.Core.dll
├── MyPlugin.json
└── MyPlugin.cs         # 继承 FlowElement
```

**带自定义编辑界面的插件：** Core + UI 两个项目
```
MyPlugin/
├── MyPlugin.sln
├── MyPlugin.json
├── Core/
│   ├── MyPlugin.csproj         # 引用 DIAVision.Core.dll
│   └── MyPlugin.cs             # 继承 FlowElement
└── UI/
    ├── MyPluginUI.csproj       # 引用 DIAVision.Core.dll + DIAVision.UI.Base.dll
    │                           # 并 ProjectReference → Core/MyPlugin.csproj
    ├── MyPluginUI.xaml          # Avalonia UserControl
    ├── MyPluginUI.xaml.cs       # 继承 PageEditor
    └── MyPluginUIViewModel.cs   # 继承 PageEditorViewModel
```

### 11.2 FlowElement 基类

```csharp
[Category("MyCategory")]        // 在菜单中的分组
[Description("这是一个示例插件")] // 鼠标悬停提示
[UIEditor("PluginSamples.MyPluginUI")]          // 绑定编辑界面 View
[UIEditorViewModel("PluginSamples.MyPluginUIViewModel")] // 绑定编辑界面 ViewModel
public class MyPlugin : FlowElement
{
    // 输入参数
    [Input]
    [InitialValue("128")]
    [Range(0, 255, RangeMode.Include)]
    public int GrayThreshold { get; set; }

    // 输出结果
    [Output]
    public int BlobCount { get; set; }

    // 执行前检查
    protected override bool CanExecute()
    {
        return true; // 返回 false 则跳过执行
    }

    // 核心执行逻辑
    protected override bool Execute()
    {
        var threshold = GetInputValue<int>(nameof(GrayThreshold));
        // ... 图像处理逻辑 ...
        BlobCount = result;
        return true; // 返回 false 表示执行失败
    }
}
```

### 11.3 常用属性标记一览

| 标记 | 作用 | 示例 |
|------|------|------|
| `[Input]` | 标记为输入参数（在界面可配置） | `[Input] public int Threshold` |
| `[Output]` | 标记为输出结果 | `[Output] public double Score` |
| `[Linkable]` | 允许输入被其他节点的输出绑定 | `[Linkable][Input] public IMImage Image` |
| `[InitialValue("128")]` | 设置默认值 | 数值、字符串均可 |
| `[Range(0, 255, RangeMode.Include)]` | 限制数值范围 | Include / Exclude 控制端点 |
| `[Category("Name")]` | 类级别：菜单分组 | `[Category("检测")]` |
| `[Category("Name", Order)]` | 属性级别：输入/输出分组显示 | `[Category("参数", 1)]` |
| `[Description("text")]` | 提示信息 | `[Description("灰度阈值")]` |
| `[UIEditor("Namespace.Class")]` | 绑定编辑模式 View | 全限定类名 |
| `[UIEditorViewModel("...")]` | 绑定编辑模式 ViewModel | 全限定类名 |
| `[SupportedROIs(...)]` | 指定支持的 ROI 类型 | `[SupportedROIs(ROIType.Rectangle)]` |
| `[Command]` | 标记方法为可调用命令 | 按钮触发 |

### 11.4 编辑界面 (PageEditor + PageEditorViewModel)

**View — MyPluginUI.xaml.cs：**
```csharp
public class MyPluginUI : PageEditor  // 注意：继承 PageEditor，不是 UserControl
{
    public MyPluginUI()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
```

**ViewModel — MyPluginUIViewModel.cs：**
```csharp
public class MyPluginUIViewModel : PageEditorViewModel
{
    // FlowElement 属性来自基类，指向当前关联的插件实例

    // 读取输入值
    public int Threshold
    {
        get => FlowElement.GetInputValue<int>("GrayThreshold");
    }

    // 修改输入值（通过表达式系统）
    public void SetThreshold(int value)
    {
        InputExpressions["GrayThreshold"].SetExpression(value.ToString());
    }
}
```

**XAML — MyPluginUI.xaml：**
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="PluginSamples.MyPluginUI">
  <StackPanel>
    <TextBlock Text="灰度阈值"/>
    <TextBox Text="{Binding Threshold}"/>
  </StackPanel>
</UserControl>
```

### 11.5 JSON 清单区别

```json
{
    "typeNames": ["PluginSamples.MyPlugin"],
    "assemblyFileNames": ["MyPlugin.dll"],       // ← 注意：用 assemblyFileNames
    "assemblyUiFileNames": ["MyPluginUI.dll"],   // ← UI DLL 单独列出
    "IsSupportedWinX64": true,
    "IsSupportedLinuxX64": true,
    "IsSupportedLinuxArm64": true
}
```

| | DiaControl（运行模式） | FlowElement（编辑模式） |
|---|---|---|
| `assemblyFileNames` | 不需要 | **需要**（核心逻辑 DLL） |
| `assemblyUiFileNames` | 需要 | 需要（如果有自定义 UI） |
| 无 UI 插件 | — | 只写 `assemblyFileNames` 即可 |

### 11.6 两种模式对比总结

| 对比项 | FlowElement（编辑模式） | DiaControl（运行模式） |
|--------|------------------------|----------------------|
| 基类 | `FlowElement` | `DiaControlPlugin` |
| 功能 | 数据处理、算法执行 | 界面展示、操作交互 |
| 生命周期 | `CanExecute()` → `Execute()` | XAML 加载 → 用户交互 |
| 输入输出 | `[Input]` / `[Output]` 属性 | `PlatformLinker` / `ElementLinker` 绑定 |
| UI 基类 | `PageEditor` + `PageEditorViewModel` | 直接写 Avalonia UserControl |
| 项目结构 | 常分 Core + UI 两个项目 | 单项目即可 |
| DLL 字段 | `assemblyFileNames` | `assemblyUiFileNames` |

---

## 12. 参考资源

- 官方样例目录：`C:\Program Files\Delta Industrial Automation\DIAVision\DMV-IVS\Samples\PluginSamples\`
  - `src/FlowElement/Basic/` —— 输入输出、属性、命令等基础用法
  - `src/FlowElement/Template/PluginBase/` —— FlowElement 最小模板（无 UI）
  - `src/FlowElement/Template/PluginBaseWithUI/` —— FlowElement 带 UI 模板
  - `src/FlowElement/Tutorial/` —— 12 步渐进教程
  - `src/FlowElement/CustomUI/` —— 自定义弹窗和面板
  - `src/DiaControl/CustomRunModeView/` —— 运行模式完整示例
  - `src/DiaControl/DiaControlSample/` —— 自定义控件示例（StatusLight）
- Avalonia 文档：https://docs.avaloniaui.net/
