# 日志系统

高级日志系统，支持多级别、颜色样式、标签过滤。

## 基础用法

```csharp
using Azathrix.Framework.Tools;

Log.Info("信息日志");
Log.Warning("警告日志");
Log.Error("错误日志");
Log.Verbose("详细日志");
```

## 颜色样式

```csharp
// 使用预设颜色（0-7）
Log.Info("彩色日志", colorStyle: 0);
Log.Info("另一种颜色", colorStyle: 2);

// 分隔线
Log.Separator("模块初始化");
Log.Separator("阶段完成", colorStyle: 1);
```

## 带标签

```csharp
Log.InfoWithTag("Network", "连接成功");
Log.WarningWithTag("Audio", "音频文件缺失");
Log.ErrorWithTag("Save", "保存失败");
```

## 输出集合

```csharp
var players = new List<string> { "Alice", "Bob", "Charlie" };
Log.LogCollection("玩家列表", players);

var config = new Dictionary<string, int> { ["hp"] = 100, ["mp"] = 50 };
Log.LogDict("配置", config);
```

## 日志级别

| 级别 | 说明 |
|------|------|
| `Verbose` | 详细日志（最低级别） |
| `Info` | 信息日志 |
| `Warning` | 警告日志 |
| `Error` | 错误日志 |
| `None` | 禁用日志 |

## 配置

在 `Project Settings > Azathrix > Log` 中配置：

### 基础配置

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| globalLogLevel | 全局日志级别 | Info |
| enableStackTrace | 启用堆栈跟踪 | true |
| enableColors | 启用颜色 | true |
| maxCollectionElements | 集合最大显示数 | 20 |

### 平台配置

可以为不同平台设置不同的日志级别：

```csharp
// 在 LogSettings 中配置
platformConfigs = new List<PlatformLogConfig>
{
    new() { platform = RuntimePlatform.Android, logLevel = LogLevel.Warning },
    new() { platform = RuntimePlatform.IPhonePlayer, logLevel = LogLevel.Error }
};
```

### 自定义标签

```csharp
customTags = new List<LogTagConfig>
{
    new() { tag = "Network", color = Color.cyan, enabled = true },
    new() { tag = "Audio", color = Color.yellow, enabled = false }
};
```

## 自定义日志记录器

实现 `ILogger` 接口：

```csharp
public class MyLogger : ILogger
{
    public void Info(object message, Object context = null, int colorStyle = 0)
    {
        // 自定义实现
    }
    // ... 其他方法
}

// 设置自定义日志记录器
AzathrixFramework.Logger = new MyLogger();
```
