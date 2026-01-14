# 编辑器工具

框架提供多个编辑器工具帮助开发和调试。

## 菜单

| 菜单路径 | 说明 |
|----------|------|
| `Azathrix > Settings` | 框架设置面板 |
| `Azathrix > System Registry` | 系统注册表管理 |
| `Azathrix > Phase Registry` | 阶段注册表管理 |
| `Azathrix > Hook Registry` | 钩子注册表管理 |
| `Azathrix > System Monitor` | 运行时系统监控 |

## 框架设置

`Project Settings > Azathrix > 框架设置`

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| projectId | 项目ID | NewGame |
| versionFormat | 版本格式 | {major}.{minor}.{patch} |
| scanMode | 扫描模式 | All |
| assemblyNames | 指定扫描的程序集 | - |
| excludeAssemblyPrefixes | 排除的程序集前缀 | System, Microsoft... |
| autoInitialize | 自动初始化框架 | true |
| enableProfiling | 启用性能统计 | false |
| systemInfoLevel | 系统信息输出级别 | Simple |

## 系统监控

`Azathrix > System Monitor`

运行时查看：

- 已注册的系统列表
- 系统状态（启用/禁用）
- 系统依赖关系
- Update 调用统计

## 编辑器模式支持

系统可以在编辑器模式下运行：

```csharp
public class EditorToolSystem : ISystem, ISystemEditorSupport
{
    public void OnEditorInitialize()
    {
        // 编辑器初始化
    }

    public void OnEditorUpdate()
    {
        // 编辑器更新
    }
}
```

使用 `[EditorSupport]` 标记系统支持编辑器模式：

```csharp
[EditorSupport]
public class MyEditorSystem : ISystem { }
```

## 调试日志

在框架设置中启用 `debugEditorPipeline` 可以查看编辑器管线的详细日志。
