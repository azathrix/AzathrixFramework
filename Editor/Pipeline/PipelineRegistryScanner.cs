using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Editor.Launcher;
using UnityEditor;
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
            var registry = PipelineRegistry.Instance;
            if (registry == null) return;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !a.FullName.StartsWith("Unity") && !a.FullName.StartsWith("System"))
                .ToArray();

            // 扫描所有管线
            ScanPipelines(registry, assemblies);

            // 扫描所有阶段和钩子
            ScanPhasesAndHooks(registry, assemblies);

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }

        private static void ScanPipelines(PipelineRegistry registry, Assembly[] assemblies)
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
                var idAttr = type.GetCustomAttribute<PipelineIdAttribute>();
                var nameAttr = type.GetCustomAttribute<PipelineDisplayNameAttribute>();

                var pipelineId = idAttr?.Id ?? type.Name;
                var displayName = nameAttr?.DisplayName ?? pipelineId;

                var entry = registry.GetOrCreatePipeline(pipelineId, displayName);
                entry.pipelineTypeName = type.FullName;
                entry.pipelineAssembly = type.Assembly.GetName().Name;
            }
        }

        private static void ScanPhasesAndHooks(PipelineRegistry registry, Assembly[] assemblies)
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

        private static void ScanPhase(PipelineRegistry registry, Type type, Assembly assembly)
        {
            // 检查是否实现了 IPhase 接口
            var phaseInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPhase<>));

            if (phaseInterface == null) return;

            // 获取管线ID
            var pipelineId = GetPipelineIdForPhase(type);
            if (string.IsNullOrEmpty(pipelineId)) return;

            var pipeline = registry.GetOrCreatePipeline(pipelineId);

            // 创建实例获取属性
            try
            {
                var instance = Activator.CreateInstance(type);
                var idProp = type.GetProperty("Id");
                var orderProp = type.GetProperty("Order");

                var phaseId = idProp?.GetValue(instance)?.ToString() ?? type.Name;
                var defaultOrder = (int)(orderProp?.GetValue(instance) ?? 0);

                // 检查是否已存在
                var existing = pipeline.phases.FirstOrDefault(p => p.typeName == type.FullName);
                if (existing != null)
                {
                    // 更新默认值
                    existing.defaultOrder = defaultOrder;
                    return;
                }

                pipeline.phases.Add(new PhaseEntry
                {
                    typeName = type.FullName,
                    assemblyName = assembly.GetName().Name,
                    displayName = type.Name,
                    phaseId = phaseId,
                    order = defaultOrder,
                    defaultOrder = defaultOrder,
                    enabled = true
                });
            }
            catch { }
        }

        private static void ScanHook(PipelineRegistry registry, Type type, Assembly assembly)
        {
            // 检查是否实现了 IHook 接口
            var hookInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHook<>));

            if (hookInterface == null) return;

            // 获取管线ID
            var pipelineId = GetPipelineIdForHook(type);
            if (string.IsNullOrEmpty(pipelineId)) return;

            var pipeline = registry.GetOrCreatePipeline(pipelineId);

            try
            {
                var instance = Activator.CreateInstance(type);
                var orderProp = type.GetProperty("Order");
                var defaultOrder = (int)(orderProp?.GetValue(instance) ?? 0);

                // 获取目标阶段
                var targetPhase = GetTargetPhaseForHook(type);

                // 检查是否已存在
                var existing = pipeline.hooks.FirstOrDefault(h => h.typeName == type.FullName);
                if (existing != null)
                {
                    existing.defaultOrder = defaultOrder;
                    return;
                }

                pipeline.hooks.Add(new HookEntry
                {
                    typeName = type.FullName,
                    assemblyName = assembly.GetName().Name,
                    displayName = type.Name,
                    targetPhase = targetPhase?.FullName ?? "",
                    targetPhaseAssembly = targetPhase?.Assembly.GetName().Name ?? "",
                    order = defaultOrder,
                    defaultOrder = defaultOrder,
                    enabled = true,
                    isBefore = true
                });
            }
            catch { }
        }

        private static string GetPipelineIdForPhase(Type phaseType)
        {
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

        private static string GetPipelineIdForHook(Type hookType)
        {
            // 检查 ILauncherHook -> Launcher
            if (typeof(ILauncherHook).IsAssignableFrom(hookType))
                return "Launcher";

            // 检查 IEditorLauncherHook -> EditorLauncher
            if (typeof(IEditorLauncherHook).IsAssignableFrom(hookType))
                return "EditorLauncher";

            if (hookType.Namespace?.Contains("Azcel") == true)
                return "Azcel.Converter";

            return null;
        }

        private static Type GetTargetPhaseForHook(Type hookType)
        {
            // 从泛型参数获取目标阶段类型
            var interfaces = hookType.GetInterfaces();
            foreach (var iface in interfaces)
            {
                if (!iface.IsGenericType) continue;

                var genericArgs = iface.GetGenericArguments();
                foreach (var arg in genericArgs)
                {
                    if (typeof(IPhase).IsAssignableFrom(arg))
                        return arg;
                }
            }
            return null;
        }
    }
}
