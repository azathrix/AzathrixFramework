using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Launcher.DefaultPhases
{
    /// <summary>
    /// Setup阶段 - 初始化基础设施
    /// </summary>
    public class SetupPhase : ISetupPhase
    {
        public string Id => "Setup";
        public int Order => 100;

        public UniTask ExecuteAsync(LauncherContext context)
        {
            Log.Separator("Setup 阶段");

            context.Logger ??= new DefaultLogger();
            context.ResourcesLoader ??= new DefaultResourcesLoader();

            AzathrixFramework.SetupInternal(context.Logger, context.ResourcesLoader);

            Log.Info("[Setup] 框架配置完成");

            return UniTask.CompletedTask;
        }
    }
}
