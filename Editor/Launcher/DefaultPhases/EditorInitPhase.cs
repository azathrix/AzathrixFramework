using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Editor.Launcher.DefaultPhases
{
    /// <summary>
    /// 编辑器Init阶段 - 编辑器初始化完成
    /// </summary>
    [Register]
    [PhaseId("EditorInit")]
    public class EditorInitPhase : IEditorInitPhase
    {
        public int Order => 500;

        public UniTask ExecuteAsync(LauncherContext context)
        {
            Log.Separator("编辑器初始化完成");
            return UniTask.CompletedTask;
        }
    }
}
