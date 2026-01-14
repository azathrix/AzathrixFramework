# 创建系统

系统是 Azathrix Framework 的核心概念，用于组织游戏逻辑。

## 基础系统

```csharp
using Azathrix.Framework.Interfaces;

public class MySystem : ISystem
{
    // 最简单的系统，只需实现 ISystem 接口
}
```

## 生命周期接口

通过实现不同接口，系统可以响应各种生命周期事件：

### ISystemInitialize

异步初始化，在系统注册后调用：

```csharp
public class DataSystem : ISystem, ISystemInitialize
{
    public async UniTask OnInitializeAsync()
    {
        await LoadDataAsync();
    }
}
```

### ISystemUpdate

每帧更新：

```csharp
public class PlayerSystem : ISystem, ISystemUpdate
{
    public void OnUpdate()
    {
        // 每帧执行
    }
}
```

### ISystemFixedUpdate

固定时间步更新（物理相关）：

```csharp
public class PhysicsSystem : ISystem, ISystemFixedUpdate
{
    public void OnFixedUpdate()
    {
        // 固定时间步执行
    }
}
```

### ISystemLateUpdate

延迟更新（相机跟随等）：

```csharp
public class CameraSystem : ISystem, ISystemLateUpdate
{
    public void OnLateUpdate()
    {
        // Update 之后执行
    }
}
```

### 其他接口

| 接口 | 说明 |
|------|------|
| `ISystemRegister` | 注册时调用 |
| `ISystemEnabled` | 启用/禁用回调 |
| `ISystemApplicationPause` | 应用暂停/恢复 |
| `ISystemApplicationFocusChanged` | 焦点变化 |
| `ISystemApplicationQuit` | 应用退出 |
| `ISystemEditorSupport` | 编辑器模式支持 |

## 系统属性

### SystemPriority

控制系统注册顺序（越小越先）：

```csharp
[SystemPriority(100)]
public class CoreSystem : ISystem { }

[SystemPriority(200)]
public class GameSystem : ISystem { }
```

### UpdateInterval

控制 Update 调用间隔：

```csharp
[UpdateInterval(100)] // 每 100ms 调用一次
public class SlowUpdateSystem : ISystem, ISystemUpdate
{
    public void OnUpdate() { }
}
```

### SystemAlias

为系统设置别名：

```csharp
[SystemAlias("Player")]
public class PlayerControlSystem : ISystem { }
```

### RequireSystem

声明系统依赖：

```csharp
[RequireSystem(typeof(DataSystem))]
public class GameSystem : ISystem { }
```

## 获取系统

```csharp
// 获取系统实例
var player = AzathrixFramework.GetSystem<PlayerSystem>();

// 检查系统是否存在
if (AzathrixFramework.HasSystem<AudioSystem>())
{
    var audio = AzathrixFramework.GetSystem<AudioSystem>();
}
```

## 暂停和恢复

```csharp
// 暂停所有系统的 Update
AzathrixFramework.Pause();

// 恢复
AzathrixFramework.Resume();
```
