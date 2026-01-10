#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.Framework.Registry;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Startup.DefaultPhases.Editor
{
    /// <summary>
    /// 编辑器系统扫描阶段
    /// </summary>
    [EditorOnly]
    public class EditorScanPhase : IStartupPhase
    {
        public string Id => "EditorScan";
        public int Order => 300;

        public UniTask ExecuteAsync(PhaseContext context)
        {
            if (!context.IsEditor) return UniTask.CompletedTask;

            var config = AzathrixFramework.ScannerConfig;

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => ShouldScanAssembly(a, config))
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => IsEditorSupportSystem(t, config))
                .ToArray();

            context.ScannedSystemTypes = types;
            return UniTask.CompletedTask;
        }

        private bool ShouldScanAssembly(Assembly assembly, Configs.ScannerConfig config)
        {
            var name = assembly.GetName().Name;
            if (config.ExcludeAssemblyPrefixes.Any(p => name.StartsWith(p)))
                return false;

            if (config.AssemblyPrefixes.Count > 0)
                return config.AssemblyPrefixes.Any(p => name.StartsWith(p));

            return true;
        }

        private bool IsEditorSupportSystem(Type type, Configs.ScannerConfig config)
        {
            if (!typeof(ISystem).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                return false;
            if (!typeof(ISystemEditorSupport).IsAssignableFrom(type))
                return false;

            var systemRegistry = SystemRegistry.Instance;
            if (systemRegistry != null && systemRegistry.IsSystemDisabled(type))
                return false;

            return true;
        }
    }
}
#endif
