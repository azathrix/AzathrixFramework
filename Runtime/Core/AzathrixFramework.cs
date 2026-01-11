#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Azathrix.Framework.Core.Configs;
using Azathrix.Framework.Core.Startup;
using Azathrix.Framework.Events.Core;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Registry;
using Azathrix.Framework.Settings;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;
using ILogger = Azathrix.Framework.Interfaces.ILogger;

namespace Azathrix.Framework.Core
{
    public static class AzathrixFramework
    {
        public const string Version = "0.0.1";

        public static bool IsApplicationStarted { get; private set; }
        public static bool IsStarted { get; private set; }
        public static bool IsStarting { get; private set; }
        public static bool IsSetup { get; private set; }

        public static EventDispatcher Dispatcher { get; private set; } = new();
        public static ILogger Logger { get; set; }
        public static IResourcesLoader ResourcesLoader { get; set; }
        public static ScannerConfig ScannerConfig { get; private set; }
        public static RuntimeConfig RuntimeConfig { get; private set; }

        private static SystemRuntimeManager _runtimeManager;
        private static StartupPipeline _pipeline;

#if UNITY_EDITOR
        private static SystemRuntimeManager _editorRuntimeManager;
        private static StartupPipeline _editorPipeline;

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
            EditorApplication.delayCall += () => InitializeEditorAsync().Forget();

            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                    Reset();
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    ResetEditorRuntime();
                    EditorApplication.delayCall += () => InitializeEditorAsync().Forget();
                }
            };

            SystemRegistry.OnRegistryChanged += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    RefreshEditorPipelineAsync().Forget();
            };
            PhaseRegistry.OnRegistryChanged += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    RefreshEditorPipelineAsync().Forget();
            };
            StartupHookRegistry.OnRegistryChanged += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    RefreshEditorPipelineAsync().Forget();
            };
        }

        private static async UniTask InitializeEditorAsync()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var settings = AzathrixFrameworkSettings.Instance;
            var scannerConfig = settings.ToScannerConfig();

            Logger ??= new DefaultLogger();

            _editorPipeline = new StartupPipeline(Logger, scannerConfig, isEditorMode: true)
            {
                SilentMode = !settings.debugEditorPipeline
            };

            var context = new PhaseContext
            {
                Logger = Logger,
                ResourcesLoader = new DefaultResourcesLoader(),
                IsEditor = true
            };

            await _editorPipeline.ExecuteAsync(context);
        }

        private static async UniTask RefreshEditorPipelineAsync()
        {
            if (_editorRuntimeManager == null || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // 只重新执行扫描和注册阶段，不重新执行整个管线
            var settings = AzathrixFrameworkSettings.Instance;
            var scannerConfig = settings.ToScannerConfig();

            // 重新扫描编辑器系统
            var context = new PhaseContext
            {
                Logger = Logger,
                ResourcesLoader = ResourcesLoader,
                IsEditor = true
            };

            // 使用新的扫描阶段获取类型
            var scanPhase = new Startup.DefaultPhases.Editor.EditorScanPhase();
            await scanPhase.ExecuteAsync(context);

            // 同步系统：移除不存在的，添加新的
            await SyncEditorSystemsAsync(context.ScannedSystemTypes);
        }

        private static async UniTask SyncEditorSystemsAsync(Type[] expectedTypes)
        {
            if (_editorRuntimeManager == null)
                return;

            var expectedSet = new HashSet<Type>(expectedTypes);
            var registeredTypes = new HashSet<Type>();
            foreach (var sys in _editorRuntimeManager.GetAllSystems())
                registeredTypes.Add(sys.GetType());

            // 移除不应该存在的系统
            foreach (var type in registeredTypes)
            {
                if (!expectedSet.Contains(type))
                    _editorRuntimeManager.UnRegister(type);
            }

            // 注册新系统
            foreach (var type in expectedTypes)
            {
                if (!registeredTypes.Contains(type))
                {
                    await _editorRuntimeManager.RegisterSystemAsync(type);
                    var system = _editorRuntimeManager.GetAllSystems().FirstOrDefault(s => s.GetType() == type);
                    if (system is Interfaces.SystemEvents.ISystemEditorSupport editorSupport)
                    {
                        try
                        {
                            editorSupport.OnEditorInitialize();
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }

        public static void ResetEditorRuntime()
        {
            _editorRuntimeManager = null;
            _editorPipeline = null;
            IsSetup = false;
        }

        internal static void SetEditorRuntimeManager(SystemRuntimeManager manager)
        {
            _editorRuntimeManager = manager;
        }
#endif

        static void Reset()
        {
            Logger = null;
            ScannerConfig = null;
            RuntimeConfig = null;
#if UNITY_EDITOR
            _editorRuntimeManager = null;
            _editorPipeline = null;
#endif
            IsApplicationStarted = false;
            IsStarted = false;
            IsStarting = false;
            _runtimeManager = null;
            _pipeline = null;
            _frameworkBehaviour = null;
            IsSetup = false;
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
                Logger = new DefaultLogger();
                var totalWatch = Stopwatch.StartNew();

                Log.Separator("Azathrix Framework");
                Log.Info($"版本: {Version}");

                LogSystemInfo();

                var settings = AzathrixFrameworkSettings.Instance;
                _pipeline = new StartupPipeline(Logger, settings.ToScannerConfig());

                var context = new PhaseContext
                {
                    Logger = Logger,
                    ResourcesLoader = new DefaultResourcesLoader()
                };

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
            _pipeline?.Refresh();
#if UNITY_EDITOR
            _editorPipeline?.Refresh();
#endif
        }

        // 内部方法供阶段调用
        internal static void SetupInternal(ILogger logger, IResourcesLoader resourcesLoader,
            ScannerConfig scannerConfig, RuntimeConfig runtimeConfig)
        {
            if (IsSetup) return;

            ResourcesLoader = resourcesLoader ?? new DefaultResourcesLoader();
            Logger = logger ?? new DefaultLogger();
            ScannerConfig = scannerConfig;
            RuntimeConfig = runtimeConfig;
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