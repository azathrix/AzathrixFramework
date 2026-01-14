# AzathrixFramework

框架静态入口类。

## 属性

### IsStarted

```csharp
public static bool IsStarted { get; }
```

框架是否已启动完成。

### IsStarting

```csharp
public static bool IsStarting { get; }
```

框架是否正在启动中。

### Dispatcher

```csharp
public static EventDispatcher Dispatcher { get; }
```

事件分发器实例。

### Logger

```csharp
public static ILogger Logger { get; set; }
```

日志记录器实例。

### ResourcesLoader

```csharp
public static IResourcesLoader ResourcesLoader { get; set; }
```

资源加载器实例。

## 方法

### GetSystem\<T\>

```csharp
public static T GetSystem<T>() where T : class, ISystem
```

获取系统实例。

**参数：**
- `T` - 系统类型

**返回：** 系统实例

**异常：** 如果框架未启动或系统不存在，抛出异常

**示例：**
```csharp
var player = AzathrixFramework.GetSystem<PlayerSystem>();
```

### HasSystem\<T\>

```csharp
public static bool HasSystem<T>() where T : class, ISystem
```

检查系统是否存在。

**返回：** 系统是否存在

**示例：**
```csharp
if (AzathrixFramework.HasSystem<AudioSystem>())
{
    var audio = AzathrixFramework.GetSystem<AudioSystem>();
}
```

### InjectTo

```csharp
public static void InjectTo(object target)
```

将依赖注入到目标对象。

**参数：**
- `target` - 目标对象

**示例：**
```csharp
public class MyBehaviour : MonoBehaviour
{
    [Inject] private PlayerSystem _player;

    void Start()
    {
        AzathrixFramework.InjectTo(this);
    }
}
```

### StartupAsync

```csharp
public static async UniTask StartupAsync()
```

手动启动框架。

**示例：**
```csharp
await AzathrixFramework.StartupAsync();
```

### Pause

```csharp
public static void Pause()
```

暂停所有系统的 Update 调用。

### Resume

```csharp
public static void Resume()
```

恢复所有系统的 Update 调用。

### RefreshPipeline

```csharp
public static void RefreshPipeline()
```

刷新启动管线（重新扫描阶段和钩子）。
