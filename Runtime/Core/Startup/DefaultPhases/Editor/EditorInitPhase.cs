#if UNITY_EDITOR
using System;
using Azathrix.Framework.Interfaces.SystemEvents;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azathrix.Framework.Core.Startup.DefaultPhases.Editor
{
    /// <summary>
    /// 编辑器初始化阶段
    /// </summary>
    [EditorOnly]
    public class EditorInitPhase : IStartupPhase
    {
        public string Id => "EditorInit";
        public int Order => 500;

        public UniTask ExecuteAsync(PhaseContext context)
        {
            if (!context.IsEditor) return UniTask.CompletedTask;

            var runtimeManager = AzathrixFramework.EditorRuntimeManager;
            if (runtimeManager == null)
                return UniTask.CompletedTask;

            foreach (var system in runtimeManager.GetAllSystems())
            {
                if (system is ISystemEditorSupport editorSupport)
                {
                    try
                    {
                        editorSupport.OnEditorInitialize();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            return UniTask.CompletedTask;
        }
    }
}
#endif
