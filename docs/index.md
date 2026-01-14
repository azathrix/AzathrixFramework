---
layout: home

hero:
  name: Azathrix Framework
  text: Unity 模块化游戏框架
  tagline: 轻量级系统管理、高性能事件分发、依赖注入解决方案
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
    details: 32ns/op 极速分发、零 GC、优先级、过滤、节流、防抖、拦截器、生命周期绑定
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
- **极致性能** - 32ns/op 事件分发，每秒 3000 万次，零 GC
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

### 创建系统

```csharp
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

// 获取系统
var player = AzathrixFramework.GetSystem<PlayerSystem>();
```

### 事件系统

```csharp
// 定义事件（struct 类型）
public struct PlayerDiedEvent
{
    public int PlayerId;
    public string Reason;
}

// 订阅事件 - 支持丰富的链式配置
AzathrixFramework.Dispatcher.Subscribe<PlayerDiedEvent>((ref PlayerDiedEvent e) =>
{
    Debug.Log($"玩家 {e.PlayerId} 死亡: {e.Reason}");
})
.Priority(100)                                      // 优先级
.Where((ref PlayerDiedEvent e) => e.PlayerId > 0)   // 过滤
.Once()                                             // 一次性
.Throttle(100)                                      // 节流 100ms
.AddTo(gameObject);                                 // 绑定生命周期

// 分发事件
AzathrixFramework.Dispatcher.Dispatch(new PlayerDiedEvent
{
    PlayerId = 1,
    Reason = "坠落"
});
```

### 更多事件功能

```csharp
// Sticky 事件 - 新订阅者立即收到最后值
AzathrixFramework.Dispatcher.DispatchSticky(new GameStateEvent { State = "Playing" });
AzathrixFramework.Dispatcher.Subscribe<GameStateEvent>(e => { }).Sticky();

// Post 事件 - 线程安全，帧结束处理
AzathrixFramework.Dispatcher.Post(new UIRefreshEvent());

// 带返回值的查询
AzathrixFramework.Dispatcher.SubscribeQuery<DamageEvent, int>(
    (ref DamageEvent e) => e.BaseDamage * 2
);
int total = AzathrixFramework.Dispatcher.Query<DamageEvent, int>(evt, (a, b) => a + b);

// 拦截器 - 修改或阻止事件
AzathrixFramework.Dispatcher.AddInterceptor<PlayerDiedEvent>(
    (ref InterceptorContext<PlayerDiedEvent> ctx) =>
    {
        if (ctx.Event.PlayerId == 0) return InterceptResult.Cancel;
        return InterceptResult.Continue;
    }
);

// 消息事件 - 字符串 ID
AzathrixFramework.Dispatcher.SubscribeMessage<string>("player.rename", name => { });
AzathrixFramework.Dispatcher.DispatchMessage("player.rename", "NewName");
```

### 启动管线

框架使用阶段化管线启动，支持自定义阶段和钩子：

```csharp
// 内置阶段顺序
ISetupPhase         → 框架配置 (Order: 200)
IScanPhase          → 系统扫描 (Order: 300)
IRegisterPhase      → 系统注册 (Order: 400)
IStartPhase         → 启动完成 (Order: 500)
```

```csharp
// 自定义阶段 - 在 Setup 之前执行
[PhaseOrder(150)]
public class MyCustomPhase : ISetupPhase
{
    public async UniTask ExecuteAsync(PhaseContext context)
    {
        await DoSomethingAsync();
        context.Set("myData", someValue); // 存储数据供后续阶段使用
    }
}

// 阶段钩子 - 在扫描阶段之前执行
public class HotUpdateHook : IBeforePhaseHook<IScanPhase>
{
    public int Order => 0;

    public async UniTask<HookResult> OnBeforeAsync(PhaseContext context)
    {
        await CheckAndDownloadUpdate();
        return HookResult.Continue;  // 或 SkipPhase / Abort
    }
}

// 阶段钩子 - 在启动完成后加载场景
public class LoadSceneHook : IAfterPhaseHook<IStartPhase>
{
    public int Order => 0;

    public async UniTask OnAfterAsync(PhaseContext context)
    {
        await SceneManager.LoadSceneAsync("MainMenu");
    }
}
```

## 性能基准

在 IL2CPP 构建下的测试结果：

| 操作 | 耗时 | 每秒处理量 |
|------|------|-----------|
| 事件分发 | 32 ns | 3125 万次 |
| Query 查询 | 24 ns | 4166 万次 |
| 10 订阅者 | 81 ns | 1234 万次 |
| 100 订阅者 | 565 ns | 177 万次 |
| 拦截器 | 43 ns | 2325 万次 |

## 依赖

| 包名 | 版本 | 说明 |
|------|------|------|
| [UniTask](https://github.com/Cysharp/UniTask) | 2.0+ | 异步支持 |
