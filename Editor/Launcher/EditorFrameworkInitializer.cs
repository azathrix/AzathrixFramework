using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Settings;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Azathrix.Framework.Editor.Launcher
{
    /// <summary>
    /// 编辑器框架初始化器
    /// </summary>
    public static class EditorFrameworkInitializer
    {
        private static EditorLauncherPipeline _editorPipeline;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += () => InitializeEditorAsync().Forget();

            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    _editorPipeline = null;
                    EditorApplication.delayCall += () => InitializeEditorAsync().Forget();
                }
            };
        }

        private static async UniTask InitializeEditorAsync()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var settings = AzathrixFrameworkSettings.Instance;

            _editorPipeline = new EditorLauncherPipeline();

            var context = new LauncherContext
            {
                Logger = new DefaultLogger(),
                ResourcesLoader = new DefaultResourcesLoader(),
                IsEditor = true,
                SilentMode = !settings.debugEditorPipeline
            };

            await _editorPipeline.ExecuteAsync(context);
        }

        public static void RefreshEditorPipeline()
        {
            _editorPipeline?.Refresh();
        }
    }
}
