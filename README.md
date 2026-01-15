<p align="center">
  <img src="Documentation~/icon.png" alt="Azathrix Framework Logo" width="96">
</p>

<h1 align="center">Azathrix Framework</h1>

<p align="center">
  Unity 模块化游戏框架，提供系统管理、事件分发、依赖注入等核心功能
</p>

<p align="center">
  <a href="https://azathrix.github.io/AzathrixFramework/"><img src="https://img.shields.io/badge/文档-官网-blue.svg" alt="Docs"></a>
  <a href="https://github.com/azathrix/AzathrixFramework"><img src="https://img.shields.io/badge/GitHub-Azathrix-black.svg" alt="GitHub"></a>
  <a href="https://www.npmjs.com/package/com.azathrix.framework"><img src="https://img.shields.io/npm/v/com.azathrix.framework.svg" alt="npm"></a>
  <a href="#license"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
  <a href="https://unity.com/"><img src="https://img.shields.io/badge/Unity-6000.3+-black.svg" alt="Unity"></a>
</p>

<p align="center">
  <a href="https://azathrix.github.io/AzathrixFramework/">📖 官网文档</a> •
  <a href="https://azathrix.github.io/AzathrixFramework/guide/">📚 教程</a> •
  <a href="https://azathrix.github.io/AzathrixFramework/api/">📋 API 参考</a>
</p>

---

## 特性

- 🏗️ **模块化系统架构** - 基于接口的系统设计，支持自动扫描和注册
- 💉 **依赖注入** - 支持强依赖 `[Inject]` 和弱依赖 `[WeakInject]`
- 📡 **高性能事件系统** - 零 GC、32ns/op、支持优先级、过滤、节流、防抖、拦截器、生命周期绑定
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
3. 输入：`https://github.com/azathrix/AzathrixFramework.git#latest`

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

高性能、零 GC 的类型安全事件系统。

```csharp
using Azathrix.Framework.Events.Core;

// 获取事件分发器
var dispatcher = AzathrixFramework.Dispatcher;

// 定义事件（必须是 struct）
public struct PlayerDiedEvent
{
    public int PlayerId;
    public string Reason;
}

// 订阅事件
dispatcher.Subscribe<PlayerDiedEvent>((ref PlayerDiedEvent e) =>
{
    Debug.Log($"玩家 {e.PlayerId} 死亡: {e.Reason}");
})
.Priority(100)           // 优先级（数值越大越先执行）
.Once()                  // 一次性订阅
.Where((ref PlayerDiedEvent e) => e.PlayerId > 0)  // 过滤条件
.Skip(2)                 // 跳过前2个事件
.Throttle(100)           // 节流（100ms内只处理一次）
.Debounce(200)           // 防抖（200ms静默后处理）
.Delay(50)               // 延迟50ms处理
.Timeout(5000)           // 5秒后自动取消订阅
.AddTo(gameObject);      // 绑定 GameObject 生命周期

// 分发事件
dispatcher.Dispatch(new PlayerDiedEvent { PlayerId = 1, Reason = "坠落" });

// ref 分发（避免复制）
var evt = new PlayerDiedEvent { PlayerId = 1, Reason = "坠落" };
dispatcher.Dispatch(ref evt);

// Sticky 事件（新订阅者立即收到最后一个值）
dispatcher.DispatchSticky(new GameStateEvent { State = "Playing" });
dispatcher.Subscribe<GameStateEvent>(e => { }).Sticky();

// Post 事件（延迟到帧结束处理，线程安全）
dispatcher.Post(new UIRefreshEvent());
dispatcher.Flush();  // 手动刷新（通常自动）

// 带返回值的查询
dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => e.BaseDamage * 2);
int total = dispatcher.Query<DamageCalcEvent, int>(
    new DamageCalcEvent { BaseDamage = 100 },
    (a, b) => a + b  // 聚合函数
);

// 消息事件（字符串 ID，线程安全）
dispatcher.SubscribeMessage<string>("player.name.changed", name => Debug.Log(name));
dispatcher.DispatchMessage("player.name.changed", "NewName");
```

### 4. 事件拦截器

```csharp
using Azathrix.Framework.Events.Interceptors;

// 添加拦截器（可修改或阻止事件）
dispatcher.AddInterceptor<PlayerDiedEvent>(
    (ref InterceptorContext<PlayerDiedEvent> ctx) =>
    {
        if (ctx.Event.PlayerId == 0)
            return InterceptResult.Cancel;  // 阻止事件
        ctx.Event.Reason = "Modified: " + ctx.Event.Reason;  // 修改事件
        return InterceptResult.Continue;    // 继续传递
    },
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
[PhaseId("MyPhase")]
public class MyPhase : ISetupPhase
{
    public int Order => 150; // 在 Setup 之前执行

    public async UniTask ExecuteAsync(LauncherContext context)
    {
        // 自定义逻辑
        context.Set("myData", someValue); // 存储数据供后续阶段使用
    }
}
```

### 阶段钩子

> 注意：`HookTarget` 只能使用“管线 ID + 阶段 ID”进行精准匹配，不支持接口/基类/类型名匹配。  
> 未标注 `PhaseId` 时，阶段 ID 默认使用类名。

```csharp
// 在扫描阶段之前执行
[HookTarget("Launcher", "Scan")]
public class MyBeforeHook : IBeforePhaseHook<LauncherContext>
{
    public int Order => 0;

    public async UniTask<HookResult> OnBeforeAsync(LauncherContext context)
    {
        // HookResult.Continue - 继续执行
        // HookResult.SkipPhase - 跳过当前阶段
        // HookResult.Abort - 中断整个管线
        return HookResult.Continue;
    }
}

// 在注册阶段之后执行
[HookTarget("Launcher", "Register")]
public class MyAfterHook : IAfterPhaseHook<LauncherContext>
{
    public int Order => 0;

    public async UniTask OnAfterAsync(LauncherContext context)
    {
        // 注册完成后的处理
    }
}
```

### 常见用例

```csharp
// 热更新：在扫描阶段之前
[HookTarget("Launcher", "Scan")]
public class HotUpdateHook : IBeforePhaseHook<LauncherContext>
{
    public int Order => 0;

    public async UniTask<HookResult> OnBeforeAsync(LauncherContext context)
    {
        await CheckAndDownloadUpdate();
        return HookResult.Continue;
    }
}

// 自定义资源加载器
[HookTarget("Launcher", "Setup")]
public class CustomLoaderHook : IBeforePhaseHook<LauncherContext>
{
    public int Order => 0;

    public async UniTask<HookResult> OnBeforeAsync(LauncherContext context)
    {
        context.ResourcesLoader = new MyResourcesLoader();
        return HookResult.Continue;
    }
}

// 加载首场景
[HookTarget("Launcher", "Start")]
public class LoadSceneHook : IAfterPhaseHook<LauncherContext>
{
    public int Order => 0;

    public async UniTask OnAfterAsync(LauncherContext context)
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
| `Azathrix > 设置` | 框架设置面板 |
| `Azathrix > 注册表 > 系统注册表` | 系统注册表管理 |
| `Azathrix > 注册表 > 启动阶段注册表` | 阶段注册表管理 |
| `Azathrix > 注册表 > 启动钩子注册表` | 钩子注册表管理 |
| `Azathrix > 调试分析 > 系统监视器` | 运行时系统监控 |

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
| `Subscribe<T>(handler)` | 订阅事件，返回 SubscriptionBuilder |
| `Dispatch<T>(evt)` | 分发事件 |
| `Dispatch<T>(ref evt)` | ref 分发事件（避免复制） |
| `DispatchSticky<T>(evt)` | 分发 Sticky 事件 |
| `Post<T>(evt)` | 延迟分发（帧结束处理） |
| `Flush()` | 刷新所有 Post 事件 |
| `SubscribeQuery<T,R>(handler)` | 订阅带返回值的查询 |
| `Query<T,R>(evt, aggregator)` | 查询并聚合结果 |
| `AddInterceptor<T>(func)` | 添加拦截器 |
| `SubscribeMessage<T>(id, handler)` | 订阅消息事件 |
| `DispatchMessage<T>(id, data)` | 分发消息事件 |

### SubscriptionBuilder（链式调用）

| 方法 | 说明 |
|------|------|
| `.Priority(n)` | 设置优先级（数值越大越先执行） |
| `.Once()` | 设为一次性订阅 |
| `.Where(filter)` | 设置过滤条件 |
| `.Skip(n)` | 跳过前 n 个事件 |
| `.Throttle(ms)` | 节流（ms 内只处理一次） |
| `.Debounce(ms)` | 防抖（ms 静默后处理） |
| `.Delay(ms)` | 延迟 ms 后处理 |
| `.Timeout(ms)` | ms 后自动取消订阅 |
| `.Sticky()` | 立即收到最后一个 Sticky 值 |
| `.AddTo(gameObject)` | 绑定到 GameObject 生命周期 |
| `.AddTo(collector)` | 添加到订阅收集器 |
| `.Unsubscribe()` | 取消订阅 |

## 性能基准

在 IL2CPP 构建下的性能测试结果：

### 吞吐量

| 操作 | 耗时 | 每秒处理量 |
|------|------|-----------|
| Dispatch | 32 ns/op | 3125 万次/秒 |
| Query | 24 ns/op | 4166 万次/秒 |
| Where 过滤 | 33 ns/op | 3030 万次/秒 |
| Priority | 42 ns/op | 2380 万次/秒 |
| Interceptor | 43 ns/op | 2325 万次/秒 |
| 10 订阅者 | 81 ns/op | 1234 万次/秒 |
| 100 订阅者 | 565 ns/op | 177 万次/秒 |
| Message | 102 ns/op | 980 万次/秒 |
| Post+Flush | 196 ns/op | 510 万次/秒 |

### GC 分配

| 操作 | 分配 (10000次) |
|------|----------------|
| Dispatch | 700 bytes |
| Event modification | 0 bytes |
| Interceptor | 1000 bytes |
| 10 订阅者分发 | 200 bytes |

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
