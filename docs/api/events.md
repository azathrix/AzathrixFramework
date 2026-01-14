# EventDispatcher

高性能事件分发器，处理事件的订阅、分发和拦截。

## 性能基准

| 操作 | 耗时 | 每秒处理量 |
|------|------|-----------|
| Dispatch | 32 ns/op | 3125 万次/秒 |
| Query | 24 ns/op | 4166 万次/秒 |
| Where 过滤 | 33 ns/op | 3030 万次/秒 |
| Interceptor | 43 ns/op | 2325 万次/秒 |
| 10 订阅者 | 81 ns/op | 1234 万次/秒 |
| 100 订阅者 | 565 ns/op | 177 万次/秒 |

---

## 核心方法

### Subscribe\<T\>

```csharp
public SubscriptionBuilder<T> Subscribe<T>(EventCallback<T> handler) where T : struct
```

订阅事件。

**参数：**
- `handler` - 事件处理函数 `(ref T evt) => { }`

**返回：** `SubscriptionBuilder<T>` 用于链式配置

**示例：**
```csharp
AzathrixFramework.Dispatcher.Subscribe<PlayerDiedEvent>((ref PlayerDiedEvent e) =>
{
    Debug.Log($"玩家死亡: {e.Reason}");
});
```

### Dispatch\<T\>

```csharp
public void Dispatch<T>(T evt) where T : struct
public void Dispatch<T>(ref T evt) where T : struct
```

分发事件。

**参数：**
- `evt` - 事件实例

**示例：**
```csharp
// 值传递
AzathrixFramework.Dispatcher.Dispatch(new PlayerDiedEvent { Reason = "坠落" });

// ref 传递（避免复制）
var evt = new PlayerDiedEvent { Reason = "坠落" };
AzathrixFramework.Dispatcher.Dispatch(ref evt);
```

### DispatchSticky\<T\>

```csharp
public void DispatchSticky<T>(T evt) where T : struct
```

分发 Sticky 事件，新订阅者会立即收到最后一个值。

### Post\<T\>

```csharp
public void Post<T>(T evt) where T : struct
```

延迟分发事件（帧结束处理），线程安全。

### Flush

```csharp
public void Flush()
public void Flush<T>() where T : struct
```

刷新所有或指定类型的 Post 事件。

### SubscribeQuery\<T, TResult\>

```csharp
public QuerySubscriptionResult<T, TResult> SubscribeQuery<T, TResult>(
    QueryHandler<T, TResult> handler,
    int priority = 0
) where T : struct
```

订阅带返回值的查询。

### Query\<T, TResult\>

```csharp
public TResult Query<T, TResult>(T evt, Func<TResult, TResult, TResult> aggregator)
public TResult QueryFirst<T, TResult>(T evt)
```

查询并聚合结果。

**示例：**
```csharp
AzathrixFramework.Dispatcher.SubscribeQuery<DamageCalcEvent, int>(
    (ref DamageCalcEvent e) => e.BaseDamage * 2
);

int total = AzathrixFramework.Dispatcher.Query<DamageCalcEvent, int>(
    new DamageCalcEvent { BaseDamage = 100 },
    (a, b) => a + b
);
```

### AddInterceptor\<T\>

```csharp
public uint AddInterceptor<T>(
    InterceptorHandler<T> interceptor,
    int priority = 0
) where T : struct
```

添加事件拦截器。

**参数：**
- `interceptor` - 拦截器函数
- `priority` - 优先级

**返回：** 拦截器 ID

**示例：**
```csharp
AzathrixFramework.Dispatcher.AddInterceptor<PlayerDiedEvent>(
    (ref InterceptorContext<PlayerDiedEvent> ctx) =>
    {
        if (ctx.Event.PlayerId == 0)
            return InterceptResult.Cancel;
        return InterceptResult.Continue;
    },
    priority: 100
);
```

### SubscribeMessage\<T\>

```csharp
public MessageSubscriptionResult SubscribeMessage<T>(string id, Action<T> handler)
```

订阅消息事件。

### DispatchMessage\<T\>

```csharp
public void DispatchMessage<T>(string id, T data)
```

分发消息事件。

---

## SubscriptionBuilder\<T\>

订阅构建器，支持链式配置。

### Priority

```csharp
public SubscriptionBuilder<T> Priority(int priority)
```

设置优先级（数值越大越先执行）。

### Once

```csharp
public SubscriptionBuilder<T> Once()
```

设置为一次性订阅（处理后自动取消）。

### Where

```csharp
public SubscriptionBuilder<T> Where(EventFilter<T> filter)
```

设置过滤条件。

**示例：**
```csharp
.Where((ref PlayerDiedEvent e) => e.PlayerId > 0)
```

### Skip

```csharp
public SubscriptionBuilder<T> Skip(int count)
```

跳过前 n 个事件。

### Throttle

```csharp
public SubscriptionBuilder<T> Throttle(int ms)
```

节流（ms 内只处理一次）。

### Debounce

```csharp
public SubscriptionBuilder<T> Debounce(int ms)
```

防抖（ms 静默后处理）。

### Delay

```csharp
public SubscriptionBuilder<T> Delay(int ms)
```

延迟 ms 后处理。

### Timeout

```csharp
public SubscriptionBuilder<T> Timeout(int ms)
```

ms 后自动取消订阅。

### Sticky

```csharp
public SubscriptionBuilder<T> Sticky()
```

立即收到最后一个 Sticky 值。

### AddTo

```csharp
public SubscriptionBuilder<T> AddTo(GameObject gameObject)
public SubscriptionBuilder<T> AddTo(Component component)
public SubscriptionBuilder<T> AddTo(SubscriptionCollector collector)
```

绑定生命周期。

### Unsubscribe / Dispose

```csharp
public void Unsubscribe()
public void Dispose()
```

取消订阅。

---

## InterceptResult

拦截器返回状态。

| 值 | 说明 |
|----|------|
| `Continue` | 继续执行下一个拦截器和事件分发 |
| `Cancel` | 终止事件分发 |

---

## 委托类型

```csharp
// 事件回调
public delegate void EventCallback<T>(ref T evt) where T : struct;

// 事件过滤器
public delegate bool EventFilter<T>(ref T evt) where T : struct;

// 查询处理器
public delegate TResult QueryHandler<T, TResult>(ref T evt) where T : struct;

// 拦截器处理器
public delegate InterceptResult InterceptorHandler<T>(ref InterceptorContext<T> ctx) where T : struct;
```
