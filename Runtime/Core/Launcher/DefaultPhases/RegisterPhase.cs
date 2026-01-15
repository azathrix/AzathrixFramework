using System.Diagnostics;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Launcher.DefaultPhases
{
    /// <summary>
    /// Register阶段 - 注册系统
    /// </summary>
    public class RegisterPhase : IRegisterPhase
    {
        public string Id => "Register";
        public int Order => 300;

        public async UniTask ExecuteAsync(LauncherContext context)
        {
            Log.Separator("Register 阶段");

            var runtimeManager = new SystemRuntimeManager();
            AzathrixFramework.SetRuntimeManager(runtimeManager);
            AzathrixFramework.CreateRuntimeBehaviour();

            var watch = Stopwatch.StartNew();
            await runtimeManager.CreateSystemFromTypesAsync(context.ScannedSystemTypes);
            watch.Stop();

            Log.Info($"[Register] 完成，共 {runtimeManager.GetAllSystems().Count} 个系统，耗时: {watch.Elapsed.TotalMilliseconds:F2}ms");
        }
    }
}
