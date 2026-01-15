using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
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

        public UniTask ExecuteAsync(LauncherContext context)
        {
            Log.Separator("编辑器注册");
            return UniTask.CompletedTask;
        }
    }
}
