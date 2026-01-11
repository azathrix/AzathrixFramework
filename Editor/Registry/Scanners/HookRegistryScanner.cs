using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Core.Startup;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Registry;
using Azathrix.Framework.Settings;
using UnityEditor;
using UnityEngine;

namespace Azathrix.Framework.Editor.Registry
{
    public static class HookRegistryScanner
    {
        public static void Scan()
        {
            var registry = StartupHookRegistry.Instance;
            if (registry == null)
            {
                Debug.LogWarning("[HookRegistryScanner] HookRegistry 未找到");
                return;
            }

            var config = AzathrixFrameworkSettings.Instance?.ToScannerConfig();
            var existingEntries = registry.entries.ToDictionary(e => e.typeName);
            var newEntries = new List<StartupHookEntry>();

            foreach (var assembly in ScannerHelper.GetAssemblies(config))
            {
                foreach (var type in ScannerHelper.GetTypes(assembly))
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    foreach (var iface in type.GetInterfaces())
                    {
                        if (!iface.IsGenericType)
                            continue;

                        var genericDef = iface.GetGenericTypeDefinition();
                        bool isBefore = genericDef == typeof(IBeforePhaseHook<>);
                        bool isAfter = genericDef == typeof(IAfterPhaseHook<>);

                        if (!isBefore && !isAfter)
                            continue;

                        try
                        {
                            var phaseType = iface.GetGenericArguments()[0];
                            var hook = Activator.CreateInstance(type);
                            var order = ((dynamic)hook).Order;

                            var entry = existingEntries.TryGetValue(type.FullName, out var existing)
                                ? existing
                                : new StartupHookEntry { typeName = type.FullName, enabled = true };

                            entry.assemblyName = type.Assembly.GetName().Name;
                            entry.displayName = type.Name;
                            entry.order = order;
                            entry.targetPhaseType = phaseType.FullName;
                            entry.isBefore = isBefore;

                            newEntries.Add(entry);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[HookRegistryScanner] 创建钩子 {type.FullName} 失败: {e.Message}");
                        }
                    }
                }
            }

            // 验证：如果扫描结果为空但原来有数据，跳过更新
            if (newEntries.Count == 0 && registry.entries.Count > 0)
            {
                Debug.LogWarning("[HookRegistryScanner] 扫描结果为空，跳过更新以保护现有数据");
                return;
            }

            registry.entries.Clear();
            registry.entries.AddRange(newEntries);
            EditorUtility.SetDirty(registry);
        }
    }
}
