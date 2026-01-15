using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Editor.Launcher.DefaultPhases
{
    /// <summary>
    /// 编辑器Setup阶段
    /// </summary>
    [Register]
    [PhaseId("EditorSetup")]
    public class EditorSetupPhase : IEditorSetupPhase
    {
        public int Order => 100;

        public UniTask ExecuteAsync(LauncherContext context)
        {
            AzathrixFramework.MarkSetup();

            return UniTask.CompletedTask;
        }
    }
}
