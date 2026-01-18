using System;
using System.Diagnostics;
using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Editor.Launcher.DefaultPhases
{
    /// <summary>
    /// 编辑器Register阶段
    /// </summary>
    [Register]
    [PhaseId("EditorRegister")]
    public class EditorRegisterPhase : IEditorRegisterPhase
    {
        public int Order => 300;

        public async UniTask ExecuteAsync(LauncherContext context)
        {
            var runtimeManager = new SystemRuntimeManager
            {
                IsEditorMode = true
            };
            AzathrixFramework.SetEditorRuntimeManager(runtimeManager);

            var systemTypes = context.ScannedSystemTypes ?? Array.Empty<Type>();
            var watch = Stopwatch.StartNew();
            await runtimeManager.CreateSystemFromTypesAsync(systemTypes);
            watch.Stop();

            foreach (var system in runtimeManager.GetAllSystems())
            {
                if (system is not ISystemEditorSupport editorSupport)
                    continue;

                try
                {
                    editorSupport.OnEditorInitialize();
                }
                catch (Exception e)
                {
                    Log.Exception(e); 
                }
            }

        }
    }
}
