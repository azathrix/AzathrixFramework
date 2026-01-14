using Azathrix.Framework.Core.Startup.Phases;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Startup.DefaultPhases
{
    /// <summary>
    /// 启动完成阶段
    /// </summary>
    public class StartPhase : IStartPhase
    {
        public string Id => "Start";
        public int Order => 500;

        public UniTask ExecuteAsync(PhaseContext context)
        {
            AzathrixFramework.Dispatcher.Dispatch<OnGameInitialized>(new OnGameInitialized());
            AzathrixFramework.SetStarted(true);

            Log.Separator("启动完成");
            return UniTask.CompletedTask;
        }
    }
}