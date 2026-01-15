using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Settings;
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
            if (_cache == null) RebuildCache();
            return _cache.TryGetValue(pipelineId, out var entry) ? entry : null;
        }

        public PipelineEntry GetOrCreatePipeline(string pipelineId, string displayName = null)
        {
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
                .Where(e => e.enabled && e.targetPhase == phaseTypeName && e.isBefore == before)
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
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => OnRegistryChanged?.Invoke();
#endif
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
        public string targetPhase;
        public string targetPhaseAssembly;
        public int order;
        public int defaultOrder;
        public bool hasCustomOrder;
        public bool enabled = true;
        public bool isBefore;

        public Type GetRuntimeType()
        {
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(assemblyName))
                return null;
            return Type.GetType($"{typeName}, {assemblyName}");
        }

        public Type GetTargetPhaseType()
        {
            if (string.IsNullOrEmpty(targetPhase) || string.IsNullOrEmpty(targetPhaseAssembly))
                return null;
            return Type.GetType($"{targetPhase}, {targetPhaseAssembly}");
        }

        public bool IsMissing => GetRuntimeType() == null;
    }
}
