using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Tools;

namespace Azathrix.Framework.Core.Pipeline
{
    /// <summary>
    /// 管线工厂 - 统一构建并缓存管线实例
    /// </summary>
    public static class PipelineFactory
    {
        private static readonly Dictionary<string, IPipeline> Pipelines = new();
        private static bool _dirty = true;

        static PipelineFactory()
        {
#if UNITY_EDITOR
            PipelineRegistry.OnRegistryChanged += MarkDirty;
#endif
        }

        public static T Get<T>() where T : IPipeline
        {
            var id = PipelineReflection.GetPipelineId(typeof(T));
            return (T)Get(id);
        }

        public static T CreateEmpty<T>() where T : IPipeline
        {
            try
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
            catch (Exception e)
            {
                Log.Error($"[PipelineFactory] 创建空管线 {typeof(T).FullName} 失败: {e}");
                return default;
            }
        }

        public static IPipeline CreateEmpty(string pipelineId)
        {
            if (string.IsNullOrEmpty(pipelineId))
                return null;

            var pipelineType = FindPipelineTypeById(pipelineId);
            if (pipelineType == null)
            {
                Log.Warning($"[PipelineFactory] 未找到管线类型: {pipelineId}");
                return null;
            }

            try
            {
                return (IPipeline)Activator.CreateInstance(pipelineType);
            }
            catch (Exception e)
            {
                Log.Error($"[PipelineFactory] 创建空管线 {pipelineType.FullName} 失败: {e}");
                return null;
            }
        }

        public static IPipeline Get(string pipelineId)
        {
            if (string.IsNullOrEmpty(pipelineId))
                return null;

            var registry = PipelineRegistry.Instance;
            if (registry == null)
            {
                Log.Error("[PipelineFactory] PipelineRegistry 未找到");
                return null;
            }

            if (!Pipelines.TryGetValue(pipelineId, out var pipeline) || pipeline == null)
            {
                pipeline = CreatePipeline(registry, pipelineId);
                if (pipeline == null)
                    return null;
                Pipelines[pipelineId] = pipeline;
            }

            if (_dirty && pipeline is IPipelineBuilder dirtyBuilder)
            {
                dirtyBuilder.BindRegistry(registry, registry.GetPipeline(pipelineId));
                dirtyBuilder.MarkDirty();
            }

            if (_dirty)
                _dirty = false;

            return pipeline;
        }

        public static void Refresh(string pipelineId = null)
        {
            if (string.IsNullOrEmpty(pipelineId))
            {
                _dirty = true;
                var registry = PipelineRegistry.Instance;
                foreach (var pair in Pipelines)
                {
                    if (pair.Value is not IPipelineBuilder builder)
                        continue;
                    var entry = registry != null ? registry.GetPipeline(pair.Key) : null;
                    builder.BindRegistry(registry, entry);
                    builder.MarkDirty();
                }
                return;
            }

            if (Pipelines.TryGetValue(pipelineId, out var pipeline) && pipeline is IPipelineBuilder pipelineBuilder)
            {
                var registry = PipelineRegistry.Instance;
                pipelineBuilder.BindRegistry(registry, registry != null ? registry.GetPipeline(pipelineId) : null);
                pipelineBuilder.MarkDirty();
            }
        }

        private static IPipeline CreatePipeline(PipelineRegistry registry, string pipelineId)
        {
            var entry = registry.GetPipeline(pipelineId);
            var pipelineType = entry?.GetPipelineType();

            if (pipelineType == null)
                pipelineType = FindPipelineTypeById(pipelineId);

            if (pipelineType == null)
            {
                Log.Warning($"[PipelineFactory] 未找到管线类型: {pipelineId}");
                return null;
            }

            if (!typeof(IPipeline).IsAssignableFrom(pipelineType))
            {
                Log.Warning($"[PipelineFactory] 类型 {pipelineType.FullName} 未实现 IPipeline");
                return null;
            }

            try
            {
                var instance = (IPipeline)Activator.CreateInstance(pipelineType);
                if (instance is IPipelineBuilder builder)
                    builder.BindRegistry(registry, entry);
                return instance;
            }
            catch (Exception e)
            {
                Log.Error($"[PipelineFactory] 创建管线 {pipelineType.FullName} 失败: {e}");
                return null;
            }
        }

        private static Type FindPipelineTypeById(string pipelineId)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface) continue;
                    if (!typeof(IPipeline).IsAssignableFrom(type)) continue;

                    var id = PipelineReflection.GetPipelineId(type);
                    if (string.Equals(id, pipelineId, StringComparison.OrdinalIgnoreCase))
                        return type;
                }
            }

            return null;
        }

        private static void MarkDirty()
        {
            _dirty = true;
            var registry = PipelineRegistry.Instance;
            foreach (var pair in Pipelines)
            {
                if (pair.Value is not IPipelineBuilder builder)
                    continue;

                var entry = registry != null ? registry.GetPipeline(pair.Key) : null;
                builder.BindRegistry(registry, entry);
                builder.MarkDirty();
            }
        }
    }
}
