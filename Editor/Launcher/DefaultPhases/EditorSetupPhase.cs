using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Editor.Launcher.DefaultPhases
{
    /// <summary>
    /// 编辑器Setup阶段
    /// </summary>
    public class EditorSetupPhase : IEditorSetupPhase
    {
        public string Id => "EditorSetup";
        public int Order => 100;

        public UniTask ExecuteAsync(LauncherContext context)
        {
            context.Logger ??= new DefaultLogger();
            context.ResourcesLoader ??= new DefaultResourcesLoader();

            AzathrixFramework.SetupInternal(context.Logger, context.ResourcesLoader);

            return UniTask.CompletedTask;
        }
    }
}
