using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Editor.Launcher.DefaultPhases
{
    /// <summary>
    /// 编辑器Scan阶段
    /// </summary>
    [PhaseId("EditorScan")]
    public class EditorScanPhase : IEditorScanPhase
    {
        public int Order => 200;

        public UniTask ExecuteAsync(LauncherContext context)
        {
            Log.Separator("编辑器扫描");
            return UniTask.CompletedTask;
        }
    }
}
