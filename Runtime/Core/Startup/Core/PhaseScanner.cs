using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Azathrix.Framework.Core.Configs;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Registry;
using Azathrix.Framework.Tools;

namespace Azathrix.Framework.Core.Startup
{
    /// <summary>
    /// 阶段和钩子扫描器
    /// </summary>
    public class PhaseScanner
    {
        private readonly ILogger _logger;
        private readonly ScannerConfig _config;
        private readonly bool _isEditorMode;

        public PhaseScanner(ILogger logger, ScannerConfig config, bool isEditorMode = false)
        {
            _logger = logger;
            _config = config;
            _isEditorMode = isEditorMode;
        }

        /// <summary>
        /// 扫描启动阶段
        /// </summary>
        public List<IStartupPhase> ScanPhases()
        {
            // 从 PhaseRegistry 读取
            var registry = PhaseRegistry.Instance;
            if (registry != null && registry.entries.Count > 0)
            {
                var phases = registry.GetOrderedPhases(_isEditorMode)
                    .Select(e => CreatePhase(e.GetRuntimeType()))
                    .Where(p => p != null)
                    .ToList();

                // Log.Info($"[PhaseScanner] 从 PhaseRegistry 加载 {phases.Count} 个阶段");
                return phases;
            }

            // 注册表为空，记录错误并返回空列表
            Log.Error("[PhaseScanner] PhaseRegistry 为空或未初始化，无法加载阶段");
            return new List<IStartupPhase>();
        }

        private IStartupPhase CreatePhase(Type type)
        {
            if (type == null) return null;
            try
            {
                return (IStartupPhase)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                Log.Warning($"[PhaseScanner] 创建阶段 {type.FullName} 失败: {e.Message}");
                return null;
            }
        }

        private List<IStartupPhase> ScanPhasesByReflection()
        {
            var phases = new List<(IStartupPhase phase, int order)>();

            foreach (var assembly in GetAssemblies())
            {
                try
                {
                    foreach (var type in GetTypesFromAssembly(assembly))
                    {
                        if (!typeof(IStartupPhase).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                            continue;

                        // 编辑器模式下检查 EditorSupport 特性
                        if (_isEditorMode && type.GetCustomAttribute<EditorSupportAttribute>() == null)
                            continue;

                        try
                        {
                            var phase = (IStartupPhase)Activator.CreateInstance(type);
                            phases.Add((phase, phase.Order));
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"[PhaseScanner] 创建阶段 {type.FullName} 失败: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[PhaseScanner] 扫描程序集 {assembly.GetName().Name} 失败: {e.Message}");
                }
            }

            return phases.OrderBy(p => p.order).Select(p => p.phase).ToList();
        }

        /// <summary>
        /// 扫描指定阶段的前置钩子
        /// </summary>
        public List<IBeforePhaseHook<TPhase>> ScanBeforeHooks<TPhase>() where TPhase : IStartupPhase
        {
            // 从 HookRegistry 读取
            var registry = StartupHookRegistry.Instance;
            if (registry != null && registry.entries.Count > 0)
            {
                return registry.GetBeforeHookTypes(typeof(TPhase).FullName)
                    .Select(t => CreateHook<IBeforePhaseHook<TPhase>>(t))
                    .Where(h => h != null)
                    .ToList();
            }

            return new List<IBeforePhaseHook<TPhase>>();
        }

        private List<IBeforePhaseHook<TPhase>> ScanBeforeHooksByReflection<TPhase>() where TPhase : IStartupPhase
        {
            var hooks = new List<(IBeforePhaseHook<TPhase> hook, int order)>();

            foreach (var assembly in GetAssemblies())
            {
                try
                {
                    foreach (var type in GetTypesFromAssembly(assembly))
                    {
                        if (!typeof(IBeforePhaseHook<TPhase>).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                            continue;

                        try
                        {
                            var hook = (IBeforePhaseHook<TPhase>)Activator.CreateInstance(type);
                            hooks.Add((hook, hook.Order));
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"[PhaseScanner] 创建钩子 {type.FullName} 失败: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[PhaseScanner] 扫描程序集 {assembly.GetName().Name} 失败: {e.Message}");
                }
            }

            return hooks.OrderBy(h => h.order).Select(h => h.hook).ToList();
        }

        /// <summary>
        /// 扫描指定阶段的后置钩子
        /// </summary>
        public List<IAfterPhaseHook<TPhase>> ScanAfterHooks<TPhase>() where TPhase : IStartupPhase
        {
            // 从 HookRegistry 读取
            var registry = StartupHookRegistry.Instance;
            if (registry != null && registry.entries.Count > 0)
            {
                return registry.GetAfterHookTypes(typeof(TPhase).FullName)
                    .Select(t => CreateHook<IAfterPhaseHook<TPhase>>(t))
                    .Where(h => h != null)
                    .ToList();
            }

            return new List<IAfterPhaseHook<TPhase>>();
        }

        private List<IAfterPhaseHook<TPhase>> ScanAfterHooksByReflection<TPhase>() where TPhase : IStartupPhase
        {
            var hooks = new List<(IAfterPhaseHook<TPhase> hook, int order)>();

            foreach (var assembly in GetAssemblies())
            {
                try
                {
                    foreach (var type in GetTypesFromAssembly(assembly))
                    {
                        if (!typeof(IAfterPhaseHook<TPhase>).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                            continue;

                        try
                        {
                            var hook = (IAfterPhaseHook<TPhase>)Activator.CreateInstance(type);
                            hooks.Add((hook, hook.Order));
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"[PhaseScanner] 创建钩子 {type.FullName} 失败: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[PhaseScanner] 扫描程序集 {assembly.GetName().Name} 失败: {e.Message}");
                }
            }

            return hooks.OrderBy(h => h.order).Select(h => h.hook).ToList();
        }

        private T CreateHook<T>(Type type) where T : class
        {
            if (type == null) return null;
            try
            {
                return Activator.CreateInstance(type) as T;
            }
            catch (Exception e)
            {
                Log.Warning($"[PhaseScanner] 创建钩子 {type.FullName} 失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 扫描所有钩子（按阶段类型分组）
        /// </summary>
        public (Dictionary<Type, List<object>> beforeHooks, Dictionary<Type, List<object>> afterHooks) ScanAllHooks()
        {
            var beforeHooks = new Dictionary<Type, List<object>>();
            var afterHooks = new Dictionary<Type, List<object>>();

            // 从 HookRegistry 读取
            var registry = StartupHookRegistry.Instance;
            if (registry != null && registry.entries.Count > 0)
            {
                foreach (var entry in registry.GetEnabledEntries())
                {
                    var hookType = entry.GetRuntimeType();
                    if (hookType == null)
                    {
                        Log.Warning($"[PhaseScanner] 钩子类型 {entry.typeName} 无法加载，请刷新注册表");
                        continue;
                    }

                    var phaseType = entry.GetTargetPhaseType();
                    if (phaseType == null)
                    {
                        // 检测旧资源缺少 targetPhaseAssembly 字段的情况
                        if (string.IsNullOrEmpty(entry.targetPhaseAssembly))
                            Log.Warning($"[PhaseScanner] 钩子 {entry.displayName} 缺少 targetPhaseAssembly，请刷新注册表 (Azathrix/Refresh All Registries)");
                        else
                            Log.Warning($"[PhaseScanner] 钩子 {entry.displayName} 的目标阶段 {entry.targetPhaseType} 无法加载");
                        continue;
                    }

                    try
                    {
                        var hook = Activator.CreateInstance(hookType);
                        var targetDict = entry.isBefore ? beforeHooks : afterHooks;

                        if (!targetDict.ContainsKey(phaseType))
                            targetDict[phaseType] = new List<object>();
                        targetDict[phaseType].Add(hook);
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"[PhaseScanner] 创建钩子 {entry.typeName} 失败: {e.Message}");
                    }
                }

                // 排序
                foreach (var key in beforeHooks.Keys.ToList())
                    beforeHooks[key] = beforeHooks[key].OrderBy(h => ((dynamic)h).Order).ToList();
                foreach (var key in afterHooks.Keys.ToList())
                    afterHooks[key] = afterHooks[key].OrderBy(h => ((dynamic)h).Order).ToList();
            }

            return (beforeHooks, afterHooks);
        }

        private (Dictionary<Type, List<object>> beforeHooks, Dictionary<Type, List<object>> afterHooks) ScanAllHooksByReflection()
        {
            var beforeHooks = new Dictionary<Type, List<object>>();
            var afterHooks = new Dictionary<Type, List<object>>();

            foreach (var assembly in GetAssemblies())
            {
                try
                {
                    foreach (var type in GetTypesFromAssembly(assembly))
                    {
                        if (type.IsAbstract || type.IsInterface)
                            continue;

                        foreach (var iface in type.GetInterfaces())
                        {
                            if (!iface.IsGenericType)
                                continue;

                            var genericDef = iface.GetGenericTypeDefinition();
                            var phaseType = iface.GetGenericArguments()[0];

                            try
                            {
                                if (genericDef == typeof(IBeforePhaseHook<>))
                                {
                                    if (!beforeHooks.ContainsKey(phaseType))
                                        beforeHooks[phaseType] = new List<object>();
                                    beforeHooks[phaseType].Add(Activator.CreateInstance(type));
                                }
                                else if (genericDef == typeof(IAfterPhaseHook<>))
                                {
                                    if (!afterHooks.ContainsKey(phaseType))
                                        afterHooks[phaseType] = new List<object>();
                                    afterHooks[phaseType].Add(Activator.CreateInstance(type));
                                }
                            }
                            catch (Exception e)
                            {
                                Log.Warning($"[PhaseScanner] 创建钩子 {type.FullName} 失败: {e.Message}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[PhaseScanner] 扫描程序集 {assembly.GetName().Name} 失败: {e.Message}");
                }
            }

            // 排序
            foreach (var key in beforeHooks.Keys.ToList())
                beforeHooks[key] = beforeHooks[key].OrderBy(h => ((dynamic)h).Order).ToList();
            foreach (var key in afterHooks.Keys.ToList())
                afterHooks[key] = afterHooks[key].OrderBy(h => ((dynamic)h).Order).ToList();

            return (beforeHooks, afterHooks);
        }

        private Type[] GetTypesFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                foreach (var loaderException in e.LoaderExceptions)
                {
                    if (loaderException != null)
                        Log.Warning($"[PhaseScanner] 类型加载失败: {loaderException.Message}");
                }
                return e.Types.Where(t => t != null).ToArray();
            }
        }

        private IEnumerable<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => ShouldScanAssembly(a));
        }

        private bool ShouldScanAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;

            // 跳过 Unity 热重载产生的临时程序集
            if (name.Contains("-") && name.Length > 50)
                return false;

            if (_config.ExcludeAssemblyPrefixes.Any(p => name.StartsWith(p)))
                return false;

            if (_config.AssemblyPrefixes.Count > 0)
                return _config.AssemblyPrefixes.Any(p => name.StartsWith(p));

            return true;
        }
    }
}
