using System;
using System.Linq;
using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.Framework.Registry;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Editor.Launcher.DefaultPhases
{
    /// <summary>
    /// 编辑器Scan阶段
    /// </summary>
    [Register]
    [PhaseId("EditorScan")]
    public class EditorScanPhase : IEditorScanPhase
    {
        public int Order => 200;

        public async UniTask ExecuteAsync(LauncherContext context)
        {
            var registry = SystemRegistry.Instance;
            if (registry == null || registry.entries.Count == 0)
            {
                context.ScannedSystemTypes = Array.Empty<Type>();
                return;
            }

            var types = registry.GetEnabledTypes()
                .Where(t => t != null
                            && typeof(ISystem).IsAssignableFrom(t)
                            && typeof(ISystemEditorSupport).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && !t.IsInterface)
                .ToArray();

            context.ScannedSystemTypes = types;
            await UniTask.Yield();
        }
    }
}
