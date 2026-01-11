using System;
using System.Collections.Generic;
using System.Reflection;
using Azathrix.Framework.Core.Configs;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Startup
{
    /// <summary>
    /// 启动管线
    /// </summary>
    public class StartupPipeline
    {
        private static readonly List<IStartupHook> _manualHooks = new();

        private readonly ILogger _logger;
        private readonly ScannerConfig _config;
        private readonly PhaseScanner _scanner;
        private readonly bool _isEditorMode;

        private List<IStartupPhase> _phases;
        private Dictionary<Type, List<object>> _beforeHooks;
        private Dictionary<Type, List<object>> _afterHooks;

        /// <summary>
        /// 静默模式（不输出日志）
        /// </summary>
        public bool SilentMode { get; set; }

        public StartupPipeline(ILogger logger, ScannerConfig config, bool isEditorMode = false)
        {
            _logger = logger;
            _config = config;
            _isEditorMode = isEditorMode;
            _scanner = new PhaseScanner(logger, config, isEditorMode);
        }

        #region 静态 Hook 管理

        /// <summary>
        /// 注册通用钩子
        /// </summary>
        public static void RegisterHook(IStartupHook hook)
        {
            _manualHooks.Add(hook);
            _manualHooks.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        /// <summary>
        /// 注销钩子
        /// </summary>
        public static void UnregisterHook(IStartupHook hook)
        {
            _manualHooks.Remove(hook);
        }

        /// <summary>
        /// 清空所有手动注册的钩子
        /// </summary>
        public static void ClearHooks() => _manualHooks.Clear();

        #endregion

        /// <summary>
        /// 刷新阶段和钩子（重新扫描）
        /// </summary>
        public void Refresh()
        {
            _phases = _scanner.ScanPhases();
            var (before, after) = _scanner.ScanAllHooks();
            _beforeHooks = before;
            _afterHooks = after;
        }

        /// <summary>
        /// 执行管线
        /// </summary>
        public async UniTask ExecuteAsync(PhaseContext context)
        {
            if (_phases == null)
                Refresh();

            foreach (var phase in _phases)
            {
                if (context.Aborted)
                {
                    if (!SilentMode)
                        Log.Warning($"[Startup] 管线已中断，跳过阶段: {phase.GetType().Name}");
                    break;
                }

                var phaseType = phase.GetType();

                // 编辑器模式下只执行带有 EditorSupport 或 EditorOnly 特性的阶段
                if (context.IsEditor &&
                    phaseType.GetCustomAttribute<EditorSupportAttribute>() == null &&
                    phaseType.GetCustomAttribute<EditorOnlyAttribute>() == null)
                    continue;

                // 运行时模式下跳过 EditorOnly 阶段
                if (!context.IsEditor && phaseType.GetCustomAttribute<EditorOnlyAttribute>() != null)
                    continue;

                var phaseId = phase.Id;

                // 执行前置钩子
                var hookResult = await ExecuteBeforeHooksAsync(phaseId, phaseType, context);
                if (hookResult == HookResult.Abort)
                {
                    if (!SilentMode)
                        Log.Warning($"[Startup] 阶段 {phaseId} 被前置钩子中断");
                    context.Aborted = true;
                    break;
                }
                if (hookResult == HookResult.SkipPhase)
                {
                    if (!SilentMode)
                        Log.Info($"[Startup] 阶段 {phaseId} 被前置钩子跳过");
                    continue;
                }

                // 执行阶段
                try
                {
                    if (!SilentMode)
                        Log.Info($"[Startup] 执行阶段: {phaseId}");
                    await phase.ExecuteAsync(context);
                }
                catch (Exception e)
                {
                    Log.Error($"[Startup] 阶段 {phaseId} 执行失败: {e}");
                    context.Aborted = true;
                    break;
                }

                // 执行后置钩子
                await ExecuteAfterHooksAsync(phaseId, phaseType, context);
            }
        }

        private async UniTask<HookResult> ExecuteBeforeHooksAsync(string phaseId, Type phaseType, PhaseContext context)
        {
            // 1. 执行手动注册的通用钩子
            foreach (var hook in _manualHooks)
            {
                if (!hook.Match(phaseId, phaseType)) continue;
                try
                {
                    var result = await hook.OnBeforeAsync(phaseId, context);
                    if (result != HookResult.Continue)
                        return result;
                }
                catch (Exception e)
                {
                    Log.Error($"[Startup] 钩子 {hook.GetType().Name} 执行失败: {e}");
                }
            }

            // 2. 执行自动扫描的类型钩子
            foreach (var iface in GetPhaseInterfaces(phaseType))
            {
                if (!_beforeHooks.TryGetValue(iface, out var hooks))
                    continue;

                foreach (var hook in hooks)
                {
                    try
                    {
                        var method = hook.GetType().GetMethod("OnBeforeAsync");
                        var task = (UniTask<HookResult>)method.Invoke(hook, new object[] { context });
                        var result = await task;
                        if (result != HookResult.Continue)
                            return result;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[Startup] 钩子 {hook.GetType().Name} 执行失败: {e}");
                    }
                }
            }

            return HookResult.Continue;
        }

        private async UniTask ExecuteAfterHooksAsync(string phaseId, Type phaseType, PhaseContext context)
        {
            // 1. 执行手动注册的通用钩子
            foreach (var hook in _manualHooks)
            {
                if (!hook.Match(phaseId, phaseType)) continue;
                try
                {
                    await hook.OnAfterAsync(phaseId, context);
                }
                catch (Exception e)
                {
                    Log.Error($"[Startup] 钩子 {hook.GetType().Name} 执行失败: {e}");
                }
            }

            // 2. 执行自动扫描的类型钩子
            foreach (var iface in GetPhaseInterfaces(phaseType))
            {
                if (!_afterHooks.TryGetValue(iface, out var hooks))
                    continue;

                foreach (var hook in hooks)
                {
                    try
                    {
                        var method = hook.GetType().GetMethod("OnAfterAsync");
                        var task = (UniTask)method.Invoke(hook, new object[] { context });
                        await task;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[Startup] 钩子 {hook.GetType().Name} 执行失败: {e}");
                    }
                }
            }
        }

        private IEnumerable<Type> GetPhaseInterfaces(Type phaseType)
        {
            foreach (var iface in phaseType.GetInterfaces())
            {
                if (typeof(IStartupPhase).IsAssignableFrom(iface) && iface != typeof(IStartupPhase))
                    yield return iface;
            }
        }
    }
}
