using System;
using System.Collections.Generic;
using System.Reflection;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Pipeline
{
    /// <summary>
    /// 管线基类
    /// </summary>
    public abstract class PipelineBase<TPhase, THook, TContext> : IPipeline
        where TPhase : IPhase<TContext>
        where THook : IHook<TContext>
        where TContext : PipelineContext
    {
        private static readonly List<THook> _manualHooks = new();

        protected List<TPhase> _phases = new();
        protected Dictionary<Type, List<object>> _beforeHooks = new();
        protected Dictionary<Type, List<object>> _afterHooks = new();

        /// <summary>
        /// 静默模式（不输出日志）
        /// </summary>
        public bool SilentMode { get; set; }

        #region IPipeline

        public virtual string Id
        {
            get
            {
                var attr = GetType().GetCustomAttribute<PipelineIdAttribute>();
                return attr?.Id ?? GetType().Name;
            }
        }

        public virtual string DisplayName
        {
            get
            {
                var attr = GetType().GetCustomAttribute<PipelineDisplayNameAttribute>();
                return attr?.DisplayName ?? Id;
            }
        }

        public Type PhaseType => typeof(TPhase);
        public Type HookType => typeof(THook);
        public Type ContextType => typeof(TContext);

        #endregion

        #region 静态Hook管理

        /// <summary>
        /// 注册钩子
        /// </summary>
        public static void RegisterHook(THook hook)
        {
            _manualHooks.Add(hook);
            _manualHooks.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        /// <summary>
        /// 注销钩子
        /// </summary>
        public static void UnregisterHook(THook hook)
        {
            _manualHooks.Remove(hook);
        }

        /// <summary>
        /// 清空所有手动注册的钩子
        /// </summary>
        public static void ClearHooks() => _manualHooks.Clear();

        #endregion

        /// <summary>
        /// 添加阶段
        /// </summary>
        public void AddPhase(TPhase phase)
        {
            _phases.Add(phase);
            _phases.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        /// <summary>
        /// 移除阶段
        /// </summary>
        public void RemovePhase(TPhase phase)
        {
            _phases.Remove(phase);
        }

        /// <summary>
        /// 添加前置钩子
        /// </summary>
        public void AddBeforeHook<TTargetPhase>(object hook) where TTargetPhase : TPhase
        {
            var type = typeof(TTargetPhase);
            if (!_beforeHooks.TryGetValue(type, out var list))
                _beforeHooks[type] = list = new List<object>();
            list.Add(hook);
        }

        /// <summary>
        /// 添加后置钩子
        /// </summary>
        public void AddAfterHook<TTargetPhase>(object hook) where TTargetPhase : TPhase
        {
            var type = typeof(TTargetPhase);
            if (!_afterHooks.TryGetValue(type, out var list))
                _afterHooks[type] = list = new List<object>();
            list.Add(hook);
        }

        /// <summary>
        /// 执行管线
        /// </summary>
        public virtual async UniTask ExecuteAsync(TContext context)
        {
            if (_phases == null || _phases.Count == 0)
            {
                Log.Error($"[{Id}] 没有可执行的阶段");
                context.Aborted = true;
                return;
            }

            foreach (var phase in _phases)
            {
                if (context.Aborted)
                {
                    if (!SilentMode)
                        Log.Warning($"[{Id}] 管线已中断，跳过阶段: {phase.GetType().Name}");
                    break;
                }

                // 检查编辑器模式
                if (!ShouldExecutePhase(phase, context))
                    continue;

                var phaseType = phase.GetType();
                var phaseId = phase.Id;

                // 执行前置钩子
                var hookResult = await ExecuteBeforeHooksAsync(phaseId, phaseType, context);
                if (hookResult == HookResult.Abort)
                {
                    if (!SilentMode)
                        Log.Warning($"[{Id}] 阶段 {phaseId} 被前置钩子中断");
                    context.Aborted = true;
                    break;
                }

                if (hookResult == HookResult.SkipPhase)
                {
                    if (!SilentMode)
                        Log.Info($"[{Id}] 阶段 {phaseId} 被前置钩子跳过");
                    continue;
                }

                // 执行阶段
                try
                {
                    if (!SilentMode)
                        Log.Info($"[{Id}] 执行阶段: {phaseId}");
                    await phase.ExecuteAsync(context);
                }
                catch (Exception e)
                {
                    Log.Error($"[{Id}] 阶段 {phaseId} 执行失败: {e}");
                    context.Aborted = true;
                    break;
                }

                // 执行后置钩子
                await ExecuteAfterHooksAsync(phaseId, phaseType, context);
            }
        }

        /// <summary>
        /// 判断是否应该执行阶段（子类可重写）
        /// </summary>
        protected virtual bool ShouldExecutePhase(TPhase phase, TContext context)
        {
            return true;
        }

        private async UniTask<HookResult> ExecuteBeforeHooksAsync(string phaseId, Type phaseType, TContext context)
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
                    Log.Error($"[{Id}] 钩子 {hook.GetType().Name} 执行失败: {e}");
                }
            }

            // 2. 执行类型钩子
            foreach (var iface in GetPhaseInterfaces(phaseType))
            {
                if (!_beforeHooks.TryGetValue(iface, out var hooks))
                    continue;

                foreach (var hook in hooks)
                {
                    try
                    {
                        var method = hook.GetType().GetMethod("OnBeforeAsync");
                        var task = (UniTask<HookResult>) method.Invoke(hook, new object[] {context});
                        var result = await task;
                        if (result != HookResult.Continue)
                            return result;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[{Id}] 钩子 {hook.GetType().Name} 执行失败: {e}");
                    }
                }
            }

            return HookResult.Continue;
        }

        private async UniTask ExecuteAfterHooksAsync(string phaseId, Type phaseType, TContext context)
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
                    Log.Error($"[{Id}] 钩子 {hook.GetType().Name} 执行失败: {e}");
                }
            }

            // 2. 执行类型钩子
            foreach (var iface in GetPhaseInterfaces(phaseType))
            {
                if (!_afterHooks.TryGetValue(iface, out var hooks))
                    continue;

                foreach (var hook in hooks)
                {
                    try
                    {
                        var method = hook.GetType().GetMethod("OnAfterAsync");
                        var task = (UniTask) method.Invoke(hook, new object[] {context});
                        await task;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[{Id}] 钩子 {hook.GetType().Name} 执行失败: {e}");
                    }
                }
            }
        }

        private IEnumerable<Type> GetPhaseInterfaces(Type phaseType)
        {
            foreach (var iface in phaseType.GetInterfaces())
            {
                if (typeof(TPhase).IsAssignableFrom(iface) && iface != typeof(TPhase) && iface != typeof(IPhase))
                    yield return iface;
            }
        }
    }
}