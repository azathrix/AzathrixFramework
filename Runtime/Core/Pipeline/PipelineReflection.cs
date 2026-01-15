using System;
using System.Reflection;

namespace Azathrix.Framework.Core.Pipeline
{
    public static class PipelineReflection
    {
        public static string GetPipelineId(Type pipelineType)
        {
            var attr = pipelineType.GetCustomAttribute<PipelineIdAttribute>();
            return attr?.Id ?? pipelineType.Name;
        }

        public static string GetPipelineDisplayName(Type pipelineType, string pipelineId)
        {
            var attr = pipelineType.GetCustomAttribute<PipelineDisplayNameAttribute>();
            return attr != null ? $"{attr.DisplayName}({pipelineId})" : pipelineId;
        }

        public static string GetPhaseId(Type phaseType)
        {
            var attr = phaseType.GetCustomAttribute<PhaseIdAttribute>();
            return attr?.Id ?? phaseType.Name;
        }

        public static string GetPhaseDisplayName(Type phaseType, string phaseId)
        {
            var attr = phaseType.GetCustomAttribute<PhaseDisplayNameAttribute>();
            return attr != null ? $"{attr.DisplayName}({phaseId})" : phaseId;
        }

        public static int GetOrder(object instance)
        {
            if (instance == null) return 0;
            var prop = instance.GetType().GetProperty("Order", BindingFlags.Instance | BindingFlags.Public);
            if (prop == null || prop.PropertyType != typeof(int)) return 0;
            return (int)prop.GetValue(instance);
        }

        public static bool MatchesPhaseTarget(Type phaseType, string phaseId, string interfaceTypeName, string target)
        {
            if (string.IsNullOrEmpty(target))
                return false;

            return string.Equals(target, phaseId, StringComparison.OrdinalIgnoreCase);
        }

    }
}
