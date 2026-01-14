# 依赖注入

Azathrix Framework 提供简洁的依赖注入机制，自动解析系统间依赖。

## 强依赖 [Inject]

被注入的系统必须存在，否则报错：

```csharp
public class PlayerSystem : ISystem
{
    [Inject] private InputSystem _input;
    [Inject] private DataSystem _data;
}
```

## 弱依赖 [WeakInject]

被注入的系统可以不存在，字段为 null：

```csharp
public class PlayerSystem : ISystem
{
    [WeakInject] private AudioSystem _audio;  // 可能为 null

    public void PlaySound()
    {
        _audio?.PlayEffect("jump");
    }
}
```

## 手动注入

可以将依赖注入到任意对象：

```csharp
public class MyMonoBehaviour : MonoBehaviour
{
    [Inject] private PlayerSystem _player;

    void Start()
    {
        AzathrixFramework.InjectTo(this);
        // 现在 _player 已被注入
    }
}
```

## 接口注入

可以注入接口类型，框架会自动选择实现：

```csharp
public interface IResourcesSystem : ISystem
{
    T Load<T>(string path);
}

public class PlayerSystem : ISystem
{
    [Inject] private IResourcesSystem _resources;
}
```

### 默认实现

使用 `[Default]` 标记默认实现：

```csharp
[Default]
public class DefaultResourcesSystem : IResourcesSystem
{
    public T Load<T>(string path) => Resources.Load<T>(path);
}

public class YooAssetResourcesSystem : IResourcesSystem
{
    public T Load<T>(string path) => YooAssets.Load<T>(path);
}
```

### 在注册表中选择

打开 `Azathrix > System Registry`，可以为每个接口选择使用哪个实现。

## 注入时机

依赖注入发生在系统注册阶段，在 `OnInitializeAsync` 之前完成。

```csharp
public class GameSystem : ISystem, ISystemInitialize
{
    [Inject] private DataSystem _data;

    public async UniTask OnInitializeAsync()
    {
        // 此时 _data 已经被注入
        await _data.LoadAsync();
    }
}
```

## 循环依赖

框架会检测循环依赖并报错：

```csharp
// ❌ 错误：循环依赖
public class SystemA : ISystem
{
    [Inject] private SystemB _b;
}

public class SystemB : ISystem
{
    [Inject] private SystemA _a;
}
```

解决方案：使用弱依赖或重构设计。
