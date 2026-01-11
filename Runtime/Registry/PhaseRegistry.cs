using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Settings;

namespace Azathrix.Framework.Registry
{
    [Serializable]
    public class PhaseEntry : RegistryEntryBase
    {
        public string phaseId;
        public int order;
        public bool editorSupport;  // 编辑器支持（运行时+编辑器都执行）
        public bool editorOnly;     // 仅编辑器（只在编辑器执行，运行时不执行）
    }

    [SettingsPath("PhaseRegistry")]
    [ShowSetting("阶段注册表")]
    public class PhaseRegistry : RegistryBase<PhaseRegistry, PhaseEntry>
    {
        public List<PhaseEntry> GetOrderedPhases(bool editorMode = false)
        {
            var result = entries.Where(e => e.enabled);
            if (editorMode)
                result = result.Where(e => e.editorSupport || e.editorOnly);
            else
                result = result.Where(e => !e.editorOnly); // 运行时排除 EditorOnly
            return result.OrderBy(e => e.order).ToList();
        }

        public Type[] GetOrderedPhaseTypes(bool editorMode = false)
        {
            return GetOrderedPhases(editorMode)
                .Select(e => e.GetRuntimeType())
                .Where(t => t != null)
                .ToArray();
        }
    }
}
