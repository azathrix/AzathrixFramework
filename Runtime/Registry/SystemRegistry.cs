using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Settings;
using UnityEngine;

namespace Azathrix.Framework.Registry
{
    [Serializable]
    public class SystemEntry : RegistryEntryBase
    {
        public bool isDefault;
        public int priority;
        public int defaultPriority;      // 从特性读取的默认值
        public bool hasCustomPriority;   // 是否使用自定义优先级
        public List<string> interfaces = new();
        public List<string> dependencies = new();

        public int EffectivePriority => hasCustomPriority ? priority : defaultPriority;
    }

    [Serializable]
    public class InterfaceEntry : RegistryEntryBase
    {
        public int priority;
        public int defaultPriority;
        public bool hasCustomPriority;
        public List<string> implementations = new();

        public int EffectivePriority => hasCustomPriority ? priority : defaultPriority;
    }

    [Serializable]
    public class InterfaceSelection
    {
        public string interfaceTypeName;
        public string selectedImplementation;
    }

    [SettingsName("SystemRegistry")]
    [ShowSetting("系统注册表")]
    public class SystemRegistry : RegistryBase<SystemRegistry, SystemEntry>
    {
        [Header("接口注册")]
        public List<InterfaceEntry> interfaceEntries = new();

        [Header("接口实现选择")]
        public List<InterfaceSelection> interfaceSelections = new();

        private Dictionary<string, string> _selectionCache;
        private Dictionary<string, InterfaceEntry> _interfaceCache;

        public InterfaceEntry GetInterfaceEntry(string typeName)
        {
            if (_interfaceCache == null) RebuildInterfaceCache();
            return _interfaceCache.TryGetValue(typeName, out var entry) ? entry : null;
        }

        public bool IsInterfaceEnabled(string typeName)
        {
            var entry = GetInterfaceEntry(typeName);
            return entry?.enabled ?? true;
        }

        public string GetSelectedImplementation(string interfaceTypeName)
        {
            if (_selectionCache == null) RebuildSelectionCache();
            return _selectionCache.TryGetValue(interfaceTypeName, out var impl) ? impl : null;
        }

        public List<SystemEntry> GetImplementations(string interfaceTypeName)
        {
            return entries.Where(e => e.interfaces.Contains(interfaceTypeName)).ToList();
        }

        public HashSet<string> GetDisabledSystems()
        {
            return new HashSet<string>(entries.Where(e => !e.enabled).Select(e => e.typeName));
        }

        public bool IsSystemDisabled(Type type)
        {
            return !IsEnabled(type);
        }

        public bool IsSystemDisabled(string typeName)
        {
            return !IsEnabled(typeName);
        }

        private void RebuildSelectionCache()
        {
            _selectionCache = new Dictionary<string, string>();
            foreach (var selection in interfaceSelections)
            {
                if (!string.IsNullOrEmpty(selection.interfaceTypeName) &&
                    !string.IsNullOrEmpty(selection.selectedImplementation))
                {
                    _selectionCache[selection.interfaceTypeName] = selection.selectedImplementation;
                }
            }
        }

        private void RebuildInterfaceCache()
        {
            _interfaceCache = new Dictionary<string, InterfaceEntry>();
            foreach (var entry in interfaceEntries)
            {
                if (!string.IsNullOrEmpty(entry.typeName))
                    _interfaceCache[entry.typeName] = entry;
            }
        }

        public void ClearSelectionCache() => _selectionCache = null;

        protected override void OnValidate()
        {
            base.OnValidate();
            _selectionCache = null;
            _interfaceCache = null;
        }
    }
}
