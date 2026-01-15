using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Settings;
using UnityEngine.Serialization;
using UnityEngine;

namespace Azathrix.Framework.Core.Pipeline
{
    /// <summary>
    /// 管线注册表 - 统一管理所有管线的阶段和钩子
    /// </summary>
    [SettingsPath("PipelineRegistry")]
    [ShowSetting("管线注册表")]
    public class PipelineRegistry : SettingsBase<PipelineRegistry>
    {
        [SerializeField]
        public List<PipelineEntry> pipelines = new();

        private Dictionary<string, PipelineEntry> _cache;

#if UNITY_EDITOR
        public static event Action OnRegistryChanged;
#endif

        public PipelineEntry GetPipeline(string pipelineId)
        {
            MigrateLegacyTargets();
            if (_cache == null) RebuildCache();
            return _cache.TryGetValue(pipelineId, out var entry) ? entry : null;
        }

        public PipelineEntry GetOrCreatePipeline(string pipelineId, string displayName = null)
        {
            MigrateLegacyTargets();
            var entry = GetPipeline(pipelineId);
            if (entry == null)
            {
                entry = new PipelineEntry
                {
                    pipelineId = pipelineId,
                    displayName = displayName ?? pipelineId
                };
                pipelines.Add(entry);
                _cache[pipelineId] = entry;
            }
            return entry;
        }

        public bool IsPhaseEnabled(string pipelineId, string phaseTypeName)
        {
            var pipeline = GetPipeline(pipelineId);
            if (pipeline == null) return true;
            var phase = pipeline.phases.Find(p => p.typeName == phaseTypeName);
            return phase?.enabled ?? true;
        }

        public bool IsHookEnabled(string pipelineId, string hookTypeName)
        {
            var pipeline = GetPipeline(pipelineId);
            if (pipeline == null) return true;
            var hook = pipeline.hooks.Find(h => h.typeName == hookTypeName);
            return hook?.enabled ?? true;
        }

        public List<PhaseEntry> GetOrderedPhases(string pipelineId)
        {
            var pipeline = GetPipeline(pipelineId);
            if (pipeline == null) return new List<PhaseEntry>();

            return pipeline.phases.Where(e => e.enabled).OrderBy(e => e.order).ToList();
        }

        public Type[] GetOrderedPhaseTypes(string pipelineId)
        {
            return GetOrderedPhases(pipelineId)
                .Select(e => e.GetRuntimeType())
                .Where(t => t != null)
                .ToArray();
        }

        public List<HookEntry> GetHooksForPhase(string pipelineId, string phaseTypeName, bool before)
        {
            var pipeline = GetPipeline(pipelineId);
            if (pipeline == null) return new List<HookEntry>();

            return pipeline.hooks
                .Where(e => e.enabled && e.isBefore == before && e.TargetsMatch(phaseTypeName))
                .OrderBy(e => e.order)
                .ToList();
        }

        public Type[] GetBeforeHookTypes(string pipelineId, string phaseTypeName)
        {
            return GetHooksForPhase(pipelineId, phaseTypeName, true)
                .Select(e => e.GetRuntimeType())
                .Where(t => t != null)
                .ToArray();
        }

        public Type[] GetAfterHookTypes(string pipelineId, string phaseTypeName)
        {
            return GetHooksForPhase(pipelineId, phaseTypeName, false)
                .Select(e => e.GetRuntimeType())
                .Where(t => t != null)
                .ToArray();
        }

        private void RebuildCache()
        {
            MigrateLegacyTargets();
            _cache = new Dictionary<string, PipelineEntry>();
            foreach (var entry in pipelines)
            {
                if (!string.IsNullOrEmpty(entry.pipelineId))
                    _cache[entry.pipelineId] = entry;
            }
        }

        public void ClearCache() => _cache = null;

        protected virtual void OnValidate()
        {
            ClearCache();
            MigrateLegacyTargets();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => OnRegistryChanged?.Invoke();
#endif
        }

        private void MigrateLegacyTargets()
        {
            foreach (var pipeline in pipelines)
            {
                foreach (var hook in pipeline.hooks)
                    hook.MigrateLegacyTargets();
            }
        }
    }

    [Serializable]
    public class PipelineEntry
    {
        public string pipelineId;
        public string displayName;
        public string pipelineTypeName;
        public string pipelineAssembly;
        public List<PhaseEntry> phases = new();
        public List<HookEntry> hooks = new();

        public Type GetPipelineType()
        {
            if (string.IsNullOrEmpty(pipelineTypeName) || string.IsNullOrEmpty(pipelineAssembly))
                return null;
            return Type.GetType($"{pipelineTypeName}, {pipelineAssembly}");
        }
    }

    [Serializable]
    public class PhaseEntry
    {
        public string typeName;
        public string assemblyName;
        public string displayName;
        public string phaseId;
        public string interfaceTypeName; // 实现的阶段接口（如 IStartPhase）
        public int order;
        public int defaultOrder;
        public bool hasCustomOrder;
        public bool enabled = true;

        public Type GetRuntimeType()
        {
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(assemblyName))
                return null;
            return Type.GetType($"{typeName}, {assemblyName}");
        }

        public bool IsMissing => GetRuntimeType() == null;
    }

    [Serializable]
    public class HookEntry
    {
        public string typeName;
        public string assemblyName;
        public string displayName;
        public List<HookTargetEntry> targets = new();
        public int order;
        public int defaultOrder;
        public bool hasCustomOrder;
        public bool enabled = true;
        public bool isBefore;

        [FormerlySerializedAs("targetPhase")]
        [SerializeField]
        private string legacyTargetPhase;

        [FormerlySerializedAs("targetPhaseAssembly")]
        [SerializeField]
        private string legacyTargetPhaseAssembly;

        public Type GetRuntimeType()
        {
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(assemblyName))
                return null;
            return Type.GetType($"{typeName}, {assemblyName}");
        }

        public bool IsMissing => GetRuntimeType() == null;

        public bool TargetsMatch(string phaseTarget)
        {
            if (targets == null || targets.Count == 0)
                return false;

            if (targets.Any(t => string.IsNullOrEmpty(t.phaseId)))
                return false;

            if (string.IsNullOrEmpty(phaseTarget))
                return false;

            return targets.Any(t => string.Equals(t.phaseId, phaseTarget, StringComparison.OrdinalIgnoreCase));
        }

        public static List<HookEntry> CreateManualEntries(object hook, List<HookTargetEntry> targets)
        {
            var entries = new List<HookEntry>();
            var hookType = hook.GetType();
            var order = PipelineReflection.GetOrder(hook);

            var implementsBefore = hook is IBeforePhaseHook ||
                hookType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBeforePhaseHook<>));
            var implementsAfter = hook is IAfterPhaseHook ||
                hookType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAfterPhaseHook<>));

            if (implementsBefore)
            {
                entries.Add(new HookEntry
                {
                    typeName = hookType.FullName,
                    assemblyName = hookType.Assembly.GetName().Name,
                    displayName = hookType.Name,
                    order = order,
                    defaultOrder = order,
                    enabled = true,
                    isBefore = true,
                    targets = targets
                });
            }

            if (implementsAfter)
            {
                entries.Add(new HookEntry
                {
                    typeName = hookType.FullName,
                    assemblyName = hookType.Assembly.GetName().Name,
                    displayName = hookType.Name,
                    order = order,
                    defaultOrder = order,
                    enabled = true,
                    isBefore = false,
                    targets = targets
                });
            }

            return entries;
        }

        public void MigrateLegacyTargets()
        {
            if ((targets == null || targets.Count == 0) && !string.IsNullOrEmpty(legacyTargetPhase))
            {
                targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = legacyTargetPhase } };
            }
        }
    }

    [Serializable]
    public class HookTargetEntry
    {
        public string phaseId;
    }
}
