using System;
using System.Linq;
using System.Reflection;
using Azathrix.Framework.Core.Startup;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Registry;
using Azathrix.Framework.Settings;
using UnityEditor;
using UnityEngine;

namespace Azathrix.Framework.Editor.Registry
{
    public static class PhaseRegistryScanner
    {
        public static void Scan()
        {
            var registry = PhaseRegistry.Instance;
            if (registry == null)
            {
                Debug.LogWarning("[PhaseRegistryScanner] PhaseRegistry 未找到");
                return;
            }

            var config = AzathrixFrameworkSettings.Instance?.ToScannerConfig();
            var existingEntries = registry.entries.ToDictionary(e => e.typeName);
            registry.entries.Clear();

            int count = 0;
            foreach (var assembly in ScannerHelper.GetAssemblies(config))
            {
                foreach (var type in ScannerHelper.GetTypes(assembly))
                {
                    if (!typeof(IStartupPhase).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                        continue;

                    try
                    {
                        var phase = (IStartupPhase)Activator.CreateInstance(type);

                        // 保留已有条目的用户配置
                        var entry = existingEntries.TryGetValue(type.FullName, out var existing)
                            ? existing
                            : new PhaseEntry { typeName = type.FullName, enabled = true };

                        // 更新扫描信息
                        entry.assemblyName = type.Assembly.GetName().Name;
                        entry.displayName = type.Name;
                        entry.phaseId = phase.Id;
                        entry.order = phase.Order;
                        entry.editorSupport = type.GetCustomAttribute<EditorSupportAttribute>() != null;
                        entry.editorOnly = type.GetCustomAttribute<EditorOnlyAttribute>() != null;

                        registry.entries.Add(entry);
                        count++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[PhaseRegistryScanner] 创建阶段 {type.FullName} 失败: {e.Message}");
                    }
                }
            }

            EditorUtility.SetDirty(registry);
            // Debug.Log($"[PhaseRegistryScanner] 扫描完成，共 {count} 个阶段");
        }
    }
}
