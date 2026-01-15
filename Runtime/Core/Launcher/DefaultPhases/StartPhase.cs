using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Launcher.DefaultPhases
{
    /// <summary>
    /// Start阶段 - 启动完成
    /// </summary>
    public class StartPhase : IStartPhase
    {
        public string Id => "Start";
        public int Order => 500;

        public UniTask ExecuteAsync(LauncherContext context)
        {
            AzathrixFramework.Dispatcher.Dispatch<OnGameInitialized>(new OnGameInitialized());
            AzathrixFramework.SetStarted(true);

            Log.Separator("启动完成");
            return UniTask.CompletedTask;
        }
    }
}
