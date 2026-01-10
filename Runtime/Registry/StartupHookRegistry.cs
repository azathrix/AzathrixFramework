using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Settings;

namespace Azathrix.Framework.Registry
{
    [Serializable]
    public class StartupHookEntry : RegistryEntryBase
    {
        public int order;
        public string targetPhaseType;
        public bool isBefore;
    }

    [SettingsName("StartupHookRegistry")]
    [ShowSetting("启动钩子注册表")]
    public class StartupHookRegistry : RegistryBase<StartupHookRegistry, StartupHookEntry>
    {
        public List<StartupHookEntry> GetHooksForPhase(string phaseTypeName, bool before)
        {
            return entries
                .Where(e => e.enabled && e.targetPhaseType == phaseTypeName && e.isBefore == before)
                .OrderBy(e => e.order)
                .ToList();
        }

        public List<StartupHookEntry> GetBeforeHooks(string phaseTypeName)
        {
            return GetHooksForPhase(phaseTypeName, true);
        }

        public List<StartupHookEntry> GetAfterHooks(string phaseTypeName)
        {
            return GetHooksForPhase(phaseTypeName, false);
        }

        public Type[] GetBeforeHookTypes(string phaseTypeName)
        {
            return GetBeforeHooks(phaseTypeName)
                .Select(e => e.GetRuntimeType())
                .Where(t => t != null)
                .ToArray();
        }

        public Type[] GetAfterHookTypes(string phaseTypeName)
        {
            return GetAfterHooks(phaseTypeName)
                .Select(e => e.GetRuntimeType())
                .Where(t => t != null)
                .ToArray();
        }
    }
}
