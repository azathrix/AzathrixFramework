<p align="center">
  <img src="Documentation~/icon.png" alt="Azathrix Framework Logo" width="120">
</p>

<h1 align="center">Azathrix Framework</h1>

<p align="center">
  Unity 模块化游戏框架
</p>

<p align="center">
  <a href="https://github.com/Azathrix/AzathrixFramework"><img src="https://img.shields.io/badge/GitHub-AzathrixFramework-black.svg" alt="GitHub"></a>
  <a href="https://www.npmjs.com/package/com.azathrix.framework"><img src="https://img.shields.io/npm/v/com.azathrix.framework.svg" alt="npm"></a>
  <a href="https://github.com/Azathrix/AzathrixFramework/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
  <a href="https://unity.com/"><img src="https://img.shields.io/badge/Unity-6000.3+-black.svg" alt="Unity"></a>
</p>

---

## 特性

- 基于接口的系统架构，支持自动扫描和注册
- 灵活的依赖注入，支持强依赖和弱依赖
- 类型安全的事件系统，支持优先级和一次性事件
- 可扩展的启动管线，支持自定义阶段和钩子
- 完整的编辑器支持，系统在编辑器模式下也能运行

## 安装

### 方式一：Package Manager 添加 Scope（推荐）

1. 打开 `Edit > Project Settings > Package Manager`
2. 在 `Scoped Registries` 中添加：
   - Name: `Azathrix`
   - URL: `https://registry.npmjs.org`
   - Scope(s): `com.azathrix`
3. 点击 `Save`
4. 打开 `Window > Package Manager`
5. 切换到 `My Registries`
6. 找到 `Azathrix Framework` 并安装

> 框架依赖 UniTask，会通过 npm 自动安装 `com.azathrix.unitask`。

### 方式二：Git URL

1. 打开 `Window > Package Manager`
2. 点击 `+` > `Add package from git URL...`
3. 输入：`https://github.com/Azathrix/AzathrixFramework.git#latest`

> ⚠️ Git 方式无法自动解析依赖，需要先手动安装：
> - [UniTask](https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask)

### 方式三：npm 命令

在项目的 `Packages` 目录下执行：

```bash
npm install com.azathrix.framework
```

## 快速开始

### 创建系统

```csharp
[AutoRegister]
public class PlayerSystem : ISystem, ISystemInitialize, ISystemUpdate
{
    [Inject] private InputSystem _input;
    [WeakInject] private AudioSystem _audio; // 可选依赖

    public async UniTask OnInitializeAsync()
    {
        // 初始化逻辑
    }

    public void OnUpdate()
    {
        // 每帧更新
    }
}
```

### 获取系统

```csharp
var player = AzathrixFramework.GetSystem<PlayerSystem>();
```

### 事件系统

```csharp
// 定义事件
public enum GameEvent { PlayerDied, LevelComplete }

// 注册监听
AzathrixFramework.Dispatcher.On<GameEvent>(GameEvent.PlayerDied, evt =>
{
    Debug.Log("Player died!");
}).Priority(100).AddTo(gameObject);

// 触发事件
AzathrixFramework.Dispatcher.Emit(GameEvent.PlayerDied);

// 带参数的事件
AzathrixFramework.Dispatcher.Emit(GameEvent.LevelComplete, new { score = 1000 });
```

## 系统属性

| 属性 | 说明 |
|------|------|
| `[AutoRegister]` | 自动注册系统 |
| `[Inject]` | 依赖注入（必须存在） |
| `[WeakInject]` | 弱依赖注入（可为空） |
| `[DependsOn(typeof(...))]` | 声明系统依赖顺序 |
| `[SystemPriority(n)]` | 系统优先级（越小越先） |
| `[SystemAlias("name")]` | 系统别名 |
| `[UpdateInterval(ms)]` | Update 调用间隔 |
| `[ConditionalSystem("SYMBOL")]` | 条件编译注册 |

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

### 运行时阶段

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
    }
}
```

### 阶段钩子

```csharp
// 在扫描阶段之前执行
public class MyBeforeHook : IBeforePhaseHook<IScanPhase>
{
    public int Order => 0;

    public async UniTask<bool> OnBeforeAsync(PhaseContext context)
    {
        // 返回 false 可中断管线
        return true;
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

    public async UniTask<bool> OnBeforeAsync(PhaseContext context)
    {
        await CheckAndDownloadUpdate();
        return true;
    }
}

// 自定义资源加载器：在 Setup 阶段之前
public class CustomLoaderHook : IBeforePhaseHook<ISetupPhase>
{
    public int Order => 0;

    public async UniTask<bool> OnBeforeAsync(PhaseContext context)
    {
        context.ResourcesLoader = new MyResourcesLoader();
        return true;
    }
}

// 加载首场景：在启动阶段之后
public class LoadSceneHook : IAfterPhaseHook<IStartPhase>
{
    public int Order => 0;

    public async UniTask OnAfterAsync(PhaseContext context)
    {
        await SceneManager.LoadSceneAsync("MainMenu");
    }
}
```

## 编辑器工具

- **Azathrix > Settings** - 框架配置
- **Azathrix > System Registry** - 系统注册管理
- **Azathrix > System Monitor** - 运行时系统监控

## 配置说明

在 `Assets/Resources/AzathrixFrameworkSettings.asset` 中配置：

- **扫描模式** - All（全部）或 Specified（指定程序集）
- **只扫描 [AutoRegister]** - 是否只注册带标记的系统
- **自动初始化** - 进入 Play 模式时自动启动框架
- **编辑器支持** - 编辑器模式下初始化支持的系统

## 依赖

- [UniTask](https://github.com/Cysharp/UniTask) - 通过 npm 自动安装

## License

MIT
