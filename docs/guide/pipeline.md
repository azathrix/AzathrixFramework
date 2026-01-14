# 启动管线

框架使用阶段化管线启动，支持自定义阶段和钩子。

## 内置阶段

| 阶段 | Order | 说明 |
|------|-------|------|
| `IResourceLoadPhase` | 0 | 资源加载 |
| `IAssemblyLoadPhase` | 100 | 程序集加载（HybridCLR） |
| `ISetupPhase` | 200 | 框架配置 |
| `IScanPhase` | 300 | 系统扫描 |
| `IRegisterPhase` | 400 | 系统注册 |
| `IStartPhase` | 500 | 启动完成 |

## 自定义阶段

```csharp
using Azathrix.Framework.Core.Startup;
using Cysharp.Threading.Tasks;

[PhaseOrder(150)] // 在 Setup 之前执行
public class MyPhase : ISetupPhase
{
    public async UniTask ExecuteAsync(PhaseContext context)
    {
        // 自定义逻辑
        context.Set("myData", someValue);
    }
}
```

## 阶段钩子

### Before 钩子

在阶段执行前调用：

```csharp
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
```

### After 钩子

在阶段执行后调用：

```csharp
public class MyAfterHook : IAfterPhaseHook<IRegisterPhase>
{
    public int Order => 0;

    public async UniTask OnAfterAsync(PhaseContext context)
    {
        // 注册完成后的处理
    }
}
```

## 常见用例

### 热更新

在程序集加载前检查更新：

```csharp
public class HotUpdateHook : IBeforePhaseHook<IAssemblyLoadPhase>
{
    public int Order => 0;

    public async UniTask<HookResult> OnBeforeAsync(PhaseContext context)
    {
        await CheckAndDownloadUpdate();
        return HookResult.Continue;
    }
}
```

### 自定义资源加载器

```csharp
public class CustomLoaderHook : IBeforePhaseHook<ISetupPhase>
{
    public int Order => 0;

    public async UniTask<HookResult> OnBeforeAsync(PhaseContext context)
    {
        context.ResourcesLoader = new MyResourcesLoader();
        return HookResult.Continue;
    }
}
```

### 加载首场景

```csharp
public class LoadSceneHook : IAfterPhaseHook<IStartPhase>
{
    public int Order => 0;

    public async UniTask OnAfterAsync(PhaseContext context)
    {
        await SceneManager.LoadSceneAsync("MainMenu");
    }
}
```

## PhaseContext

阶段上下文用于在阶段间传递数据：

```csharp
// 存储数据
context.Set("key", value);

// 读取数据
var value = context.Get<MyType>("key");

// 内置属性
context.Logger          // 日志记录器
context.ResourcesLoader // 资源加载器
context.IsEditor        // 是否编辑器模式
context.Aborted         // 是否中断
```

## 编辑器模式

框架在编辑器模式下也会执行管线，但使用独立的编辑器阶段：

- `EditorSetupPhase`
- `EditorScanPhase`
- `EditorRegisterPhase`
- `EditorInitPhase`

使用 `[EditorOnly]` 标记只在编辑器执行的阶段或钩子。
