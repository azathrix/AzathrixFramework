#if UNITY_EDITOR
using Azathrix.Framework.Settings;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Startup.DefaultPhases.Editor
{
    /// <summary>
    /// 编辑器配置阶段
    /// </summary>
    [EditorOnly]
    public class EditorSetupPhase : IStartupPhase
    {
        public string Id => "EditorSetup";
        public int Order => 200;

        public UniTask ExecuteAsync(PhaseContext context)
        {
            if (!context.IsEditor) return UniTask.CompletedTask;

            context.Logger ??= new DefaultLogger();
            context.ResourcesLoader ??= new DefaultResourcesLoader();

            var settings = AzathrixFrameworkSettings.Instance;
            AzathrixFramework.SetupInternal(
                context.Logger,
                context.ResourcesLoader,
                settings.ToScannerConfig(),
                settings.ToRuntimeConfig()
            );

            return UniTask.CompletedTask;
        }
    }
}
#endif
