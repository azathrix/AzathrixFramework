# EventDispatcher

事件分发器，处理事件的注册、发送和拦截。

## 方法

### Register\<T\>

```csharp
public EventResult Register<T>(EventHandler<T> handler) where T : IEventDefine
public EventResult Register<T>(AsyncEventHandler<T> handler) where T : IEventDefine
```

注册事件监听器。

**参数：**
- `handler` - 事件处理函数

**返回：** `EventResult` 用于链式配置

**示例：**
```csharp
// 同步
AzathrixFramework.Dispatcher.Register<PlayerDiedEvent>(evt =>
{
    Debug.Log($"玩家死亡: {evt.Reason}");
});

// 异步
AzathrixFramework.Dispatcher.Register<DataLoadEvent>(async evt =>
{
    await LoadDataAsync(evt.Path);
});
```

### Send\<T\>

```csharp
public void Send<T>(T evt, object sender = null) where T : IEventDefine
public void Send<T>(EventInitializer<T> initializer, object sender = null) where T : IEventDefine, new()
```

发送事件。

**参数：**
- `evt` - 事件实例
- `initializer` - 事件初始化器（用于 struct）
- `sender` - 发送者（可选）

**示例：**
```csharp
// 发送实例
AzathrixFramework.Dispatcher.Send(new PlayerDiedEvent { Reason = "坠落" });

// 使用初始化器
AzathrixFramework.Dispatcher.Send<PlayerDiedEvent>(ref evt =>
{
    evt.Reason = "坠落";
});
```

### SendDefault\<T\>

```csharp
public void SendDefault<T>(object sender = null) where T : IEventDefine
```

发送默认事件（无参数）。

**示例：**
```csharp
AzathrixFramework.Dispatcher.SendDefault<GameStartEvent>();
```

### AddInterceptor\<T\>

```csharp
public uint AddInterceptor<T>(EventInterceptorFunction interceptor, string name = "拦截器", int priority = 0)
    where T : IEventDefine
```

添加事件拦截器。

**参数：**
- `interceptor` - 拦截器函数
- `name` - 拦截器名称
- `priority` - 优先级

**返回：** 拦截器 ID

**示例：**
```csharp
AzathrixFramework.Dispatcher.AddInterceptor<PlayerDiedEvent>(
    (ref EventSendPackage package) =>
    {
        return InterceptorStateEnum.Next;
    },
    name: "验证器",
    priority: 100
);
```

### UnRegister

```csharp
public void UnRegister(uint id)
```

注销事件监听器。

### RemoveInterceptor

```csharp
public void RemoveInterceptor(uint id)
```

移除拦截器。

### Reset

```csharp
public void Reset()
```

重置分发器，清除所有注册。

---

## EventResult

事件注册结果，支持链式配置。

### Priority

```csharp
public EventResult Priority(int priority)
```

设置优先级（数值越大越先执行）。

### Once

```csharp
public EventResult Once()
```

设置为一次性事件（触发后自动注销）。

### InvokeNow

```csharp
public EventResult InvokeNow()
```

立即调用一次处理器。

### AddTo

```csharp
public EventResult AddTo(GameObject gameObject)
public EventResult AddTo(Component component)
public EventResult AddTo(EventCollector collector)
public EventResult AddTo(IEventCollector collector)
```

绑定生命周期。

### Destroy

```csharp
public void Destroy()
```

销毁/注销事件。

---

## InterceptorStateEnum

拦截器返回状态。

| 值 | 说明 |
|----|------|
| `Next` | 继续执行下一个拦截器 |
| `Break` | 中断拦截器链，继续分发事件 |
| `Return` | 终止事件分发 |
