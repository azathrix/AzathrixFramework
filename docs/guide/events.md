# 事件系统

类型安全的事件系统，支持优先级、拦截器、生命周期绑定。

## 定义事件

推荐使用 struct 避免 GC 分配：

```csharp
using Azathrix.Framework.Events.Interfaces;

public struct PlayerDiedEvent : IEventDefine
{
    public int PlayerId;
    public string Reason;
}

public struct GameStartEvent : IEventDefine { }
```

## 注册监听

```csharp
// 基础注册
AzathrixFramework.Dispatcher.Register<PlayerDiedEvent>(evt =>
{
    Debug.Log($"玩家 {evt.PlayerId} 死亡: {evt.Reason}");
});
```

## 链式配置

```csharp
AzathrixFramework.Dispatcher.Register<PlayerDiedEvent>(evt =>
{
    Debug.Log("处理死亡事件");
})
.Priority(100)           // 优先级（越大越先执行）
.Once()                  // 一次性事件
.InvokeNow()             // 立即调用一次
.AddTo(gameObject);      // 绑定生命周期
```

## 生命周期绑定

### 绑定到 GameObject

GameObject 销毁时自动注销事件：

```csharp
AzathrixFramework.Dispatcher.Register<GameEvent>(evt => { })
    .AddTo(gameObject);
```

### 绑定到 EventCollector

手动管理事件生命周期：

```csharp
var collector = new EventCollector();

AzathrixFramework.Dispatcher.Register<Event1>(e => { }).AddTo(collector);
AzathrixFramework.Dispatcher.Register<Event2>(e => { }).AddTo(collector);

// 一次性注销所有事件
collector.Dispose();
```

## 发送事件

```csharp
// 发送事件实例
AzathrixFramework.Dispatcher.Send(new PlayerDiedEvent
{
    PlayerId = 1,
    Reason = "坠落"
});

// 使用初始化器（支持 struct）
AzathrixFramework.Dispatcher.Send<PlayerDiedEvent>(ref evt =>
{
    evt.PlayerId = 1;
    evt.Reason = "坠落";
});

// 发送默认事件（无参数）
AzathrixFramework.Dispatcher.SendDefault<GameStartEvent>();
```

## 异步事件

```csharp
AzathrixFramework.Dispatcher.Register<DataLoadEvent>(async evt =>
{
    await LoadDataAsync(evt.Path);
    Debug.Log("数据加载完成");
});
```

## 事件拦截器

拦截器可以修改或阻止事件：

```csharp
AzathrixFramework.Dispatcher.AddInterceptor<PlayerDiedEvent>(
    (ref EventSendPackage package) =>
    {
        var evt = (PlayerDiedEvent)package.eventData;

        // 阻止事件
        if (evt.PlayerId == 0)
            return InterceptorStateEnum.Return;

        // 修改事件
        package.eventData = new PlayerDiedEvent
        {
            PlayerId = evt.PlayerId,
            Reason = "修改后的原因"
        };

        // 继续传递
        return InterceptorStateEnum.Next;
    },
    name: "死亡验证",
    priority: 100
);
```

### 拦截器返回值

| 返回值 | 说明 |
|--------|------|
| `Next` | 继续执行下一个拦截器 |
| `Break` | 中断拦截器链，但继续分发事件 |
| `Return` | 终止事件分发 |

## 手动注销

```csharp
var result = AzathrixFramework.Dispatcher.Register<MyEvent>(e => { });

// 手动注销
result.Destroy();
```
