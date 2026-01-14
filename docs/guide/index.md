# 快速开始

本指南将帮助你在 5 分钟内上手 Azathrix Framework。

## 安装

### 方式一：Package Manager 添加 Scope（推荐）

1. 打开 `Edit > Project Settings > Package Manager`
2. 在 `Scoped Registries` 中添加：
   - **Name**: `Azathrix`
   - **URL**: `https://registry.npmjs.org`
   - **Scope(s)**: `com.azathrix`
3. 点击 `Save`
4. 打开 `Window > Package Manager`
5. 切换到 `My Registries`
6. 找到 `Azathrix Framework` 并安装

### 方式二：npm 命令

在项目的 `Packages` 目录下执行：

```bash
npm install com.azathrix.framework
```

## 创建第一个系统

```csharp
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class HelloSystem : ISystem, ISystemInitialize
{
    public async UniTask OnInitializeAsync()
    {
        Debug.Log("Hello, Azathrix Framework!");
    }
}
```

框架会自动扫描并注册这个系统。进入 Play 模式后，你会在控制台看到输出。

## 获取系统

```csharp
using Azathrix.Framework.Core;

var hello = AzathrixFramework.GetSystem<HelloSystem>();
```

## 依赖注入

```csharp
public class PlayerSystem : ISystem
{
    [Inject] private InputSystem _input;      // 必须存在
    [WeakInject] private AudioSystem _audio;  // 可选
}
```

## 事件系统

```csharp
using Azathrix.Framework.Events.Interfaces;

// 定义事件
public struct GameStartEvent : IEventDefine { }

// 监听
AzathrixFramework.Dispatcher.Register<GameStartEvent>(evt =>
{
    Debug.Log("游戏开始！");
}).AddTo(gameObject);

// 发送
AzathrixFramework.Dispatcher.SendDefault<GameStartEvent>();
```

## 下一步

- [创建系统](./systems) - 深入了解系统的创建和生命周期
- [依赖注入](./injection) - 学习如何管理系统间依赖
- [事件系统](./events) - 掌握事件的注册、发送和拦截
