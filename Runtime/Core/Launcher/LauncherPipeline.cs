using Azathrix.Framework.Core.Pipeline;

namespace Azathrix.Framework.Core.Launcher
{
    /// <summary>
    /// 运行时启动管线
    /// </summary>
    [PipelineId("Launcher")]
    [PipelineDisplayName("运行时启动")]
    public class LauncherPipeline : PipelineBase<ILauncherPhase, LauncherContext>
    {
        /// <summary>
        /// 刷新管线（从注册表重建）
        /// </summary>
        public void Refresh()
        {
            PipelineFactory.Refresh(Id);
        }
    }
}
