# 属性

系统和依赖注入相关属性。

## [Inject]

标记字段需要依赖注入（必须存在）。

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InjectAttribute : Attribute { }
```

**示例：**
```csharp
public class PlayerSystem : ISystem
{
    [Inject] private InputSystem _input;
}
```

## [WeakInject]

标记字段为弱依赖注入（可为空）。

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class WeakInjectAttribute : Attribute { }
```

**示例：**
```csharp
public class PlayerSystem : ISystem
{
    [WeakInject] private AudioSystem _audio;  // 可能为 null
}
```

## [SystemPriority]

设置系统注册优先级（越小越先）。

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class SystemPriorityAttribute : Attribute
{
    public int Priority { get; }
    public SystemPriorityAttribute(int priority) { }
}
```

**示例：**
```csharp
[SystemPriority(100)]
public class CoreSystem : ISystem { }
```

## [RequireSystem]

声明系统依赖。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequireSystemAttribute : Attribute
{
    public Type SystemType { get; }
    public RequireSystemAttribute(Type systemType) { }
}
```

**示例：**
```csharp
[RequireSystem(typeof(DataSystem))]
[RequireSystem(typeof(ConfigSystem))]
public class GameSystem : ISystem { }
```

## [SystemAlias]

为系统设置别名。

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class SystemAliasAttribute : Attribute
{
    public string Alias { get; }
    public SystemAliasAttribute(string alias) { }
}
```

**示例：**
```csharp
[SystemAlias("Player")]
public class PlayerControlSystem : ISystem { }
```

## [UpdateInterval]

设置 Update 调用间隔（毫秒）。

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class UpdateIntervalAttribute : Attribute
{
    public int IntervalMs { get; }
    public UpdateIntervalAttribute(int intervalMs) { }
}
```

**示例：**
```csharp
[UpdateInterval(100)]  // 每 100ms 调用一次
public class SlowSystem : ISystem, ISystemUpdate
{
    public void OnUpdate() { }
}
```

## [Default]

标记为接口的默认实现。

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class DefaultAttribute : Attribute { }
```

**示例：**
```csharp
public interface IResourcesSystem : ISystem { }

[Default]
public class DefaultResourcesSystem : IResourcesSystem { }
public class YooAssetResourcesSystem : IResourcesSystem { }
```

## [PhaseOrder]

设置启动阶段顺序。

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class PhaseOrderAttribute : Attribute
{
    public int Order { get; }
    public PhaseOrderAttribute(int order) { }
}
```

**示例：**
```csharp
[PhaseOrder(150)]
public class MyPhase : ISetupPhase { }
```

## [EditorSupport]

标记系统支持编辑器模式。

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class EditorSupportAttribute : Attribute { }
```

## [EditorOnly]

标记阶段或钩子只在编辑器执行。

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class EditorOnlyAttribute : Attribute { }
```
