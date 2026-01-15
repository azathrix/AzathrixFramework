using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Editor.Launcher.DefaultPhases
{
    /// <summary>
    /// 编辑器Register阶段
    /// </summary>
    public class EditorRegisterPhase : IEditorRegisterPhase
    {
        public string Id => "EditorRegister";
        public int Order => 300;

        public UniTask ExecuteAsync(LauncherContext context)
        {
            Log.Separator("编辑器注册");
            return UniTask.CompletedTask;
        }
    }
}
