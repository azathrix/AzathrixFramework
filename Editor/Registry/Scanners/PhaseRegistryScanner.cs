using System;
using System.Collections.Generic;
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
            var newEntries = new List<PhaseEntry>();

            foreach (var assembly in ScannerHelper.GetAssemblies(config))
            {
                foreach (var type in ScannerHelper.GetTypes(assembly))
                {
                    if (!typeof(IStartupPhase).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                        continue;

                    try
                    {
                        var phase = (IStartupPhase)Activator.CreateInstance(type);

                        var entry = existingEntries.TryGetValue(type.FullName, out var existing)
                            ? existing
                            : new PhaseEntry { typeName = type.FullName, enabled = true };

                        entry.assemblyName = type.Assembly.GetName().Name;
                        entry.displayName = type.Name;
                        entry.phaseId = phase.Id;
                        entry.order = phase.Order;
                        entry.editorSupport = type.GetCustomAttribute<EditorSupportAttribute>() != null;
                        entry.editorOnly = type.GetCustomAttribute<EditorOnlyAttribute>() != null;

                        newEntries.Add(entry);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[PhaseRegistryScanner] 创建阶段 {type.FullName} 失败: {e.Message}");
                    }
                }
            }

            // 验证：如果扫描结果为空但原来有数据，跳过更新
            if (newEntries.Count == 0 && registry.entries.Count > 0)
            {
                Debug.LogWarning("[PhaseRegistryScanner] 扫描结果为空，跳过更新以保护现有数据");
                return;
            }

            registry.entries.Clear();
            registry.entries.AddRange(newEntries);
            EditorUtility.SetDirty(registry);
        }
    }
}
