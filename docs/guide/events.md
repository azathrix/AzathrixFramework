# 事件系统

高性能、零 GC 的类型安全事件系统，支持优先级、过滤、节流、防抖、拦截器、生命周期绑定。

## 性能特点

- **32ns/op** - 每秒可处理 3125 万次事件
- **零 GC** - 核心分发路径无内存分配
- **类型安全** - 编译时类型检查

## 定义事件

事件必须是 struct 类型：

```csharp
public struct PlayerDiedEvent
{
    public int PlayerId;
    public string Reason;
}

public struct GameStartEvent { }
```

## 订阅事件

```csharp
// 基础订阅
AzathrixFramework.Dispatcher.Subscribe<PlayerDiedEvent>((ref PlayerDiedEvent e) =>
{
    Debug.Log($"玩家 {e.PlayerId} 死亡: {e.Reason}");
});
```

## 链式配置

```csharp
AzathrixFramework.Dispatcher.Subscribe<PlayerDiedEvent>((ref PlayerDiedEvent e) =>
{
    Debug.Log("处理死亡事件");
})
.Priority(100)                                    // 优先级（越大越先执行）
.Once()                                           // 一次性订阅
.Where((ref PlayerDiedEvent e) => e.PlayerId > 0) // 过滤条件
.Skip(2)                                          // 跳过前2个事件
.Throttle(100)                                    // 节流（100ms内只处理一次）
.Debounce(200)                                    // 防抖（200ms静默后处理）
.Delay(50)                                        // 延迟50ms处理
.Timeout(5000)                                    // 5秒后自动取消订阅
.AddTo(gameObject);                               // 绑定生命周期
```

## 生命周期绑定

### 绑定到 GameObject

GameObject 销毁时自动取消订阅：

```csharp
AzathrixFramework.Dispatcher.Subscribe<GameEvent>((ref GameEvent e) => { })
    .AddTo(gameObject);
```

### 绑定到 SubscriptionCollector

手动管理订阅生命周期：

```csharp
var collector = new SubscriptionCollector();

AzathrixFramework.Dispatcher.Subscribe<Event1>((ref Event1 e) => { }).AddTo(collector);
AzathrixFramework.Dispatcher.Subscribe<Event2>((ref Event2 e) => { }).AddTo(collector);

// 一次性取消所有订阅
collector.Dispose();
```

## 分发事件

```csharp
// 分发事件实例
AzathrixFramework.Dispatcher.Dispatch(new PlayerDiedEvent
{
    PlayerId = 1,
    Reason = "坠落"
});

// ref 分发（避免复制，性能更好）
var evt = new PlayerDiedEvent { PlayerId = 1, Reason = "坠落" };
AzathrixFramework.Dispatcher.Dispatch(ref evt);
```

## Sticky 事件

新订阅者立即收到最后一个值：

```csharp
// 分发 Sticky 事件
AzathrixFramework.Dispatcher.DispatchSticky(new GameStateEvent { State = "Playing" });

// 订阅时立即收到最后的值
AzathrixFramework.Dispatcher.Subscribe<GameStateEvent>((ref GameStateEvent e) =>
{
    Debug.Log(e.State); // 立即输出 "Playing"
}).Sticky();
```

## Post 事件（延迟分发）

线程安全，延迟到帧结束处理：

```csharp
// 从任意线程安全调用
AzathrixFramework.Dispatcher.Post(new UIRefreshEvent());

// 帧结束时自动 Flush，也可手动：
AzathrixFramework.Dispatcher.Flush();
```

## 带返回值的查询

```csharp
// 订阅查询处理器
AzathrixFramework.Dispatcher.SubscribeQuery<DamageCalcEvent, int>(
    (ref DamageCalcEvent e) => e.BaseDamage * 2
);

// 查询并聚合结果
int total = AzathrixFramework.Dispatcher.Query<DamageCalcEvent, int>(
    new DamageCalcEvent { BaseDamage = 100 },
    (a, b) => a + b  // 聚合函数
);
```

## 消息事件

基于字符串 ID 的事件，线程安全：

```csharp
// 订阅消息
AzathrixFramework.Dispatcher.SubscribeMessage<string>("player.name.changed", name =>
{
    Debug.Log($"玩家名称变更: {name}");
});

// 分发消息
AzathrixFramework.Dispatcher.DispatchMessage("player.name.changed", "NewName");
```

## 事件拦截器

拦截器可以修改或阻止事件：

```csharp
AzathrixFramework.Dispatcher.AddInterceptor<PlayerDiedEvent>(
    (ref InterceptorContext<PlayerDiedEvent> ctx) =>
    {
        // 阻止事件
        if (ctx.Event.PlayerId == 0)
            return InterceptResult.Cancel;

        // 修改事件
        ctx.Event.Reason = "修改后的原因";

        // 继续传递
        return InterceptResult.Continue;
    },
    priority: 100
);
```

### 拦截器返回值

| 返回值 | 说明 |
|--------|------|
| `Continue` | 继续执行下一个拦截器和事件分发 |
| `Cancel` | 终止事件分发 |

## 手动取消订阅

```csharp
var sub = AzathrixFramework.Dispatcher.Subscribe<MyEvent>((ref MyEvent e) => { });

// 手动取消
sub.Unsubscribe();
// 或
sub.Dispose();
```
