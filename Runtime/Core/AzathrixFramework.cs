#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Registry;
using Azathrix.Framework.Settings;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;
using EventDispatcher = Azathrix.Framework.Events.Core.EventDispatcher;
using ILogger = Azathrix.Framework.Interfaces.ILogger;

namespace Azathrix.Framework.Core
{
    public static class AzathrixFramework
    {
        public const string Version = "0.0.9";

        public static bool IsApplicationStarted { get; private set; }
        public static bool IsStarted { get; private set; }
        public static bool IsStarting { get; private set; }
        public static bool IsSetup { get; private set; }

        public static EventDispatcher Dispatcher { get; private set; } = new();
        public static ILogger Logger { get; set; } = new DefaultLogger();
        public static IResourcesLoader ResourcesLoader { get; set; } = new DefaultResourcesLoader();

        private static SystemRuntimeManager _runtimeManager;
        private static LauncherPipeline _pipeline;

#if UNITY_EDITOR
        private static SystemRuntimeManager _editorRuntimeManager;

        public static SystemRuntimeManager EffectiveRuntimeManager =>
            EditorApplication.isPlaying ? _runtimeManager : _editorRuntimeManager;

        public static SystemRuntimeManager EditorRuntimeManager => _editorRuntimeManager;
#else
        public static SystemRuntimeManager EffectiveRuntimeManager => _runtimeManager;
#endif

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void EditorInitialize()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                    Reset();
                else if (state == PlayModeStateChange.EnteredEditMode)
                    ResetEditorRuntime();
            };
        }

        public static void ResetEditorRuntime()
        {
            _editorRuntimeManager = null;
            IsSetup = false;
        }

        public static void SetEditorRuntimeManager(SystemRuntimeManager manager)
        {
            _editorRuntimeManager = manager;
        }

        public static void MarkEditorStarted()
        {
            IsApplicationStarted = true;
            MarkSetup();
            SetStarted(true);
        }
#endif

        static void Reset()
        {
            Logger = new DefaultLogger();
            ResourcesLoader = new DefaultResourcesLoader();
#if UNITY_EDITOR
            _editorRuntimeManager = null;
#endif
            IsApplicationStarted = false;
            IsStarted = false;
            IsStarting = false;
            _runtimeManager = null;
            _pipeline = null;
            _frameworkBehaviour = null;
            IsSetup = false;
            Dispatcher.Clear();
            Dispatcher = new EventDispatcher();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RuntimeAutoStartup()
        {
            var settings = AzathrixFrameworkSettings.Instance;
            if (settings != null && settings.autoInitialize)
                StartupAsync().Forget();
        }

        /// <summary>
        /// 完整的启动流程（统一入口）
        /// </summary>
        public static async UniTask StartupAsync()
        {
            if (IsStarted || IsStarting)
                return;

            IsStarting = true;
            IsApplicationStarted = true;

            try
            {
                // 初始化 Logger 和 ResourcesLoader
                Logger ??= new DefaultLogger();
                ResourcesLoader ??= new DefaultResourcesLoader();

                var totalWatch = Stopwatch.StartNew();

                Log.Separator("Azathrix Framework");
                Log.Info($"版本: {Version}");

                LogSystemInfo();

                _pipeline = PipelineFactory.Get<LauncherPipeline>() as LauncherPipeline;
                if (_pipeline == null)
                    throw new Exception("LauncherPipeline 未找到或创建失败");

                var context = new LauncherContext();

                await _pipeline.ExecuteAsync(context);

                if (context.Aborted)
                {
                    Log.Error("[Framework] 启动被中断");
                    IsStarting = false;
                    return;
                }

                totalWatch.Stop();
                Log.Info($"[Framework] 总耗时: {totalWatch.Elapsed.TotalMilliseconds:F2}ms");
            }
            catch (Exception e)
            {
                Log.Error($"[Framework] 启动失败: {e}");
                IsStarting = false;
                throw;
            }
        }

        /// <summary>
        /// 刷新管线（重新扫描阶段和钩子）
        /// </summary>
        public static void RefreshPipeline()
        {
            PipelineFactory.Refresh("Launcher");
        }

        // 内部方法供阶段调用
        public static void MarkSetup()
        {
            IsSetup = true;
        }


        internal static void SetRuntimeManager(SystemRuntimeManager manager)
        {
            _runtimeManager = manager;
        }

        internal static void SetStarted(bool value)
        {
            IsStarted = value;
            IsStarting = !value;
        }

        private static FrameworkBehaviour _frameworkBehaviour;

        internal static void CreateRuntimeBehaviour()
        {
            // 已存在则重新初始化（支持重试场景）
            if (_frameworkBehaviour != null)
            {
                _frameworkBehaviour.Initialize(_runtimeManager);
                return;
            }

            var existing = GameObject.Find("[Azathrix Framework]");
            if (existing != null)
            {
                _frameworkBehaviour = existing.GetComponent<FrameworkBehaviour>();
                if (_frameworkBehaviour != null)
                {
                    _frameworkBehaviour.Initialize(_runtimeManager);
                    return;
                }
            }

            var go = new GameObject("[Azathrix Framework]");
            _frameworkBehaviour = go.AddComponent<FrameworkBehaviour>();
            _frameworkBehaviour.Initialize(_runtimeManager);
            Log.Info("[Register] 创建 FrameworkBehaviour");
        }

        public static T GetSystem<T>() where T : class, ISystem
        {
            if (EffectiveRuntimeManager == null)
                throw new Exception("AzathrixFramework 未启动");
            return EffectiveRuntimeManager.GetSystem<T>();
        }

        public static bool HasSystem<T>() where T : class, ISystem
        {
            return EffectiveRuntimeManager?.HasSystem<T>() ?? false;
        }

        public static void InjectTo(object target)
        {
            EffectiveRuntimeManager?.InjectTo(target);
        }

        public static void Pause() => _runtimeManager?.Pause();
        public static void Resume() => _runtimeManager?.Resume();

        private static void LogSystemInfo()
        {
            var settings = AzathrixFrameworkSettings.Instance;
            var level = settings?.systemInfoLevel ?? SystemInfoLevel.Simple;

            if (level == SystemInfoLevel.None)
                return;

#if UNITY_EDITOR
            Log.Info($"游戏: {PlayerSettings.productName}");
#endif
            Log.Info($"平台: {Application.platform}");
            Log.Info($"分辨率: {Screen.width}x{Screen.height}");

            if (level == SystemInfoLevel.Simple)
                return;

            Log.Info($"Unity: {Application.unityVersion}");
            Log.Info($"设备: {SystemInfo.deviceModel}");
            Log.Info($"系统: {SystemInfo.operatingSystem}");
            Log.Info($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount}核)");
            Log.Info($"内存: {SystemInfo.systemMemorySize}MB");
            Log.Info($"显卡: {SystemInfo.graphicsDeviceName}");
        }
    }
}
