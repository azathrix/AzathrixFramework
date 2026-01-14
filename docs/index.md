---
layout: home

hero:
  name: Azathrix Framework
  text: Unity 模块化游戏框架
  tagline: 系统管理、事件分发、依赖注入、启动管线
  actions:
    - theme: brand
      text: 快速开始
      link: /guide/
    - theme: alt
      text: API 参考
      link: /api/
    - theme: alt
      text: GitHub
      link: https://github.com/azathrix/AzathrixFramework

features:
  - icon: 🏗️
    title: 模块化系统架构
    details: 基于接口的系统设计，支持自动扫描和注册，轻松管理游戏各个模块
  - icon: 💉
    title: 依赖注入
    details: 支持强依赖 [Inject] 和弱依赖 [WeakInject]，自动解析系统间依赖关系
  - icon: 📡
    title: 类型安全事件系统
    details: 支持优先级、一次性事件、拦截器、生命周期绑定，零 GC 分配
  - icon: 🚀
    title: 可扩展启动管线
    details: 阶段化启动流程，支持自定义阶段和钩子，适配热更新场景
  - icon: 📝
    title: 高级日志系统
    details: 多级别日志、平台配置、颜色样式、标签过滤，调试更高效
  - icon: ⚙️
    title: 注册表系统
    details: 可视化管理系统启用/禁用、优先级、接口实现选择
---

## 快速安装

### Package Manager（推荐）

1. 打开 `Edit > Project Settings > Package Manager`
2. 添加 Scoped Registry：
   - **Name**: `Azathrix`
   - **URL**: `https://registry.npmjs.org`
   - **Scope(s)**: `com.azathrix`
3. 在 Package Manager 中安装 `Azathrix Framework`

### npm 命令

```bash
cd Packages
npm install com.azathrix.framework
```

## 快速示例

```csharp
using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;

// 创建系统
[SystemPriority(100)]
public class PlayerSystem : ISystem, ISystemInitialize, ISystemUpdate
{
    [Inject] private InputSystem _input;
    [WeakInject] private AudioSystem _audio;

    public async UniTask OnInitializeAsync()
    {
        // 初始化
    }

    public void OnUpdate()
    {
        // 每帧更新
    }
}

// 获取系统
var player = AzathrixFramework.GetSystem<PlayerSystem>();

// 事件系统
AzathrixFramework.Dispatcher.Register<PlayerDiedEvent>(evt =>
{
    Debug.Log($"玩家死亡: {evt.Reason}");
}).Priority(100).AddTo(gameObject);

AzathrixFramework.Dispatcher.Send(new PlayerDiedEvent { Reason = "坠落" });
```
