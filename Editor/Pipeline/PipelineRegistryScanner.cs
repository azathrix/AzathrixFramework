using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Editor.Launcher;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Azathrix.Framework.Editor.Pipeline
{
    /// <summary>
    /// 管线注册表扫描器
    /// </summary>
    public static class PipelineRegistryScanner
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += ScanAll;
        }

        [MenuItem("Azathrix/注册表/扫描管线")]
        public static void ScanAll()
        {
#if UNITY_EDITOR
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
#endif
            var registry = PipelineRegistry.Instance;
            if (registry == null) return;

            var assemblies = CollectTargetAssemblies();

            // 扫描所有管线
            ScanPipelines(registry, assemblies);

            // 扫描所有阶段和钩子
            ScanPhasesAndHooks(registry, assemblies);

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }

        private static System.Reflection.Assembly[] CollectTargetAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .ToArray();

            try
            {
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    var assetsRoot = $"{projectRoot}/Assets";
                    var packagesRoot = $"{projectRoot}/Packages";
                    var packageCacheRoot = $"{projectRoot}/Library/PackageCache";

                    foreach (var asm in CompilationPipeline.GetAssemblies())
                    {
                        if (asm.sourceFiles == null || asm.sourceFiles.Length == 0)
                            continue;

                        var include = asm.sourceFiles.Any(path =>
                        {
                            var normalized = path.Replace('\\', '/');
                            if (normalized.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
                                return true;
                            if (normalized.StartsWith(packagesRoot, StringComparison.OrdinalIgnoreCase) &&
                                !normalized.StartsWith($"{packagesRoot}/com.unity.", StringComparison.OrdinalIgnoreCase))
                                return true;
                            if (normalized.StartsWith(packageCacheRoot, StringComparison.OrdinalIgnoreCase) &&
                                !normalized.StartsWith($"{packageCacheRoot}/com.unity.", StringComparison.OrdinalIgnoreCase))
                                return true;
                            return false;
                        });

                        if (include)
                            allowed.Add(asm.name);
                    }
                }

                if (allowed.Count > 0)
                {
                    assemblies = assemblies
                        .Where(a => allowed.Contains(a.GetName().Name))
                        .ToArray();
                }
                else
                {
                    assemblies = assemblies
                        .Where(a => !a.FullName.StartsWith("Unity") && !a.FullName.StartsWith("System"))
                        .ToArray();
                }
            }
            catch
            {
                assemblies = assemblies
                    .Where(a => !a.FullName.StartsWith("Unity") && !a.FullName.StartsWith("System"))
                    .ToArray();
            }

            return assemblies;
        }

        private static void ScanPipelines(PipelineRegistry registry, System.Reflection.Assembly[] assemblies)
        {
            var pipelineTypes = new List<Type>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface) continue;
                        if (!typeof(IPipeline).IsAssignableFrom(type)) continue;
                        pipelineTypes.Add(type);
                    }
                }
                catch { }
            }

            foreach (var type in pipelineTypes)
            {
                var pipelineId = PipelineReflection.GetPipelineId(type);
                var displayName = PipelineReflection.GetPipelineDisplayName(type, pipelineId);

                var entry = registry.GetOrCreatePipeline(pipelineId, displayName);
                entry.displayName = displayName;
                entry.pipelineTypeName = type.FullName;
                entry.pipelineAssembly = type.Assembly.GetName().Name;
            }
        }

        private static void ScanPhasesAndHooks(PipelineRegistry registry, System.Reflection.Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface) continue;

                        // 扫描阶段
                        ScanPhase(registry, type, assembly);

                        // 扫描钩子
                        ScanHook(registry, type, assembly);
                    }
                }
                catch { }
            }
        }

        private static void ScanPhase(PipelineRegistry registry, Type type, System.Reflection.Assembly assembly)
        {
            // 检查是否实现了 IPhase 接口
            var phaseInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPhase<>));

            if (phaseInterface == null) return;

            // 获取管线ID
            var pipelineId = GetPipelineIdForPhase(type);
            if (string.IsNullOrEmpty(pipelineId)) return;

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
                    existing.defaultOrder = defaultOrder;
                    if (!existing.hasCustomOrder)
                        existing.order = defaultOrder;
                    existing.interfaceTypeName = specificInterface?.FullName;
                    existing.phaseId = phaseId;
                    existing.displayName = displayName;
                    return;
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
                    enabled = true
                });
            }
            catch { }
        }

        private static void ScanHook(PipelineRegistry registry, Type type, System.Reflection.Assembly assembly)
        {
            var isBefore = ImplementsBeforeHook(type);
            var isAfter = ImplementsAfterHook(type);

            if (!isBefore && !isAfter)
                return;

            var hookTargets = type.GetCustomAttributes<HookTargetAttribute>(true).ToList();
            var pipelineTargets = new Dictionary<string, List<HookTargetEntry>>();

            if (hookTargets.Count > 0)
            {
                foreach (var group in hookTargets.GroupBy(t => t.PipelineId))
                {
                    if (string.IsNullOrEmpty(group.Key)) continue;
                    var targets = group
                        .Where(t => !string.IsNullOrEmpty(t.PhaseId))
                        .Select(t => new HookTargetEntry { phaseId = t.PhaseId })
                        .ToList();
                    if (targets.Count > 0)
                        pipelineTargets[group.Key] = targets;
                }
            }

            if (pipelineTargets.Count == 0)
                return;

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

            foreach (var kvp in pipelineTargets)
            {
                var pipelineId = kvp.Key;
                var targets = kvp.Value;
                var pipeline = registry.GetOrCreatePipeline(pipelineId);

                if (isBefore)
                    UpsertHookEntry(pipeline, type, assembly, defaultOrder, targets, true);
                if (isAfter)
                    UpsertHookEntry(pipeline, type, assembly, defaultOrder, targets, false);
            }
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

        private static void UpsertHookEntry(PipelineEntry pipeline, Type type, System.Reflection.Assembly assembly, int defaultOrder, List<HookTargetEntry> targets, bool isBefore)
        {
            var existing = pipeline.hooks.FirstOrDefault(h => h.typeName == type.FullName && h.isBefore == isBefore);
            if (existing != null)
            {
                existing.defaultOrder = defaultOrder;
                if (!existing.hasCustomOrder)
                    existing.order = defaultOrder;
                existing.targets = targets;
                existing.displayName = type.Name;
                return;
            }

            pipeline.hooks.Add(new HookEntry
            {
                typeName = type.FullName,
                assemblyName = assembly.GetName().Name,
                displayName = type.Name,
                targets = targets,
                order = defaultOrder,
                defaultOrder = defaultOrder,
                enabled = true,
                isBefore = isBefore
            });
        }
    }
}
