using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Launcher.DefaultPhases
{
    /// <summary>
    /// Setup阶段 - 初始化基础设施
    /// </summary>
    [Register]
    [PhaseId("Setup")]
    public class SetupPhase : ISetupPhase
    {
        public int Order => 100;

        public UniTask ExecuteAsync(LauncherContext context)
        {
            AzathrixFramework.MarkSetup();

            Log.Separator("Setup 阶段");
            Log.Info("[Setup] 框架配置完成");

            return UniTask.CompletedTask;
        }
    }
}
