# 启动管线

启动管线相关接口。

## IStartupPhase

启动阶段接口。

```csharp
public interface IStartupPhase
{
    UniTask ExecuteAsync(PhaseContext context);
}
```

### 内置阶段接口

| 接口 | Order | 说明 |
|------|-------|------|
| `IResourceLoadPhase` | 0 | 资源加载 |
| `IAssemblyLoadPhase` | 100 | 程序集加载 |
| `ISetupPhase` | 200 | 框架配置 |
| `IScanPhase` | 300 | 系统扫描 |
| `IRegisterPhase` | 400 | 系统注册 |
| `IStartPhase` | 500 | 启动完成 |

## IBeforePhaseHook\<T\>

阶段执行前钩子。

```csharp
public interface IBeforePhaseHook<TPhase> where TPhase : IStartupPhase
{
    int Order { get; }
    UniTask<HookResult> OnBeforeAsync(PhaseContext context);
}
```

## IAfterPhaseHook\<T\>

阶段执行后钩子。

```csharp
public interface IAfterPhaseHook<TPhase> where TPhase : IStartupPhase
{
    int Order { get; }
    UniTask OnAfterAsync(PhaseContext context);
}
```

## IStartupHook

通用钩子（可匹配多个阶段）。

```csharp
public interface IStartupHook
{
    int Order { get; }
    bool Match(string phaseId, Type phaseType);
    UniTask<HookResult> OnBeforeAsync(string phaseId, PhaseContext context);
    UniTask OnAfterAsync(string phaseId, PhaseContext context);
}
```

## HookResult

钩子返回结果。

```csharp
public enum HookResult
{
    Continue,   // 继续执行
    SkipPhase,  // 跳过当前阶段
    Abort       // 中断整个管线
}
```

## PhaseContext

阶段执行上下文。

```csharp
public class PhaseContext
{
    public ILogger Logger { get; set; }
    public IResourcesLoader ResourcesLoader { get; set; }
    public bool IsEditor { get; set; }
    public Type[] ScannedSystemTypes { get; set; }
    public bool Aborted { get; set; }

    public T Get<T>(string key);
    public void Set(string key, object value);
}
```

## 示例

### 自定义阶段

```csharp
[PhaseOrder(150)]
public class ConfigLoadPhase : ISetupPhase
{
    public async UniTask ExecuteAsync(PhaseContext context)
    {
        var config = await LoadConfigAsync();
        context.Set("gameConfig", config);
    }
}
```

### Before 钩子

```csharp
public class HotUpdateHook : IBeforePhaseHook<IAssemblyLoadPhase>
{
    public int Order => 0;

    public async UniTask<HookResult> OnBeforeAsync(PhaseContext context)
    {
        var needUpdate = await CheckUpdateAsync();
        if (needUpdate)
        {
            await DownloadUpdateAsync();
        }
        return HookResult.Continue;
    }
}
```

### After 钩子

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
