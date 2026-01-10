using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Settings;

namespace Azathrix.Framework.Registry
{
    public abstract class RegistryBase<T, TEntry> : SettingsBase<T>
        where T : RegistryBase<T, TEntry>
        where TEntry : RegistryEntryBase
    {
        public List<TEntry> entries = new();

        private Dictionary<string, TEntry> _cache;

        public TEntry GetEntry(string typeName)
        {
            if (_cache == null) RebuildCache();
            return _cache.TryGetValue(typeName, out var entry) ? entry : null;
        }

        public List<TEntry> GetEnabledEntries()
        {
            return entries.Where(e => e.enabled).ToList();
        }

        public Type[] GetEnabledTypes()
        {
            return GetEnabledEntries()
                .Select(e => e.GetRuntimeType())
                .Where(t => t != null)
                .ToArray();
        }

        public bool IsEnabled(string typeName)
        {
            var entry = GetEntry(typeName);
            return entry?.enabled ?? true;
        }

        public bool IsEnabled(Type type)
        {
            return IsEnabled(type.FullName);
        }

        protected void RebuildCache()
        {
            _cache = new Dictionary<string, TEntry>();
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.typeName))
                    _cache[entry.typeName] = entry;
            }
        }

        public void ClearCache() => _cache = null;

        protected virtual void OnValidate()
        {
            ClearCache();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += NotifyChanged;
#endif
        }

#if UNITY_EDITOR
        public static event Action OnRegistryChanged;
        protected static void NotifyChanged() => OnRegistryChanged?.Invoke();
#endif
    }
}
