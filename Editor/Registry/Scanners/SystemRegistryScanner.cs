using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Registry;
using Azathrix.Framework.Settings;
using UnityEditor;
using UnityEngine;

namespace Azathrix.Framework.Editor.Registry
{
    public static class SystemRegistryScanner
    {
        public static void Scan()
        {
            var registry = SystemRegistry.Instance;
            if (registry == null)
            {
                Debug.LogWarning("[SystemRegistryScanner] SystemRegistry 未找到");
                return;
            }

            var config = AzathrixFrameworkSettings.Instance?.ToScannerConfig();
            var existingEntries = registry.entries.ToDictionary(e => e.typeName);
            var existingInterfaces = registry.interfaceEntries.ToDictionary(e => e.typeName);
            registry.entries.Clear();
            registry.interfaceEntries.Clear();

            var interfaceImplementations = new Dictionary<string, List<string>>();

            foreach (var assembly in ScannerHelper.GetAssemblies(config))
            {
                foreach (var type in ScannerHelper.GetTypes(assembly))
                {
                    // 扫描接口
                    if (type.IsInterface && typeof(ISystem).IsAssignableFrom(type) && type != typeof(ISystem))
                    {
                        var ifaceEntry = existingInterfaces.TryGetValue(type.FullName, out var existingIface)
                            ? existingIface
                            : new InterfaceEntry { typeName = type.FullName, enabled = true };

                        ifaceEntry.assemblyName = type.Assembly.GetName().Name;
                        ifaceEntry.displayName = type.Name;
                        ifaceEntry.implementations.Clear();

                        // 读取接口的默认优先级
                        var ifacePriorityAttr = type.GetCustomAttribute<SystemPriorityAttribute>();
                        ifaceEntry.defaultPriority = ifacePriorityAttr?.Priority ?? 0;

                        registry.interfaceEntries.Add(ifaceEntry);
                        continue;
                    }

                    // 扫描系统类
                    if (!typeof(ISystem).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                        continue;

                    var entry = existingEntries.TryGetValue(type.FullName, out var existing)
                        ? existing
                        : new SystemEntry { typeName = type.FullName, enabled = true };

                    entry.assemblyName = type.Assembly.GetName().Name;
                    entry.displayName = type.Name;
                    entry.isDefault = type.GetCustomAttribute<DefaultAttribute>() != null;

                    // 读取系统的默认优先级
                    var priorityAttr = type.GetCustomAttribute<SystemPriorityAttribute>();
                    entry.defaultPriority = priorityAttr?.Priority ?? 0;

                    entry.interfaces = type.GetInterfaces()
                        .Where(i => typeof(ISystem).IsAssignableFrom(i) && i != typeof(ISystem))
                        .Select(i => i.FullName)
                        .ToList();

                    entry.dependencies = type.GetCustomAttributes<RequireSystemAttribute>()
                        .Select(d => d.DependencyType.FullName)
                        .ToList();

                    registry.entries.Add(entry);

                    // 记录接口实现
                    foreach (var iface in entry.interfaces)
                    {
                        if (!interfaceImplementations.ContainsKey(iface))
                            interfaceImplementations[iface] = new List<string>();
                        interfaceImplementations[iface].Add(type.FullName);
                    }
                }
            }

            // 更新接口的实现列表
            foreach (var ifaceEntry in registry.interfaceEntries)
            {
                if (interfaceImplementations.TryGetValue(ifaceEntry.typeName, out var impls))
                    ifaceEntry.implementations = impls;
            }

            EditorUtility.SetDirty(registry);
        }
    }
}
