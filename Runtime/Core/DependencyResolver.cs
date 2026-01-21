using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Registry;
using Azathrix.Framework.Tools;

namespace Azathrix.Framework.Core
{
    /// <summary>
    /// 依赖解析器 - 负责系统依赖的拓扑排序和接口实现选择
    /// </summary>
    public class DependencyResolver
    {
        /// <summary>
        /// 是否为编辑器模式（控制日志输出）
        /// </summary>
        public bool IsEditorMode { get; set; }

        /// <summary>
        /// 解析结果 - 包含排序后的类型和接口映射
        /// </summary>
        public class ResolveResult
        {
            /// <summary>
            /// 拓扑排序后的系统类型数组
            /// </summary>
            public Type[] SortedTypes { get; set; }

            /// <summary>
            /// 接口到选定实现的映射
            /// </summary>
            public Dictionary<Type, Type> InterfaceToSelectedImpl { get; set; }

            /// <summary>
            /// 默认系统类型集合
            /// </summary>
            public HashSet<Type> DefaultTypes { get; set; }
        }

        /// <summary>
        /// 解析系统依赖关系
        /// </summary>
        /// <param name="systemTypes">待解析的系统类型数组</param>
        /// <returns>解析结果</returns>
        public ResolveResult Resolve(Type[] systemTypes)
        {
            // 拓扑排序
            var sortedTypes = TopologicalSort(systemTypes);

            // 收集默认系统和接口实现
            var defaultTypes = new HashSet<Type>();
            var interfaceToImplementations = new Dictionary<Type, List<Type>>();

            foreach (var type in sortedTypes)
            {
                if (type.GetCustomAttribute<DefaultAttribute>() != null)
                    defaultTypes.Add(type);

                foreach (var iface in type.GetInterfaces())
                {
                    if (iface != typeof(ISystem) && typeof(ISystem).IsAssignableFrom(iface))
                    {
                        if (!interfaceToImplementations.ContainsKey(iface))
                            interfaceToImplementations[iface] = new List<Type>();
                        interfaceToImplementations[iface].Add(type);
                    }
                }
            }

            // 确定每个接口的实现
            var interfaceToSelectedImpl = SelectImplementations(interfaceToImplementations, defaultTypes);

            if (!IsEditorMode)
                Log.Info($"[Register] 非默认系统: {sortedTypes.Length - defaultTypes.Count} 个，默认系统: {defaultTypes.Count} 个");

            return new ResolveResult
            {
                SortedTypes = sortedTypes,
                InterfaceToSelectedImpl = interfaceToSelectedImpl,
                DefaultTypes = defaultTypes
            };
        }

        /// <summary>
        /// 选择每个接口的实现
        /// 优先级：SystemRegistry 配置 > 非默认 > 默认
        /// </summary>
        private Dictionary<Type, Type> SelectImplementations(
            Dictionary<Type, List<Type>> interfaceToImplementations,
            HashSet<Type> defaultTypes)
        {
            var settings = SystemRegistry.Instance;
            var result = new Dictionary<Type, Type>();

            foreach (var (iface, implementations) in interfaceToImplementations)
            {
                // 检查接口是否被禁用
                if (settings != null && !settings.IsInterfaceEnabled(iface.FullName))
                    continue;

                Type selected = null;

                // 1. 检查 SystemRegistry 配置
                var selectedImplName = settings?.GetSelectedImplementation(iface.FullName);
                if (!string.IsNullOrEmpty(selectedImplName))
                {
                    if (settings.IsSystemDisabled(selectedImplName))
                    {
                        if (!IsEditorMode)
                            Log.Warning($"[Register] 接口 {iface.Name} 的配置实现 {selectedImplName.Split('.').Last()} 已禁用，跳过注册");
                        continue;
                    }

                    selected = implementations.FirstOrDefault(t => t.FullName == selectedImplName);
                    if (selected == null)
                    {
                        if (!IsEditorMode)
                            Log.Warning($"[Register] 接口 {iface.Name} 的配置实现 {selectedImplName.Split('.').Last()} 已丢失，跳过注册");
                        continue;
                    }
                }

                // 2. 选择第一个未禁用的非默认实现
                if (selected == null)
                {
                    selected = implementations.FirstOrDefault(t =>
                        !defaultTypes.Contains(t) &&
                        (settings == null || !settings.IsSystemDisabled(t.FullName)));
                }

                // 3. 选择第一个未禁用的默认实现
                if (selected == null)
                {
                    selected = implementations.FirstOrDefault(t =>
                        settings == null || !settings.IsSystemDisabled(t.FullName));
                }

                if (selected != null)
                    result[iface] = selected;
            }

            return result;
        }

        /// <summary>
        /// 拓扑排序处理系统依赖关系
        /// </summary>
        /// <param name="types">待排序的类型数组</param>
        /// <returns>排序后的类型数组</returns>
        private Type[] TopologicalSort(Type[] types)
        {
            var typeSet = new HashSet<Type>(types);
            var result = new List<Type>();
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>();
            var disabled = new HashSet<Type>();

            foreach (var type in types)
            {
                if (!visited.Contains(type) && !disabled.Contains(type))
                    Visit(type, typeSet, visited, visiting, result, disabled);
            }

            return result.ToArray();
        }

        /// <summary>
        /// 深度优先遍历访问节点
        /// </summary>
        private void Visit(Type type, HashSet<Type> typeSet, HashSet<Type> visited, HashSet<Type> visiting,
            List<Type> result, HashSet<Type> disabled)
        {
            if (visited.Contains(type) || disabled.Contains(type)) return;
            if (visiting.Contains(type))
                throw new Exception($"循环依赖检测: {type.Name}");

            visiting.Add(type);

            var deps = type.GetCustomAttributes<RequireSystemAttribute>();
            var settings = SystemRegistry.Instance;

            foreach (var dep in deps)
            {
                Type depType = null;

                if (dep.DependencyType.IsInterface)
                {
                    // 检查接口是否被禁用
                    if (settings != null && !settings.IsInterfaceEnabled(dep.DependencyType.FullName))
                    {
                        Log.Warning($"[Register] 系统 {type.Name} 依赖的接口 {dep.DependencyType.Name} 已禁用，{type.Name} 也已禁用");
                        visiting.Remove(type);
                        disabled.Add(type);
                        return;
                    }

                    // 优先使用 SystemRegistry 中指定的实现
                    var selectedImpl = settings?.GetSelectedImplementation(dep.DependencyType.FullName);
                    if (!string.IsNullOrEmpty(selectedImpl))
                    {
                        if (settings.IsSystemDisabled(selectedImpl))
                        {
                            Log.Warning($"[Register] 系统 {type.Name} 依赖的接口 {dep.DependencyType.Name} 实现 {selectedImpl.Split('.').Last()} 已禁用，{type.Name} 也已禁用");
                            visiting.Remove(type);
                            disabled.Add(type);
                            return;
                        }
                        depType = typeSet.FirstOrDefault(t => t.FullName == selectedImpl);
                    }
                }

                depType ??= typeSet.FirstOrDefault(t =>
                    dep.DependencyType.IsAssignableFrom(t) || t == dep.DependencyType);

                if (depType != null)
                {
                    // 检查依赖是否在注册表中被禁用
                    if (settings != null && settings.IsSystemDisabled(depType.FullName))
                    {
                        Log.Warning($"系统 {type.Name} 依赖 {depType.Name} 在注册表中已禁用，{type.Name} 也已禁用");
                        visiting.Remove(type);
                        disabled.Add(type);
                        return;
                    }

                    Visit(depType, typeSet, visited, visiting, result, disabled);

                    // 依赖被禁用，当前系统也禁用
                    if (disabled.Contains(depType))
                    {
                        Log.Warning($"系统 {type.Name} 依赖 {depType.Name} 已被禁用，{type.Name} 也已禁用");
                        visiting.Remove(type);
                        disabled.Add(type);
                        return;
                    }
                }
                else
                {
                    // 依赖不存在
                    var depTypeName = dep.DependencyType.FullName;
                    if (settings != null && settings.IsSystemDisabled(depTypeName))
                        Log.Warning($"[Register] 系统 {type.Name} 依赖 {dep.DependencyType.Name} 已禁用，{type.Name} 也已禁用");
                    else
                        Log.Warning($"[Register] 系统 {type.Name} 依赖 {dep.DependencyType.Name} 不存在，{type.Name} 已禁用");

                    visiting.Remove(type);
                    disabled.Add(type);
                    return;
                }
            }

            visiting.Remove(type);
            visited.Add(type);
            result.Add(type);
        }
    }
}
