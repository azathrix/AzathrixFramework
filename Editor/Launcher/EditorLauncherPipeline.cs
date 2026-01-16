using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Editor.Launcher
{
    /// <summary>
    /// 编辑器启动管线
    /// </summary>
    [PipelineId("EditorLauncher")]
    [PipelineDisplayName("编辑器启动")]
    [Register]
    public class EditorLauncherPipeline : PipelineBase<IEditorLauncherPhase, LauncherContext>
    {
        /// <summary>
        /// 刷新管线（从注册表重建）
        /// </summary>
        public void Refresh()
        {
            PipelineFactory.Refresh(Id);
        }

        public override async UniTask ExecuteAsync(LauncherContext context)
        {
            context.IsEditor = true;
            await base.ExecuteAsync(context);
        }
    }
}
