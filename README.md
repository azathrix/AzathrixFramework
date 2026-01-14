<p align="center">
  <img src="Documentation~/icon.png" alt="Azathrix Framework Logo" width="96">
</p>

<h1 align="center">Azathrix Framework</h1>

<p align="center">
  Unity 模块化游戏框架，提供系统管理、事件分发、依赖注入等核心功能
</p>

<p align="center">
  <a href="https://azathrixdev.github.io/com.azathrix.framework/"><img src="https://img.shields.io/badge/文档-官网-blue.svg" alt="Docs"></a>
  <a href="https://github.com/AzathrixDev/com.azathrix.framework"><img src="https://img.shields.io/badge/GitHub-Azathrix-black.svg" alt="GitHub"></a>
  <a href="https://www.npmjs.com/package/com.azathrix.framework"><img src="https://img.shields.io/npm/v/com.azathrix.framework.svg" alt="npm"></a>
  <a href="#license"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
  <a href="https://unity.com/"><img src="https://img.shields.io/badge/Unity-6000.3+-black.svg" alt="Unity"></a>
</p>

<p align="center">
  <a href="https://azathrixdev.github.io/com.azathrix.framework/">📖 官网文档</a> •
  <a href="https://azathrixdev.github.io/com.azathrix.framework/guide/">📚 教程</a> •
  <a href="https://azathrixdev.github.io/com.azathrix.framework/api/">📋 API 参考</a>
</p>

---

## 特性

- 🏗️ **模块化系统架构** - 基于接口的系统设计，支持自动扫描和注册
- 💉 **依赖注入** - 支持强依赖 `[Inject]` 和弱依赖 `[WeakInject]`
- 📡 **类型安全事件系统** - 支持优先级、一次性事件、拦截器、生命周期绑定
- 🚀 **可扩展启动管线** - 阶段化启动流程，支持自定义阶段和钩子
- 📝 **高级日志系统** - 多级别日志、平台配置、颜色样式、标签过滤
- 🔧 **完整编辑器支持** - 系统监控、注册表管理、设置面板
- ⚙️ **注册表系统** - 可视化管理系统启用/禁用、优先级、接口实现选择

## 安装

### 方式一：Package Manager 添加 Scope（推荐）

1. 打开 `Edit > Project Settings > Package Manager`
2. 在 `Scoped Registries` 中添加：
   - **Name**: `Azathrix`
   - **URL**: `https://registry.npmjs.org`
   - **Scope(s)**: `com.azathrix`
3. 点击 `Save`
4. 打开 `Window > Package Manager`
5. 切换到 `My Registries`
6. 找到 `Azathrix Framework` 并安装

### 方式二：Git URL

1. 打开 `Window > Package Manager`
2. 点击 `+` > `Add package from git URL...`
3. 输入：`https://github.com/AzathrixDev/com.azathrix.framework.git#latest`

> ⚠️ Git 方式无法自动解析依赖，需要先手动安装：
> - [UniTask](https://github.com/Cysharp/UniTask)

### 方式三：npm 命令

在项目的 `Packages` 目录下执行：

```bash
npm install com.azathrix.framework
```

## 快速开始

### 1. 创建系统

```csharp
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Cysharp.Threading.Tasks;

[SystemPriority(100)]
public class PlayerSystem : ISystem, ISystemInitialize, ISystemUpdate
{
    [Inject] private InputSystem _input;      // 必须存在
    [WeakInject] private AudioSystem _audio;  // 可选依赖

    public async UniTask OnInitializeAsync()
    {
        // 异步初始化逻辑
    }

    public void OnUpdate()
    {
        // 每帧更新
    }
}
```

### 2. 获取系统

```csharp
using Azathrix.Framework.Core;

var player = AzathrixFramework.GetSystem<PlayerSystem>();

// 检查系统是否存在
if (AzathrixFramework.HasSystem<AudioSystem>())
{
    var audio = AzathrixFramework.GetSystem<AudioSystem>();
}

// 手动注入依赖到任意对象
AzathrixFramework.InjectTo(myObject);
```

### 3. 事件系统

```csharp
using Azathrix.Framework.Core;
using Azathrix.Framework.Events.Interfaces;

// 定义事件（推荐使用 struct 避免 GC）
public struct PlayerDiedEvent : IEventDefine
{
    public int PlayerId;
    public string Reason;
}

// 注册事件监听
AzathrixFramework.Dispatcher.Register<PlayerDiedEvent>(evt =>
{
    Debug.Log($"玩家 {evt.PlayerId} 死亡: {evt.Reason}");
})
.Priority(100)           // 设置优先级（数值越大越先执行）
.Once()                  // 一次性事件
.AddTo(gameObject);      // 绑定生命周期

// 发送事件
AzathrixFramework.Dispatcher.Send(new PlayerDiedEvent
{
    PlayerId = 1,
    Reason = "坠落"
});

// 使用初始化器发送（支持 struct）
AzathrixFramework.Dispatcher.Send<PlayerDiedEvent>(ref evt =>
{
    evt.PlayerId = 1;
    evt.Reason = "坠落";
});

// 发送默认事件（无参数）
AzathrixFramework.Dispatcher.SendDefault<GameStartEvent>();
```

### 4. 事件拦截器

```csharp
// 添加拦截器（可修改或阻止事件）
AzathrixFramework.Dispatcher.AddInterceptor<PlayerDiedEvent>(
    (ref EventSendPackage package) =>
    {
        var evt = (PlayerDiedEvent)package.eventData;
        if (evt.PlayerId == 0)
            return InterceptorStateEnum.Return; // 阻止事件
        return InterceptorStateEnum.Next;       // 继续传递
    },
    name: "死亡验证",
    priority: 100
);
```

### 5. 日志系统

```csharp
using Azathrix.Framework.Tools;

// 基础日志
Log.Info("信息日志");
Log.Warning("警告日志");
Log.Error("错误日志");
Log.Verbose("详细日志");

// 带颜色样式（0-7 对应不同颜色）
Log.Info("彩色日志", colorStyle: 2);

// 带标签
Log.InfoWithTag("Network", "连接成功");

// 分隔线
Log.Separator("模块初始化");

// 输出集合
Log.LogCollection("玩家列表", playerList);
Log.LogDict("配置", configDict);
```

## 系统属性

| 属性 | 说明 |
|------|------|
| `[Inject]` | 依赖注入（必须存在，否则报错） |
| `[WeakInject]` | 弱依赖注入（可为空） |
| `[RequireSystem(typeof(...))]` | 声明系统依赖顺序 |
| `[SystemPriority(n)]` | 系统优先级（越小越先注册） |
| `[SystemAlias("name")]` | 系统别名 |
| `[UpdateInterval(ms)]` | Update 调用间隔（毫秒） |
| `[Default]` | 标记为接口的默认实现 |

## 生命周期接口

| 接口 | 说明 |
|------|------|
| `ISystemRegister` | 注册时调用 |
| `ISystemInitialize` | 异步初始化 |
| `ISystemEnabled` | 启用/禁用回调 |
| `ISystemUpdate` | 每帧更新 |
| `ISystemFixedUpdate` | 固定时间步更新 |
| `ISystemLateUpdate` | 延迟更新 |
| `ISystemApplicationPause` | 应用暂停/恢复 |
| `ISystemApplicationFocusChanged` | 焦点变化 |
| `ISystemApplicationQuit` | 应用退出 |
| `ISystemEditorSupport` | 编辑器模式支持 |

## 启动管线

框架使用阶段化管线启动，支持自定义阶段和钩子：

### 内置阶段

| 阶段 | Order | 说明 |
|------|-------|------|
| `IResourceLoadPhase` | 0 | 资源加载 |
| `IAssemblyLoadPhase` | 100 | 程序集加载（HybridCLR） |
| `ISetupPhase` | 200 | 框架配置 |
| `IScanPhase` | 300 | 系统扫描 |
| `IRegisterPhase` | 400 | 系统注册 |
| `IStartPhase` | 500 | 启动完成 |

### 自定义阶段

```csharp
[PhaseOrder(150)] // 在 Setup 之前执行
public class MyPhase : ISetupPhase
{
    public async UniTask ExecuteAsync(PhaseContext context)
    {
        // 自定义逻辑
        context.Set("myData", someValue); // 存储数据供后续阶段使用
    }
}
```

### 阶段钩子

```csharp
// 在扫描阶段之前执行
public class MyBeforeHook : IBeforePhaseHook<IScanPhase>
{
    public int Order => 0;

    public async UniTask<HookResult> OnBeforeAsync(PhaseContext context)
    {
        // HookResult.Continue - 继续执行
        // HookResult.SkipPhase - 跳过当前阶段
        // HookResult.Abort - 中断整个管线
        return HookResult.Continue;
    }
}

// 在注册阶段之后执行
public class MyAfterHook : IAfterPhaseHook<IRegisterPhase>
{
    public int Order => 0;

    public async UniTask OnAfterAsync(PhaseContext context)
    {
        // 注册完成后的处理
    }
}
```

### 常见用例

```csharp
// 热更新：在程序集加载阶段之前
public class HotUpdateHook : IBeforePhaseHook<IAssemblyLoadPhase>
{
    public int Order => 0;

    public async UniTask<HookResult> OnBeforeAsync(PhaseContext context)
    {
        await CheckAndDownloadUpdate();
        return HookResult.Continue;
    }
}

// 自定义资源加载器
public class CustomLoaderHook : IBeforePhaseHook<ISetupPhase>
{
    public int Order => 0;

    public async UniTask<HookResult> OnBeforeAsync(PhaseContext context)
    {
        context.ResourcesLoader = new MyResourcesLoader();
        return HookResult.Continue;
    }
}

// 加载首场景
public class LoadSceneHook : IAfterPhaseHook<IStartPhase>
{
    public int Order => 0;

    public async UniTask OnAfterAsync(PhaseContext context)
    {
        await SceneManager.LoadSceneAsync("MainMenu");
    }
}
```

## 日志配置

在 `Project Settings > Azathrix > Log` 中配置：

| 配置项 | 说明 |
|--------|------|
| globalLogLevel | 全局日志级别（Verbose/Info/Warning/Error/None） |
| enableStackTrace | 是否启用堆栈跟踪 |
| enableColors | 是否启用颜色 |
| maxCollectionElements | 集合日志最大显示元素数 |
| platformConfigs | 平台特定配置 |
| infoColors | 信息日志颜色列表（8种） |
| customTags | 自定义标签配置 |

## 编辑器工具

| 菜单路径 | 说明 |
|----------|------|
| `Azathrix > Settings` | 框架设置面板 |
| `Azathrix > System Registry` | 系统注册表管理 |
| `Azathrix > Phase Registry` | 阶段注册表管理 |
| `Azathrix > Hook Registry` | 钩子注册表管理 |
| `Azathrix > System Monitor` | 运行时系统监控 |

### 系统注册表功能

- 启用/禁用系统
- 调整系统优先级
- 查看系统依赖关系
- 选择接口的默认实现

## 框架配置

在 `Project Settings > Azathrix > 框架设置` 中配置：

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| projectId | 项目ID | NewGame |
| versionFormat | 版本格式 | {major}.{minor}.{patch} |
| scanMode | 扫描模式（All/Specified） | All |
| assemblyNames | 指定扫描的程序集 | - |
| excludeAssemblyPrefixes | 排除的程序集前缀 | System, Microsoft, Unity... |
| autoInitialize | 自动初始化框架 | true |
| enableProfiling | 启用性能统计 | false |
| systemInfoLevel | 系统信息输出级别 | Simple |

## API 参考

### AzathrixFramework

| 属性/方法 | 说明 |
|-----------|------|
| `GetSystem<T>()` | 获取系统实例 |
| `HasSystem<T>()` | 检查系统是否存在 |
| `InjectTo(object)` | 手动注入依赖 |
| `Dispatcher` | 事件分发器 |
| `Logger` | 日志记录器 |
| `ResourcesLoader` | 资源加载器 |
| `StartupAsync()` | 手动启动框架 |
| `Pause()` / `Resume()` | 暂停/恢复系统更新 |

### EventDispatcher

| 方法 | 说明 |
|------|------|
| `Register<T>(handler)` | 注册事件监听 |
| `Send<T>(evt)` | 发送事件 |
| `SendDefault<T>()` | 发送默认事件 |
| `AddInterceptor<T>(func)` | 添加拦截器 |
| `UnRegister(id)` | 注销事件 |

### EventResult（链式调用）

| 方法 | 说明 |
|------|------|
| `.Priority(n)` | 设置优先级 |
| `.Once()` | 设为一次性事件 |
| `.InvokeNow()` | 立即调用一次 |
| `.AddTo(gameObject)` | 绑定到 GameObject 生命周期 |
| `.AddTo(collector)` | 添加到事件收集器 |
| `.Destroy()` | 销毁/注销事件 |

## 依赖

| 依赖 | 版本 | 说明 |
|------|------|------|
| com.azathrix.unitask | 2.5.10+ | 异步任务库（自动安装） |

## 架构

```
AzathrixFramework (静态入口)
├── StartupPipeline (启动管线)
│   ├── IStartupPhase (阶段接口)
│   └── IStartupHook (钩子接口)
├── SystemRuntimeManager (系统运行时管理)
│   ├── ISystem (系统接口)
│   └── ISystemEvent (生命周期接口)
├── EventDispatcher (事件分发器)
│   ├── IEventDefine (事件定义)
│   └── EventInterceptor (拦截器)
├── Registry (注册表系统)
│   ├── SystemRegistry
│   ├── PhaseRegistry
│   └── StartupHookRegistry
└── Settings (配置系统)
    ├── AzathrixFrameworkSettings
    └── LogSettings
```

## License

MIT License

Copyright (c) 2024 Azathrix
