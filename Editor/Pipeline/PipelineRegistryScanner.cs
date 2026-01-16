using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Editor.Launcher;
using Azathrix.Framework.Editor.Registry;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Azathrix.Framework.Editor.Pipeline
{
    /// <summary>
    /// 管线注册表扫描器
    /// </summary>
    public static class PipelineRegistryScanner
    {
       // [MenuItem("Azathrix/注册表/扫描管线")]
        public static void ScanAll()
        {
            ScanAllInternal();
        }

        private static void ScanAllInternal()
        {
#if UNITY_EDITOR
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
#endif
            var watch = Stopwatch.StartNew();
            var registry = PipelineRegistry.Instance;
            if (registry == null) return;

            var assemblies = ScannerHelper.GetAssemblies().ToArray();
            var collectMs = watch.Elapsed.TotalMilliseconds;

            var changed = false;

            changed |= NormalizeHookTargets(registry);
            var normalizeMs = watch.Elapsed.TotalMilliseconds;

            // 扫描所有管线
            changed |= ScanPipelines(registry, assemblies);
            var pipelinesMs = watch.Elapsed.TotalMilliseconds;

            // 扫描所有阶段和钩子
            changed |= ScanPhasesAndHooks(registry, assemblies);
            var phasesHooksMs = watch.Elapsed.TotalMilliseconds;

            changed |= CleanupUnregisteredEntries(registry);
            changed |= CleanupMissingEntries(registry);
            changed |= CleanupEmptyTargets(registry);
            changed |= CleanupOrphanHooks(registry);
            var cleanupMs = watch.Elapsed.TotalMilliseconds;

            var saveMs = 0d;
            if (changed)
            {
                var saveWatch = Stopwatch.StartNew();
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
                saveWatch.Stop();
                saveMs = saveWatch.Elapsed.TotalMilliseconds;
            }

            watch.Stop();
            Debug.Log($"[PipelineRegistry] 刷新完成，耗时 {watch.Elapsed.TotalMilliseconds:F2}ms，扫描程序集 {assemblies.Length} 个" +
                      $" (Collect {collectMs:F2}ms, Normalize {normalizeMs - collectMs:F2}ms, Pipelines {pipelinesMs - normalizeMs:F2}ms," +
                      $" Phases+Hooks {phasesHooksMs - pipelinesMs:F2}ms, Cleanup {cleanupMs - phasesHooksMs:F2}ms, Save {saveMs:F2}ms)");
        }

        private static bool CleanupEmptyTargets(PipelineRegistry registry)
        {
            var changed = false;
            foreach (var pipeline in registry.pipelines)
            {
                var removed = pipeline.hooks.RemoveAll(h =>
                    h.targets == null ||
                    h.targets.Count == 0 ||
                    h.targets.Any(t => string.IsNullOrEmpty(t.phaseId)));
                if (removed > 0)
                    changed = true;
            }
            return changed;
        }

        private static bool CleanupUnregisteredEntries(PipelineRegistry registry)
        {
            var removedAny = false;
            foreach (var pipeline in registry.pipelines.ToList())
            {
                var pipelineType = pipeline.GetPipelineType();
                if (pipelineType != null && pipelineType.GetCustomAttribute<RegisterAttribute>() == null)
                {
                    registry.pipelines.Remove(pipeline);
                    removedAny = true;
                    continue;
                }

                if (pipeline.phases.RemoveAll(p =>
                        p.GetRuntimeType() != null && p.GetRuntimeType().GetCustomAttribute<RegisterAttribute>() == null) > 0)
                    removedAny = true;

                if (pipeline.hooks.RemoveAll(h =>
                        h.GetRuntimeType() != null && h.GetRuntimeType().GetCustomAttribute<RegisterAttribute>() == null) > 0)
                    removedAny = true;
            }

            if (removedAny)
                registry.ClearCache();
            return removedAny;
        }

        private static bool CleanupMissingEntries(PipelineRegistry registry)
        {
            var removedAny = false;
            foreach (var pipeline in registry.pipelines.ToList())
            {
                if (pipeline.phases.RemoveAll(p => p.IsMissing) > 0)
                    removedAny = true;
                if (pipeline.hooks.RemoveAll(h => h.IsMissing) > 0)
                    removedAny = true;

                if (pipeline.GetPipelineType() == null && pipeline.phases.Count == 0 && pipeline.hooks.Count == 0)
                {
                    registry.pipelines.Remove(pipeline);
                    removedAny = true;
                }
            }

            if (removedAny)
                registry.ClearCache();
            return removedAny;
        }

        private static bool CleanupOrphanHooks(PipelineRegistry registry)
        {
            var changed = false;
            foreach (var pipeline in registry.pipelines)
            {
                var phaseIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var phase in pipeline.phases)
                {
                    if (!string.IsNullOrEmpty(phase.phaseId))
                    {
                        phaseIds.Add(phase.phaseId);
                        continue;
                    }

                    var type = phase.GetRuntimeType();
                    if (type == null) continue;

                    var computed = PipelineReflection.GetPhaseId(type);
                    if (!string.IsNullOrEmpty(computed))
                    {
                        phase.phaseId = computed;
                        phaseIds.Add(computed);
                    }
                }

                var removed = pipeline.hooks.RemoveAll(h =>
                    h.targets == null ||
                    h.targets.Count != 1 ||
                    string.IsNullOrEmpty(h.targets[0].phaseId) ||
                    !phaseIds.Contains(h.targets[0].phaseId));
                if (removed > 0)
                    changed = true;
            }
            return changed;
        }

        private static bool NormalizeHookTargets(PipelineRegistry registry)
        {
            var changed = false;
            foreach (var pipeline in registry.pipelines)
            {
                var expanded = new List<HookEntry>();
                var pipelineChanged = false;

                foreach (var hook in pipeline.hooks)
                {
                    if (hook.targets == null || hook.targets.Count <= 1)
                    {
                        expanded.Add(hook);
                        continue;
                    }

                    pipelineChanged = true;
                    foreach (var target in hook.targets)
                    {
                        if (string.IsNullOrEmpty(target.phaseId))
                            continue;

                        expanded.Add(new HookEntry
                        {
                            typeName = hook.typeName,
                            assemblyName = hook.assemblyName,
                            displayName = hook.displayName,
                            targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = target.phaseId } },
                            order = hook.order,
                            defaultOrder = hook.defaultOrder,
                            hasCustomOrder = hook.hasCustomOrder,
                            enabled = hook.enabled,
                            isBefore = hook.isBefore,
                            isAuto = hook.isAuto
                        });
                    }
                }

                if (pipelineChanged)
                {
                    pipeline.hooks = expanded;
                    changed = true;
                }
            }
            return changed;
        }

        private static bool ScanPipelines(PipelineRegistry registry, System.Reflection.Assembly[] assemblies)
        {
            var pipelineTypes = new List<Type>();
            var changed = false;

            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface) continue;
                        if (!typeof(IPipeline).IsAssignableFrom(type)) continue;
                        if (type.GetCustomAttribute<RegisterAttribute>() == null) continue;
                        pipelineTypes.Add(type);
                    }
                }
                catch { }
            }

            foreach (var type in pipelineTypes)
            {
                var pipelineId = PipelineReflection.GetPipelineId(type);
                var displayName = PipelineReflection.GetPipelineDisplayName(type, pipelineId);

                var existed = registry.GetPipeline(pipelineId) != null;
                var entry = registry.GetOrCreatePipeline(pipelineId, displayName);
                if (!existed)
                    changed = true;

                if (entry.displayName != displayName)
                {
                    entry.displayName = displayName;
                    changed = true;
                }
                if (entry.pipelineTypeName != type.FullName)
                {
                    entry.pipelineTypeName = type.FullName;
                    changed = true;
                }
                var asmName = type.Assembly.GetName().Name;
                if (entry.pipelineAssembly != asmName)
                {
                    entry.pipelineAssembly = asmName;
                    changed = true;
                }
            }
            return changed;
        }

        private static bool ScanPhasesAndHooks(PipelineRegistry registry, System.Reflection.Assembly[] assemblies)
        {
            var changed = false;
            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface) continue;

                        // 扫描阶段
                        if (ScanPhase(registry, type, assembly))
                            changed = true;

                        // 扫描钩子
                        if (ScanHook(registry, type, assembly))
                            changed = true;
                    }
                }
                catch { }
            }
            return changed;
        }

        private static bool ScanPhase(PipelineRegistry registry, Type type, System.Reflection.Assembly assembly)
        {
            var autoAttr = type.GetCustomAttribute<RegisterAttribute>();
            if (autoAttr == null)
                return false;

            // 检查是否实现了 IPhase 接口
            var phaseInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPhase<>));

            if (phaseInterface == null) return false;

            // 获取管线ID
            var pipelineId = string.IsNullOrEmpty(autoAttr.PipelineId)
                ? GetPipelineIdForPhase(type)
                : autoAttr.PipelineId;
            if (string.IsNullOrEmpty(pipelineId)) return false;

            var pipeline = registry.GetOrCreatePipeline(pipelineId);

            // 获取特定的阶段接口（如 IStartPhase, ISetupPhase 等）
            var specificInterface = GetSpecificPhaseInterface(type);

            // 创建实例获取属性
            try
            {
                var instance = Activator.CreateInstance(type);
                var phaseId = PipelineReflection.GetPhaseId(type);
                var displayName = PipelineReflection.GetPhaseDisplayName(type, phaseId);
                var defaultOrder = PipelineReflection.GetOrder(instance);

                // 检查是否已存在
                var existing = pipeline.phases.FirstOrDefault(p => p.typeName == type.FullName);
                if (existing != null)
                {
                    // 更新默认值
                    var changed = false;
                    if (existing.defaultOrder != defaultOrder)
                    {
                        existing.defaultOrder = defaultOrder;
                        changed = true;
                    }
                    if (!existing.hasCustomOrder && existing.order != defaultOrder)
                    {
                        existing.order = defaultOrder;
                        changed = true;
                    }
                    var ifaceName = specificInterface?.FullName;
                    if (existing.interfaceTypeName != ifaceName)
                    {
                        existing.interfaceTypeName = ifaceName;
                        changed = true;
                    }
                    if (existing.phaseId != phaseId)
                    {
                        existing.phaseId = phaseId;
                        changed = true;
                    }
                    if (existing.displayName != displayName)
                    {
                        existing.displayName = displayName;
                        changed = true;
                    }
                    if (!existing.isAuto)
                    {
                        existing.isAuto = true;
                        changed = true;
                    }
                    return changed;
                }

                pipeline.phases.Add(new PhaseEntry
                {
                    typeName = type.FullName,
                    assemblyName = assembly.GetName().Name,
                    displayName = displayName,
                    phaseId = phaseId,
                    interfaceTypeName = specificInterface?.FullName,
                    order = defaultOrder,
                    defaultOrder = defaultOrder,
                    enabled = true,
                    isAuto = true
                });
                return true;
            }
            catch { }
            return false;
        }

        private static bool ScanHook(PipelineRegistry registry, Type type, System.Reflection.Assembly assembly)
        {
            var autoAttr = type.GetCustomAttribute<RegisterAttribute>();
            if (autoAttr == null)
                return false;

            var isBefore = ImplementsBeforeHook(type);
            var isAfter = ImplementsAfterHook(type);

            if (!isBefore && !isAfter)
                return false;

            var hookTargets = type.GetCustomAttributes<HookTargetAttribute>(true)
                .Where(t => !string.IsNullOrEmpty(t.PipelineId))
                .Where(t => !string.IsNullOrEmpty(t.PhaseId))
                .ToList();

            if (hookTargets.Count == 0)
                return false;

            int defaultOrder;
            try
            {
                var instance = Activator.CreateInstance(type);
                defaultOrder = PipelineReflection.GetOrder(instance);
            }
            catch
            {
                defaultOrder = 0;
            }

            var changed = false;
            foreach (var target in hookTargets)
            {
                var pipeline = registry.GetOrCreatePipeline(target.PipelineId);

                if (isBefore)
                    changed |= UpsertHookEntry(pipeline, type, assembly, defaultOrder, target.PhaseId, true);
                if (isAfter)
                    changed |= UpsertHookEntry(pipeline, type, assembly, defaultOrder, target.PhaseId, false);
            }
            return changed;
        }

        private static string GetPipelineIdForPhase(Type phaseType)
        {
            // 如果类型上有 PipelineId 特性，优先使用
            var idAttr = phaseType.GetCustomAttribute<PipelineIdAttribute>();
            if (idAttr != null)
                return idAttr.Id;

            // 检查 ILauncherPhase -> Launcher
            if (typeof(ILauncherPhase).IsAssignableFrom(phaseType))
                return "Launcher";

            // 检查 IEditorLauncherPhase -> EditorLauncher
            if (typeof(IEditorLauncherPhase).IsAssignableFrom(phaseType))
                return "EditorLauncher";

            // 检查 IConvertPhase -> Azcel.Converter (通过命名空间推断)
            if (phaseType.Namespace?.Contains("Azcel") == true)
                return "Azcel.Converter";

            return null;
        }

        private static Type GetSpecificPhaseInterface(Type phaseType)
        {
            // 查找特定的阶段接口（如 IStartPhase, ISetupPhase）
            // 排除通用的 IPhase 和 IPhase<>
            foreach (var iface in phaseType.GetInterfaces())
            {
                if (iface == typeof(IPhase)) continue;
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IPhase<>)) continue;

                if (typeof(IPhase).IsAssignableFrom(iface))
                    return iface;
            }
            return null;
        }

        private static bool ImplementsBeforeHook(Type hookType)
        {
            if (typeof(IBeforePhaseHook).IsAssignableFrom(hookType))
                return true;
            return hookType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBeforePhaseHook<>));
        }

        private static bool ImplementsAfterHook(Type hookType)
        {
            if (typeof(IAfterPhaseHook).IsAssignableFrom(hookType))
                return true;
            return hookType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAfterPhaseHook<>));
        }

        private static bool UpsertHookEntry(PipelineEntry pipeline, Type type, System.Reflection.Assembly assembly, int defaultOrder, string phaseId, bool isBefore)
        {
            var existing = pipeline.hooks.FirstOrDefault(h =>
                h.typeName == type.FullName &&
                h.isBefore == isBefore &&
                h.targets != null &&
                h.targets.Count == 1 &&
                string.Equals(h.targets[0].phaseId, phaseId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                var changed = false;
                if (existing.defaultOrder != defaultOrder)
                {
                    existing.defaultOrder = defaultOrder;
                    changed = true;
                }
                if (!existing.hasCustomOrder && existing.order != defaultOrder)
                {
                    existing.order = defaultOrder;
                    changed = true;
                }
                if (existing.displayName != type.Name)
                {
                    existing.displayName = type.Name;
                    changed = true;
                }
                if (!existing.isAuto)
                {
                    existing.isAuto = true;
                    changed = true;
                }
                return changed;
            }

            pipeline.hooks.Add(new HookEntry
            {
                typeName = type.FullName,
                assemblyName = assembly.GetName().Name,
                displayName = type.Name,
                targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = phaseId } },
                order = defaultOrder,
                defaultOrder = defaultOrder,
                enabled = true,
                isBefore = isBefore,
                isAuto = true
            });
            return true;
        }
    }
}
