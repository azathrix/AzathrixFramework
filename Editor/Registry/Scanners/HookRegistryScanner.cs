using System;
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
            registry.entries.Clear();

            int count = 0;
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

                            // 保留已有条目的用户配置
                            var entry = existingEntries.TryGetValue(type.FullName, out var existing)
                                ? existing
                                : new StartupHookEntry { typeName = type.FullName, enabled = true };

                            // 更新扫描信息
                            entry.assemblyName = type.Assembly.GetName().Name;
                            entry.displayName = type.Name;
                            entry.order = order;
                            entry.targetPhaseType = phaseType.FullName;
                            entry.isBefore = isBefore;

                            registry.entries.Add(entry);
                            count++;
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[HookRegistryScanner] 创建钩子 {type.FullName} 失败: {e.Message}");
                        }
                    }
                }
            }

            EditorUtility.SetDirty(registry);
            // Debug.Log($"[HookRegistryScanner] 扫描完成，共 {count} 个钩子");
        }
    }
}
