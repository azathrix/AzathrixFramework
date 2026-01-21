using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core
{
    /// <summary>
    /// 系统状态信息
    /// </summary>
    public class SystemStatus
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public Type Type { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsInitialized { get; set; }
        public int Priority { get; set; }
        public double LastUpdateMs { get; set; }
        public double AverageUpdateMs { get; set; }
        public bool CanToggle { get; set; }
        public string ModuleId { get; set; }
    }

    /// <summary>
    /// 游戏系统运行时管理器
    /// 协调系统容器、依赖注入、依赖解析等组件，管理系统的完整生命周期
    /// </summary>
    public class SystemRuntimeManager
    {
        #region 组件

        /// <summary>
        /// 系统容器 - 负责系统存储和访问
        /// </summary>
        private readonly SystemContainer _container = new();

        /// <summary>
        /// 依赖注入器 - 负责依赖注入
        /// </summary>
        private readonly DependencyInjector _injector;

        /// <summary>
        /// 依赖解析器 - 负责拓扑排序和接口选择
        /// </summary>
        private readonly DependencyResolver _resolver = new();

        #endregion

        #region 生命周期事件列表

        private readonly List<ISystemUpdate> _updateList = new();
        private readonly List<ISystemFixedUpdate> _fixedUpdateList = new();
        private readonly List<ISystemLateUpdate> _lateUpdateList = new();
        private readonly List<ISystemApplicationQuit> _quitList = new();
        private readonly List<ISystemApplicationFocusChanged> _focusList = new();
        private readonly List<ISystemApplicationPause> _pauseList = new();
        private readonly List<ISystemRegister> _registerList = new();
        private readonly List<ISystemInitialize> _initAsyncList = new();

        #endregion

        #region 性能统计

        private readonly Dictionary<ISystem, PerformanceData> _performanceData = new();
        private readonly Stopwatch _stopwatch = new();

        #endregion

        #region Update 间隔控制

        private readonly Dictionary<ISystem, UpdateIntervalData> _updateIntervals = new();
        private int _frameCount;

        #endregion

        #region 元数据缓存

        private readonly Dictionary<Type, SystemMetadata> _metadataCache = new();

        #endregion

        #region 属性

        /// <summary>
        /// Runtime 是否暂停
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 是否启用性能统计
        /// </summary>
        public bool EnableProfiling { get; set; }

        /// <summary>
        /// 是否为编辑器模式（跳过运行时生命周期）
        /// </summary>
        public bool IsEditorMode
        {
            get => _isEditorMode;
            set
            {
                _isEditorMode = value;
                _injector.IsEditorMode = value;
                _resolver.IsEditorMode = value;
            }
        }
        private bool _isEditorMode;

        #endregion

        #region 事件

        /// <summary>
        /// 系统注册时触发
        /// </summary>
        public event Action<ISystem> OnSystemRegistered
        {
            add => _container.OnSystemRegistered += value;
            remove => _container.OnSystemRegistered -= value;
        }

        /// <summary>
        /// 系统注销时触发
        /// </summary>
        public event Action<ISystem> OnSystemUnregistered
        {
            add => _container.OnSystemUnregistered += value;
            remove => _container.OnSystemUnregistered -= value;
        }

        /// <summary>
        /// 系统启用状态变化时触发
        /// </summary>
        public event Action<ISystem, bool> OnSystemEnabledChanged
        {
            add => _container.OnSystemEnabledChanged += value;
            remove => _container.OnSystemEnabledChanged -= value;
        }

        #endregion

        /// <summary>
        /// 创建系统运行时管理器
        /// </summary>
        public SystemRuntimeManager()
        {
            _injector = new DependencyInjector(_container);
        }

        #region 系统访问

        /// <summary>
        /// 获取系统名称
        /// </summary>
        public string GetSystemName(ISystem system) => GetSystemName(system.GetType());

        /// <summary>
        /// 获取系统名称（通过类型）
        /// </summary>
        public string GetSystemName(Type type)
        {
            var meta = GetOrCreateMetadata(type);
            return meta.Alias ?? meta.Name;
        }

        /// <summary>
        /// 获取系统优先级
        /// </summary>
        public int GetSystemPriority(ISystem system) => GetSystemPriority(system.GetType());

        /// <summary>
        /// 获取系统优先级（通过类型）
        /// </summary>
        public int GetSystemPriority(Type type) => GetOrCreateMetadata(type).Priority;

        /// <summary>
        /// 获取系统 - 支持接口或具体类
        /// </summary>
        public T GetSystem<T>() where T : class, ISystem => _container.GetSystem<T>();

        /// <summary>
        /// 检查系统是否存在
        /// </summary>
        public bool HasSystem<T>() where T : class, ISystem => _container.HasSystem<T>();

        /// <summary>
        /// 获取所有已注册的系统
        /// </summary>
        public IReadOnlyList<ISystem> GetAllSystems() => _container.Systems;

        /// <summary>
        /// 通过别名获取系统
        /// </summary>
        public ISystem GetSystemByAlias(string alias) => _container.GetSystemByAlias(alias);

        /// <summary>
        /// 通过别名获取系统（泛型版本）
        /// </summary>
        public T GetSystemByAlias<T>(string alias) where T : class, ISystem => _container.GetSystemByAlias<T>(alias);

        /// <summary>
        /// 获取系统状态信息
        /// </summary>
        public SystemStatus GetSystemStatus<T>() where T : class, ISystem
        {
            var system = GetSystem<T>();
            if (system == null) return null;

            var perf = _performanceData.GetValueOrDefault(system);
            var type = system.GetType();
            return new SystemStatus
            {
                Name = type.Name,
                Alias = type.GetCustomAttribute<SystemAliasAttribute>()?.Alias,
                Type = type,
                IsEnabled = system is not ISystemEnabled {Enabled: false},
                IsInitialized = _container.IsInitialized(system),
                Priority = GetSystemPriority(type),
                LastUpdateMs = perf?.LastMs ?? 0,
                AverageUpdateMs = perf?.AverageMs ?? 0
            };
        }

        /// <summary>
        /// 获取所有系统状态
        /// </summary>
        public List<SystemStatus> GetAllSystemStatus()
        {
            return _container.Systems.Select(sys =>
            {
                var perf = _performanceData.GetValueOrDefault(sys);
                var type = sys.GetType();
                var meta = GetOrCreateMetadata(type);
                return new SystemStatus
                {
                    Name = meta.Name,
                    Alias = meta.Alias,
                    Type = type,
                    IsEnabled = sys is not ISystemEnabled {Enabled: false},
                    IsInitialized = _container.IsInitialized(sys),
                    Priority = meta.Priority,
                    LastUpdateMs = perf?.LastMs ?? 0,
                    AverageUpdateMs = perf?.AverageMs ?? 0,
                    CanToggle = sys is ISystemEnabled,
                    ModuleId = type.Assembly.GetName().Name
                };
            }).ToList();
        }

        #endregion

        #region Runtime 控制

        /// <summary>
        /// 暂停 Runtime
        /// </summary>
        public void Pause() => IsPaused = true;

        /// <summary>
        /// 恢复 Runtime
        /// </summary>
        public void Resume() => IsPaused = false;

        #endregion

        #region 系统注册

        /// <summary>
        /// 从类型数组创建并注册系统
        /// </summary>
        public async UniTask CreateSystemFromTypesAsync(Type[] systemTypes)
        {
            if (!IsEditorMode)
                Log.Info($"[Register] 开始处理 {systemTypes.Length} 个系统类型");

            // 解析依赖关系
            var result = _resolver.Resolve(systemTypes);

            // 按拓扑排序顺序注册
            foreach (var type in result.SortedTypes)
            {
                try
                {
                    bool isSelected = result.InterfaceToSelectedImpl.Values.Contains(type);
                    if (result.DefaultTypes.Contains(type) && !isSelected)
                        continue;

                    Register(type, result.InterfaceToSelectedImpl);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // 依赖注入
            _injector.InjectAll();

            // 排序
            SortByPriority();

            // 调用生命周期
            InvokeRegister();
            await InvokeInitializeAsync();

            if (!IsEditorMode)
                Log.Info($"[Register] 系统注册完成，共 {_container.Systems.Count} 个系统");
        }

        /// <summary>
        /// 动态注册单个系统（运行时）
        /// </summary>
        public async UniTask RegisterSystemAsync<T>() where T : class, ISystem, new()
        {
            await RegisterSystemAsync(typeof(T));
        }

        /// <summary>
        /// 动态注册单个系统（同步版本）
        /// </summary>
        public void RegisterSystem<T>() where T : class, ISystem, new()
        {
            RegisterSystemAsync<T>().Forget();
        }

        /// <summary>
        /// 动态注册单个系统（通过类型）
        /// </summary>
        public async UniTask RegisterSystemAsync(Type type)
        {
            if (_container.HasSystem(type))
            {
                Log.Info($"系统 {type.Name} 已注册，跳过");
                return;
            }

            Register(type);
            _injector.InjectTo(_container.TypeToInstance[type]);
            SortByPriority();

            var system = _container.TypeToInstance[type];

            if (system is ISystemRegister reg)
            {
                try { reg.OnRegister(); }
                catch (Exception e) { Log.Exception(e); }
            }

            if (system is ISystemInitialize init)
            {
                try { await init.OnInitializeAsync(); }
                catch (Exception e) { Log.Exception(e); }
            }

            _container.MarkInitialized(system);
        }

        /// <summary>
        /// 注册系统
        /// </summary>
        private void Register(Type type, Dictionary<Type, Type> interfaceToSelectedImpl = null)
        {
            if (_container.HasSystem(type))
                return;

            var system = Activator.CreateInstance(type) as ISystem
                         ?? throw new Exception($"创建系统实例失败: {type}");

            _container.Add(system, type);
            _performanceData[system] = new PerformanceData();

            // 注册接口映射
            var registeredInterfaces = new List<string>();
            foreach (var iface in type.GetInterfaces())
            {
                if (iface != typeof(ISystem) && typeof(ISystem).IsAssignableFrom(iface))
                {
                    if (_container.HasSystem(iface))
                        continue;

                    if (interfaceToSelectedImpl != null)
                    {
                        if (interfaceToSelectedImpl.TryGetValue(iface, out var selected) && selected != type)
                            continue;
                    }

                    _container.RegisterInterface(iface, system);
                    registeredInterfaces.Add(iface.Name);
                }
            }

            // 注册生命周期事件
            TryAddEvent(system, _updateList);
            TryAddEvent(system, _fixedUpdateList);
            TryAddEvent(system, _lateUpdateList);
            TryAddEvent(system, _quitList);
            TryAddEvent(system, _focusList);
            TryAddEvent(system, _pauseList);
            TryAddEvent(system, _registerList);
            TryAddEvent(system, _initAsyncList);

            // 注册别名和 Update 间隔
            var meta = GetOrCreateMetadata(type);
            _container.RegisterAlias(meta.Alias, system);

            if (meta.UpdateInterval > 1)
                _updateIntervals[system] = new UpdateIntervalData(meta.UpdateInterval);

            if (!IsEditorMode)
            {
                var systemName = GetSystemName(system);
                var priority = meta.Priority != 0 ? $" (优先级:{meta.Priority})" : "";
                var interfaces = registeredInterfaces.Count > 0 ? $" → {string.Join(", ", registeredInterfaces)}" : "";
                Log.Info($"[Register]   + {systemName}{priority}{interfaces}");
            }

            _container.RaiseSystemRegistered(system);
        }

        /// <summary>
        /// 注销系统
        /// </summary>
        public void UnRegister<T>() where T : class, ISystem => UnRegister(typeof(T));

        /// <summary>
        /// 注销系统（通过类型）
        /// </summary>
        public void UnRegister(Type type)
        {
            if (!_container.TypeToInstance.TryGetValue(type, out var system))
                return;

            if (system is ISystemRegister reg)
            {
                try { reg.OnUnRegister(); }
                catch (Exception e) { Log.Exception(e); }
            }

            _performanceData.Remove(system);

            TryRemoveEvent(system, _updateList);
            TryRemoveEvent(system, _fixedUpdateList);
            TryRemoveEvent(system, _lateUpdateList);
            TryRemoveEvent(system, _quitList);
            TryRemoveEvent(system, _focusList);
            TryRemoveEvent(system, _pauseList);
            TryRemoveEvent(system, _registerList);
            TryRemoveEvent(system, _initAsyncList);

            _updateIntervals.Remove(system);
            _container.Remove(system);

            if (!IsEditorMode)
                Log.Info($"[Register] 注销系统: {GetSystemName(system)}");

            _container.RaiseSystemUnregistered(system);
        }

        /// <summary>
        /// 启用/禁用指定系统
        /// </summary>
        public void SetSystemEnabled<T>(bool enabled) where T : class, ISystem
        {
            var system = GetSystem<T>();
            SetSystemEnabledInternal(system, enabled);
        }

        /// <summary>
        /// 启用/禁用指定系统（通过类型）
        /// </summary>
        public void SetSystemEnabled(Type type, bool enabled)
        {
            if (_container.TypeToInstance.TryGetValue(type, out var system))
                SetSystemEnabledInternal(system, enabled);
        }

        private void SetSystemEnabledInternal(ISystem system, bool enabled)
        {
            if (system is ISystemEnabled sys)
            {
                var oldEnabled = sys.Enabled;
                sys.Enabled = enabled;
                if (oldEnabled != enabled)
                    _container.RaiseSystemEnabledChanged(system, enabled);
            }
        }

        #endregion

        #region 依赖注入

        /// <summary>
        /// 向任意对象注入依赖
        /// </summary>
        public void InjectTo(object target) => _injector.InjectTo(target);

        #endregion

        #region 生命周期调用

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update(float deltaTime)
        {
            if (IsPaused) return;

            _frameCount++;

            foreach (var sys in _updateList)
            {
                if (sys is ISystemEnabled {Enabled: false}) continue;

                var gameSystem = (ISystem) sys;
                if (_updateIntervals.TryGetValue(gameSystem, out var intervalData))
                {
                    if (!intervalData.ShouldUpdate(_frameCount))
                        continue;
                }

                try
                {
                    if (EnableProfiling)
                    {
                        _stopwatch.Restart();
                        sys.OnUpdate(deltaTime);
                        _stopwatch.Stop();
                        RecordPerformance(gameSystem, _stopwatch.Elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        sys.OnUpdate(deltaTime);
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }
        }

        /// <summary>
        /// 固定时间步更新
        /// </summary>
        public void FixedUpdate(float deltaTime)
        {
            if (IsPaused) return;
            InvokeLifecycle(_fixedUpdateList, sys => sys.OnFixedUpdate(deltaTime));
        }

        /// <summary>
        /// 延迟更新
        /// </summary>
        public void LateUpdate(float deltaTime)
        {
            if (IsPaused) return;
            InvokeLifecycle(_lateUpdateList, sys => sys.OnLateUpdate(deltaTime));
        }

        /// <summary>
        /// 应用退出
        /// </summary>
        public void OnApplicationQuit() =>
            InvokeLifecycle(_quitList, sys => sys.OnApplicationQuit(), checkEnabled: false);

        /// <summary>
        /// 应用焦点变化
        /// </summary>
        public void OnApplicationFocus(bool focus) =>
            InvokeLifecycle(_focusList, sys => sys.OnApplicationFocus(focus), checkEnabled: false);

        /// <summary>
        /// 应用暂停
        /// </summary>
        public void OnApplicationPause(bool pause) =>
            InvokeLifecycle(_pauseList, sys => sys.OnApplicationPause(pause), checkEnabled: false);

        private void InvokeLifecycle<T>(List<T> list, Action<T> action, bool checkEnabled = true)
        {
            foreach (var sys in list)
            {
                if (checkEnabled && sys is ISystemEnabled {Enabled: false}) continue;
                try { action(sys); }
                catch (Exception e) { Log.Exception(e); }
            }
        }

        private void InvokeRegister()
        {
            if (IsEditorMode) return;
            foreach (var sys in _registerList)
            {
                try { sys.OnRegister(); }
                catch (Exception e) { Log.Exception(e); }
            }
        }

        private async UniTask InvokeInitializeAsync()
        {
            if (IsEditorMode)
            {
                foreach (var sys in _container.Systems)
                    _container.MarkInitialized(sys);
                return;
            }

            foreach (var sys in _initAsyncList)
            {
                try
                {
                    await sys.OnInitializeAsync();
                    _container.MarkInitialized((ISystem) sys);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            foreach (var sys in _container.Systems)
                _container.MarkInitialized(sys);
        }

        #endregion

        #region 私有方法

        private void TryAddEvent<T>(ISystem system, List<T> list) where T : class
        {
            if (system is T evt)
                list.Add(evt);
        }

        private void TryRemoveEvent<T>(ISystem system, List<T> list) where T : class
        {
            if (system is T evt)
                list.Remove(evt);
        }

        private void SortByPriority()
        {
            int Compare(ISystem a, ISystem b) => GetSystemPriority(a).CompareTo(GetSystemPriority(b));

            _container.Sort(Compare);
            _updateList.Sort((a, b) => Compare((ISystem) a, (ISystem) b));
            _fixedUpdateList.Sort((a, b) => Compare((ISystem) a, (ISystem) b));
            _lateUpdateList.Sort((a, b) => Compare((ISystem) a, (ISystem) b));
            _quitList.Sort((a, b) => Compare((ISystem) a, (ISystem) b));
            _focusList.Sort((a, b) => Compare((ISystem) a, (ISystem) b));
            _pauseList.Sort((a, b) => Compare((ISystem) a, (ISystem) b));
            _registerList.Sort((a, b) => Compare((ISystem) a, (ISystem) b));
            _initAsyncList.Sort((a, b) => Compare((ISystem) a, (ISystem) b));
        }

        #endregion

        #region 元数据缓存

        private class SystemMetadata
        {
            public string Name;
            public string Alias;
            public int Priority;
            public int UpdateInterval;
        }

        private SystemMetadata GetOrCreateMetadata(Type type)
        {
            if (_metadataCache.TryGetValue(type, out var meta))
                return meta;

            var aliasAttr = type.GetCustomAttribute<SystemAliasAttribute>();
            var priorityAttr = type.GetCustomAttribute<SystemPriorityAttribute>();
            var intervalAttr = type.GetCustomAttribute<UpdateIntervalAttribute>();

            meta = new SystemMetadata
            {
                Name = type.Name,
                Alias = aliasAttr?.Alias,
                Priority = priorityAttr?.Priority ?? 0,
                UpdateInterval = intervalAttr?.FrameInterval ?? 0
            };
            _metadataCache[type] = meta;
            return meta;
        }

        #endregion

        #region 性能统计

        private class PerformanceData
        {
            private const int SampleCount = 60;
            private readonly Queue<double> _samples = new();
            public double LastMs { get; private set; }
            public double AverageMs => _samples.Count > 0 ? _samples.Average() : 0;

            public void Record(double ms)
            {
                LastMs = ms;
                _samples.Enqueue(ms);
                if (_samples.Count > SampleCount)
                    _samples.Dequeue();
            }
        }

        private void RecordPerformance(ISystem system, double ms)
        {
            if (_performanceData.TryGetValue(system, out var data))
                data.Record(ms);
        }

        #endregion

        #region Update 间隔控制

        private class UpdateIntervalData
        {
            public int Interval { get; }
            private int _lastUpdateFrame;

            public UpdateIntervalData(int interval) => Interval = interval;

            public bool ShouldUpdate(int currentFrame)
            {
                if (currentFrame - _lastUpdateFrame >= Interval)
                {
                    _lastUpdateFrame = currentFrame;
                    return true;
                }
                return false;
            }
        }

        #endregion

        #region 依赖图导出

        /// <summary>
        /// 依赖关系信息
        /// </summary>
        public class DependencyInfo
        {
            public string SystemName { get; set; }
            public Type SystemType { get; set; }
            public List<Type> Dependencies { get; set; } = new();
            public List<Type> Injections { get; set; } = new();
        }

        /// <summary>
        /// 获取所有系统的依赖关系
        /// </summary>
        public List<DependencyInfo> GetDependencyGraph()
        {
            var result = new List<DependencyInfo>();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var system in _container.Systems)
            {
                var type = system.GetType();
                var info = new DependencyInfo
                {
                    SystemName = GetSystemName(system),
                    SystemType = type
                };

                var deps = type.GetCustomAttributes<RequireSystemAttribute>();
                foreach (var dep in deps)
                    info.Dependencies.Add(dep.DependencyType);

                foreach (var field in type.GetFields(flags))
                {
                    if (field.GetCustomAttribute<InjectAttribute>() != null ||
                        field.GetCustomAttribute<WeakInjectAttribute>() != null)
                        info.Injections.Add(field.FieldType);
                }

                foreach (var prop in type.GetProperties(flags))
                {
                    if (prop.GetCustomAttribute<InjectAttribute>() != null ||
                        prop.GetCustomAttribute<WeakInjectAttribute>() != null)
                        info.Injections.Add(prop.PropertyType);
                }

                result.Add(info);
            }

            return result;
        }

        /// <summary>
        /// 导出依赖图为字符串
        /// </summary>
        public string ExportDependencyGraphAsText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 系统依赖图 ===");

            var graph = GetDependencyGraph();
            foreach (var info in graph)
            {
                sb.AppendLine($"\n[{info.SystemName}] ({info.SystemType.Name})");

                if (info.Dependencies.Count > 0)
                {
                    sb.AppendLine("  依赖:");
                    foreach (var dep in info.Dependencies)
                        sb.AppendLine($"    - {dep.Name}");
                }

                if (info.Injections.Count > 0)
                {
                    sb.AppendLine("  注入:");
                    foreach (var inj in info.Injections)
                        sb.AppendLine($"    - {inj.Name}");
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}
