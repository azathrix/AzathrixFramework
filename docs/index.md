---
layout: home

hero:
  name: Azathrix Framework
  text: Unity 模块化游戏框架
  tagline: 轻量级系统管理、事件分发、依赖注入解决方案
  image:
    src: /logo.png
    alt: Azathrix Framework
  actions:
    - theme: brand
      text: 快速开始 →
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
    details: 基于接口的系统设计，自动扫描注册，支持优先级排序和依赖拓扑，轻松管理游戏各模块
  - icon: 💉
    title: 智能依赖注入
    details: 强依赖 [Inject] 确保系统存在，弱依赖 [WeakInject] 优雅处理可选模块，自动解析依赖图
  - icon: 📡
    title: 高性能事件系统
    details: 类型安全、优先级控制、一次性事件、拦截器、生命周期绑定，零 GC 分配设计
  - icon: 🚀
    title: 可扩展启动管线
    details: 阶段化启动流程，支持自定义 Phase 和 Hook，完美适配热更新和异步加载场景
  - icon: 📝
    title: 专业日志系统
    details: 多级别日志、平台差异化配置、颜色样式、标签过滤，让调试事半功倍
  - icon: ⚙️
    title: 可视化注册表
    details: 编辑器内管理系统启用/禁用、优先级调整、接口实现选择，所见即所得
---

## 为什么选择 Azathrix Framework？

- **零配置启动** - 实现接口即可，框架自动扫描注册
- **类型安全** - 编译期检查，告别字符串魔法
- **高性能** - 零 GC 事件系统，优化的依赖解析
- **易于测试** - 基于接口设计，方便 Mock 和单元测试
- **热更新友好** - 启动管线支持动态加载

## 快速安装

::: code-group

```text [Package Manager（推荐）]
1. Edit > Project Settings > Package Manager
2. 添加 Scoped Registry:
   Name: Azathrix
   URL: https://registry.npmjs.org
   Scope(s): com.azathrix
3. 在 Package Manager 中安装
```

```bash [npm]
cd Packages
npm install com.azathrix.framework
```

```text [Git URL]
Window > Package Manager > + > Add package from git URL
https://github.com/azathrix/AzathrixFramework.git
```

:::

## 30 秒上手

```csharp
// 1. 创建系统 - 实现接口即可
public class PlayerSystem : ISystem, ISystemInitialize, ISystemUpdate
{
    [Inject] private InputSystem _input;      // 强依赖
    [WeakInject] private AudioSystem _audio;  // 可选依赖

    public async UniTask OnInitializeAsync()
    {
        Debug.Log("PlayerSystem 初始化完成");
    }

    public void OnUpdate()
    {
        if (_input.GetKeyDown(KeyCode.Space))
            _audio?.PlayJump();
    }
}

// 2. 获取系统
var player = AzathrixFramework.GetSystem<PlayerSystem>();

// 3. 使用事件
AzathrixFramework.Dispatcher.Register<PlayerDiedEvent>(evt =>
{
    Debug.Log($"玩家死亡: {evt.Reason}");
}).AddTo(gameObject);

AzathrixFramework.Dispatcher.Send(new PlayerDiedEvent { Reason = "坠落" });
```

## 依赖

| 包名 | 版本 | 说明 |
|------|------|------|
| [UniTask](https://github.com/Cysharp/UniTask) | 2.0+ | 异步支持 |
