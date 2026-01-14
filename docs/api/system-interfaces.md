# 系统接口

系统生命周期接口参考。

## ISystem

系统基础接口，所有系统必须实现。

```csharp
public interface ISystem { }
```

## ISystemRegister

系统注册时调用。

```csharp
public interface ISystemRegister : ISystemEvent
{
    void OnRegister();
}
```

## ISystemInitialize

异步初始化，在注册后调用。

```csharp
public interface ISystemInitialize : ISystemEvent
{
    UniTask OnInitializeAsync();
}
```

## ISystemEnabled

启用/禁用回调。

```csharp
public interface ISystemEnabled : ISystemEvent
{
    void OnEnabled();
    void OnDisabled();
}
```

## ISystemUpdate

每帧更新。

```csharp
public interface ISystemUpdate : ISystemEvent
{
    void OnUpdate();
}
```

## ISystemFixedUpdate

固定时间步更新。

```csharp
public interface ISystemFixedUpdate : ISystemEvent
{
    void OnFixedUpdate();
}
```

## ISystemLateUpdate

延迟更新。

```csharp
public interface ISystemLateUpdate : ISystemEvent
{
    void OnLateUpdate();
}
```

## ISystemApplicationPause

应用暂停/恢复。

```csharp
public interface ISystemApplicationPause : ISystemEvent
{
    void OnApplicationPause(bool pause);
}
```

## ISystemApplicationFocusChanged

焦点变化。

```csharp
public interface ISystemApplicationFocusChanged : ISystemEvent
{
    void OnApplicationFocusChanged(bool focus);
}
```

## ISystemApplicationQuit

应用退出。

```csharp
public interface ISystemApplicationQuit : ISystemEvent
{
    void OnApplicationQuit();
}
```

## ISystemEditorSupport

编辑器模式支持。

```csharp
public interface ISystemEditorSupport : ISystemEvent
{
    void OnEditorInitialize();
    void OnEditorUpdate();
}
```

## 使用示例

```csharp
public class GameSystem : ISystem,
    ISystemInitialize,
    ISystemUpdate,
    ISystemApplicationPause
{
    public async UniTask OnInitializeAsync()
    {
        // 初始化
    }

    public void OnUpdate()
    {
        // 每帧更新
    }

    public void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveGame();
    }
}
```
