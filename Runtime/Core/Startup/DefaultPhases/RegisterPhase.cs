using System.Diagnostics;
using Azathrix.Framework.Core.Startup.Phases;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Startup.DefaultPhases
{
    /// <summary>
    /// 系统注册阶段
    /// </summary>
    public class RegisterPhase : IRegisterPhase
    {
        public string Id => "Register";
        public int Order => 400;

        public async UniTask ExecuteAsync(PhaseContext context)
        {
            Log.Separator("Register 阶段");

            var runtimeConfig = AzathrixFramework.RuntimeConfig;
            var runtimeManager = new SystemRuntimeManager();
            runtimeManager.EnableProfiling = runtimeConfig.EnableProfiling;

            AzathrixFramework.SetRuntimeManager(runtimeManager);
            AzathrixFramework.CreateRuntimeBehaviour();

            var watch = Stopwatch.StartNew();
            await runtimeManager.CreateSystemFromTypesAsync(context.ScannedSystemTypes);
            watch.Stop();

            Log.Info($"[Register] 完成，共 {runtimeManager.GetAllSystems().Count} 个系统，耗时: {watch.Elapsed.TotalMilliseconds:F2}ms");
        }
    }
}
