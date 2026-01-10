#if UNITY_EDITOR
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Startup.DefaultPhases.Editor
{
    /// <summary>
    /// 编辑器系统注册阶段
    /// </summary>
    [EditorOnly]
    public class EditorRegisterPhase : IStartupPhase
    {
        public string Id => "EditorRegister";
        public int Order => 400;

        public async UniTask ExecuteAsync(PhaseContext context)
        {
            if (!context.IsEditor) return;

            var runtimeConfig = AzathrixFramework.RuntimeConfig;
            var runtimeManager = new SystemRuntimeManager();
            runtimeManager.IsEditorMode = true;
            runtimeManager.EnableProfiling = runtimeConfig.EnableProfiling;

            AzathrixFramework.SetEditorRuntimeManager(runtimeManager);

            await runtimeManager.CreateSystemFromTypesAsync(context.ScannedSystemTypes);
        }
    }
}
#endif
