# 注册表

注册表系统用于可视化管理系统、阶段和钩子。

## 系统注册表

打开 `Azathrix > System Registry`

### 功能

- **启用/禁用系统** - 禁用的系统不会被注册
- **调整优先级** - 覆盖 `[SystemPriority]` 设置
- **查看依赖** - 显示系统的依赖关系
- **接口实现选择** - 为接口选择默认实现

### 接口实现选择

当多个系统实现同一接口时，可以在注册表中选择使用哪个：

```csharp
public interface IResourcesSystem : ISystem { }

[Default]
public class DefaultResourcesSystem : IResourcesSystem { }
public class YooAssetResourcesSystem : IResourcesSystem { }
```

在注册表中可以切换使用哪个实现。

## 阶段注册表

打开 `Azathrix > Phase Registry`

- 查看所有启动阶段
- 启用/禁用阶段
- 调整阶段顺序

## 钩子注册表

打开 `Azathrix > Hook Registry`

- 查看所有启动钩子
- 启用/禁用钩子
- 查看钩子绑定的阶段

## 注册表存储

注册表数据存储在 `Assets/Resources/` 目录下：

- `SystemRegistry.asset`
- `PhaseRegistry.asset`
- `StartupHookRegistry.asset`

这些文件应该提交到版本控制。
