# API 概述

Azathrix Framework 的核心 API 参考。

## 核心类

| 类 | 说明 |
|----|------|
| [AzathrixFramework](./framework) | 框架静态入口 |
| [EventDispatcher](./events) | 事件分发器 |

## 接口

| 接口 | 说明 |
|------|------|
| [ISystem](./system-interfaces#isystem) | 系统基础接口 |
| [ISystemInitialize](./system-interfaces#isysteminitialize) | 异步初始化 |
| [ISystemUpdate](./system-interfaces#isystemupdate) | 每帧更新 |
| [更多...](./system-interfaces) | 其他生命周期接口 |

## 属性

| 属性 | 说明 |
|------|------|
| [[Inject]](./attributes#inject) | 依赖注入 |
| [[WeakInject]](./attributes#weakinject) | 弱依赖注入 |
| [[SystemPriority]](./attributes#systempriority) | 系统优先级 |
| [更多...](./attributes) | 其他属性 |

## 启动管线

| 接口 | 说明 |
|------|------|
| [IStartupPhase](./pipeline#istartupphase) | 启动阶段 |
| [IBeforePhaseHook](./pipeline#ibeforephasehook) | 阶段前钩子 |
| [IAfterPhaseHook](./pipeline#iafterphasehook) | 阶段后钩子 |

## 命名空间

```csharp
using Azathrix.Framework.Core;           // 核心功能
using Azathrix.Framework.Core.Attributes; // 属性
using Azathrix.Framework.Core.Startup;    // 启动管线
using Azathrix.Framework.Events.Core;     // 事件系统
using Azathrix.Framework.Events.Interfaces; // 事件接口
using Azathrix.Framework.Interfaces;      // 系统接口
using Azathrix.Framework.Interfaces.SystemEvents; // 生命周期接口
using Azathrix.Framework.Tools;           // 工具类
using Azathrix.Framework.Settings;        // 设置
```
