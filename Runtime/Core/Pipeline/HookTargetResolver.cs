using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Azathrix.Framework.Core.Pipeline
{
    internal static class HookTargetResolver
    {
        public static List<HookTargetEntry> GetTargetsForPipeline(Type hookType, string pipelineId)
        {
            var targets = hookType.GetCustomAttributes<HookTargetAttribute>(true).ToList();
            if (targets.Count == 0)
                return null;

            var matched = targets
                .Where(t => !string.IsNullOrEmpty(t.PhaseId))
                .Where(t => string.Equals(t.PipelineId, pipelineId, StringComparison.OrdinalIgnoreCase))
                .Select(t => new HookTargetEntry { phaseId = t.PhaseId })
                .ToList();

            return matched.Count == 0 ? null : matched;
        }
    }
}
