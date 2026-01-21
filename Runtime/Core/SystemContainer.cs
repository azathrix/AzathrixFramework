using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Interfaces;

namespace Azathrix.Framework.Core
{
    /// <summary>
    /// 系统容器 - 负责系统实例的存储和访问
    /// </summary>
    public class SystemContainer
    {
        /// <summary>
        /// 所有已注册的系统实例列表
        /// </summary>
        private readonly List<ISystem> _systems = new();

        /// <summary>
        /// 类型到实例的映射（支持接口和具体类两种方式访问）
        /// </summary>
        private readonly Dictionary<Type, ISystem> _typeToInstance = new();

        /// <summary>
        /// 别名到实例的映射
        /// </summary>
        private readonly Dictionary<string, ISystem> _aliasToInstance = new();

        /// <summary>
        /// 已完成初始化的系统集合
        /// </summary>
        private readonly HashSet<ISystem> _initializedSystems = new();

        /// <summary>
        /// 系统注册时触发
        /// </summary>
        public event Action<ISystem> OnSystemRegistered;

        /// <summary>
        /// 系统注销时触发
        /// </summary>
        public event Action<ISystem> OnSystemUnregistered;

        /// <summary>
        /// 系统启用状态变化时触发
        /// </summary>
        public event Action<ISystem, bool> OnSystemEnabledChanged;

        /// <summary>
        /// 获取所有已注册的系统（只读）
        /// </summary>
        public IReadOnlyList<ISystem> Systems => _systems;

        /// <summary>
        /// 获取类型到实例的映射（只读）
        /// </summary>
        public IReadOnlyDictionary<Type, ISystem> TypeToInstance => _typeToInstance;

        /// <summary>
        /// 获取系统 - 支持接口或具体类
        /// </summary>
        /// <typeparam name="T">系统类型（接口或具体类）</typeparam>
        /// <returns>系统实例，未找到返回 null</returns>
        public T GetSystem<T>() where T : class, ISystem
        {
            return _typeToInstance.TryGetValue(typeof(T), out var system) ? system as T : null;
        }

        /// <summary>
        /// 检查系统是否存在
        /// </summary>
        /// <typeparam name="T">系统类型</typeparam>
        public bool HasSystem<T>() where T : class, ISystem
        {
            return _typeToInstance.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 检查系统是否存在（通过类型）
        /// </summary>
        /// <param name="type">系统类型</param>
        public bool HasSystem(Type type)
        {
            return _typeToInstance.ContainsKey(type);
        }

        /// <summary>
        /// 通过别名获取系统
        /// </summary>
        /// <param name="alias">系统别名</param>
        /// <returns>系统实例，未找到返回 null</returns>
        public ISystem GetSystemByAlias(string alias)
        {
            return _aliasToInstance.TryGetValue(alias, out var system) ? system : null;
        }

        /// <summary>
        /// 通过别名获取系统（泛型版本）
        /// </summary>
        /// <typeparam name="T">系统类型</typeparam>
        /// <param name="alias">系统别名</param>
        public T GetSystemByAlias<T>(string alias) where T : class, ISystem
        {
            return GetSystemByAlias(alias) as T;
        }

        /// <summary>
        /// 添加系统到容器
        /// </summary>
        /// <param name="system">系统实例</param>
        /// <param name="type">系统具体类型</param>
        public void Add(ISystem system, Type type)
        {
            _systems.Add(system);
            _typeToInstance[type] = system;
        }

        /// <summary>
        /// 注册接口到系统实例的映射
        /// </summary>
        /// <param name="interfaceType">接口类型</param>
        /// <param name="system">系统实例</param>
        public void RegisterInterface(Type interfaceType, ISystem system)
        {
            if (!_typeToInstance.ContainsKey(interfaceType))
                _typeToInstance[interfaceType] = system;
        }

        /// <summary>
        /// 注册系统别名
        /// </summary>
        /// <param name="alias">别名</param>
        /// <param name="system">系统实例</param>
        public void RegisterAlias(string alias, ISystem system)
        {
            if (!string.IsNullOrEmpty(alias))
                _aliasToInstance[alias] = system;
        }

        /// <summary>
        /// 从容器中移除系统
        /// </summary>
        /// <param name="system">要移除的系统实例</param>
        public void Remove(ISystem system)
        {
            _systems.Remove(system);
            _initializedSystems.Remove(system);

            // 移除所有指向该实例的类型映射
            var keysToRemove = _typeToInstance.Where(kv => kv.Value == system).Select(kv => kv.Key).ToList();
            foreach (var key in keysToRemove)
                _typeToInstance.Remove(key);

            // 移除别名映射
            var aliasToRemove = _aliasToInstance.Where(kv => kv.Value == system).Select(kv => kv.Key).ToList();
            foreach (var alias in aliasToRemove)
                _aliasToInstance.Remove(alias);
        }

        /// <summary>
        /// 标记系统为已初始化
        /// </summary>
        /// <param name="system">系统实例</param>
        public void MarkInitialized(ISystem system)
        {
            _initializedSystems.Add(system);
        }

        /// <summary>
        /// 检查系统是否已初始化
        /// </summary>
        /// <param name="system">系统实例</param>
        public bool IsInitialized(ISystem system)
        {
            return _initializedSystems.Contains(system);
        }

        /// <summary>
        /// 按指定比较器排序系统列表
        /// </summary>
        /// <param name="comparison">比较器</param>
        public void Sort(Comparison<ISystem> comparison)
        {
            _systems.Sort(comparison);
        }

        /// <summary>
        /// 触发系统注册事件
        /// </summary>
        public void RaiseSystemRegistered(ISystem system) => OnSystemRegistered?.Invoke(system);

        /// <summary>
        /// 触发系统注销事件
        /// </summary>
        public void RaiseSystemUnregistered(ISystem system) => OnSystemUnregistered?.Invoke(system);

        /// <summary>
        /// 触发系统启用状态变化事件
        /// </summary>
        public void RaiseSystemEnabledChanged(ISystem system, bool enabled) => OnSystemEnabledChanged?.Invoke(system, enabled);
    }
}
