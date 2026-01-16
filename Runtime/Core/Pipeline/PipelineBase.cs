using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Pipeline
{
    internal interface IPipelineBuilder
    {
        void BindRegistry(PipelineRegistry registry, PipelineEntry entry);
        void MarkDirty();
    }

    /// <summary>
    /// 管线基类（仅关心阶段与上下文）
    /// </summary>
    public abstract class PipelineBase<TPhase, TContext> : IPipeline, IPipelineBuilder
        where TPhase : IPhase<TContext>
        where TContext : PipelineContext
    {
        private sealed class PhaseExecution
        {
            public TPhase Phase;
            public string PhaseId;
            public string InterfaceTypeName;
            public int Order;
            public bool IsManual;
            public List<BeforeHookInvoker> BeforeHooks = new();
            public List<AfterHookInvoker> AfterHooks = new();
        }

        private sealed class BeforeHookInvoker
        {
            public int Order;
            public Func<TContext, UniTask<HookResult>> Invoke;
            public object Source;
            public bool IsManual;
        }

        private sealed class AfterHookInvoker
        {
            public int Order;
            public Func<TContext, UniTask> Invoke;
            public object Source;
            public bool IsManual;
        }

        private readonly List<PhaseExecution> _executions = new();

        private PipelineRegistry _registry;
        private PipelineEntry _registryEntry;
        private bool _dirty = true;
        private bool _isExecuting;

        /// <summary>
        /// 静默模式（不输出日志）
        /// </summary>
        public bool SilentMode { get; set; }

        #region IPipeline

        public virtual string Id => PipelineReflection.GetPipelineId(GetType());

        public virtual string DisplayName => PipelineReflection.GetPipelineDisplayName(GetType(), Id);

        public Type PhaseType => typeof(TPhase);

        public Type ContextType => typeof(TContext);

        #endregion

        #region 构建与注册

        public void BindRegistry(PipelineRegistry registry, PipelineEntry entry)
        {
            _registry = registry;
            _registryEntry = entry;
            _dirty = true;
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public void AddPhase(TPhase phase)
        {
            AddPhase(phase, null, null);
        }

        public void AddPhase(TPhase phase, string phaseId)
        {
            AddPhase(phase, phaseId, null);
        }

        public void AddPhase(TPhase phase, string phaseId, int? orderOverride)
        {
            if (phase == null)
                return;
            if (_isExecuting)
            {
                Log.Warning($"[{Id}] 管线执行中，忽略添加阶段: {phase.GetType().Name}");
                return;
            }

            EnsureBuilt();

            var phaseType = phase.GetType();
            var resolvedId = string.IsNullOrEmpty(phaseId)
                ? PipelineReflection.GetPhaseId(phaseType)
                : phaseId;
            var interfaceTypeName = GetSpecificPhaseInterfaceName(phaseType);
            var order = orderOverride ?? phase.Order;

            _executions.Add(new PhaseExecution
            {
                Phase = phase,
                PhaseId = resolvedId,
                InterfaceTypeName = interfaceTypeName,
                Order = order,
                IsManual = true
            });

            SortExecutions();
        }

        public void RemovePhase(TPhase phase)
        {
            if (phase == null)
                return;
            _executions.RemoveAll(e => ReferenceEquals(e.Phase, phase));
        }

        public void RegisterHook(object hook)
        {
            if (hook == null)
                return;
            if (_isExecuting)
            {
                Log.Warning($"[{Id}] 管线执行中，忽略添加钩子: {hook.GetType().Name}");
                return;
            }

            var targets = HookTargetResolver.GetTargetsForPipeline(hook.GetType(), Id);
            if (targets == null)
                return;

            foreach (var target in targets)
            {
                if (target == null || string.IsNullOrEmpty(target.phaseId))
                    continue;
                AddHook(target.phaseId, hook);
            }
        }

        public void ClearManualHooks()
        {
            foreach (var execution in _executions)
            {
                execution.BeforeHooks.RemoveAll(h => h.IsManual);
                execution.AfterHooks.RemoveAll(h => h.IsManual);
            }
        }

        public void AddBeforeHook(string phaseId, object hook)
        {
            AddBeforeHook(phaseId, hook, null);
        }

        public void AddBeforeHook(string phaseId, object hook, int? orderOverride)
        {
            if (string.IsNullOrEmpty(phaseId) || hook == null)
                return;
            if (_isExecuting)
            {
                Log.Warning($"[{Id}] 管线执行中，忽略添加前置钩子: {hook.GetType().Name}");
                return;
            }

            EnsureBuilt();
            var invoker = CreateBeforeInvoker(hook);
            if (invoker == null)
            {
                Log.Warning($"[{Id}] 前置钩子 {hook.GetType().Name} 未实现 IBeforePhaseHook");
                return;
            }

            invoker.IsManual = true;
            if (orderOverride.HasValue)
                invoker.Order = orderOverride.Value;

            var added = false;
            foreach (var execution in _executions)
            {
                if (!string.Equals(execution.PhaseId, phaseId, StringComparison.OrdinalIgnoreCase))
                    continue;
                execution.BeforeHooks.Add(CloneBeforeInvoker(invoker));
                SortHooks(execution);
                added = true;
            }

            if (!added)
                Log.Warning($"[{Id}] 未找到阶段 {phaseId}，无法添加前置钩子 {hook.GetType().Name}");
        }

        public void AddAfterHook(string phaseId, object hook)
        {
            AddAfterHook(phaseId, hook, null);
        }

        public void AddAfterHook(string phaseId, object hook, int? orderOverride)
        {
            if (string.IsNullOrEmpty(phaseId) || hook == null)
                return;
            if (_isExecuting)
            {
                Log.Warning($"[{Id}] 管线执行中，忽略添加后置钩子: {hook.GetType().Name}");
                return;
            }

            EnsureBuilt();
            var invoker = CreateAfterInvoker(hook);
            if (invoker == null)
            {
                Log.Warning($"[{Id}] 后置钩子 {hook.GetType().Name} 未实现 IAfterPhaseHook");
                return;
            }

            invoker.IsManual = true;
            if (orderOverride.HasValue)
                invoker.Order = orderOverride.Value;

            var added = false;
            foreach (var execution in _executions)
            {
                if (!string.Equals(execution.PhaseId, phaseId, StringComparison.OrdinalIgnoreCase))
                    continue;
                execution.AfterHooks.Add(CloneAfterInvoker(invoker));
                SortHooks(execution);
                added = true;
            }

            if (!added)
                Log.Warning($"[{Id}] 未找到阶段 {phaseId}，无法添加后置钩子 {hook.GetType().Name}");
        }

        public void AddHook(string phaseId, object hook)
        {
            AddHook(phaseId, hook, null, null);
        }

        public void AddHook(string phaseId, object hook, int? beforeOrderOverride, int? afterOrderOverride)
        {
            if (hook == null || string.IsNullOrEmpty(phaseId))
                return;
            if (_isExecuting)
            {
                Log.Warning($"[{Id}] 管线执行中，忽略添加钩子: {hook.GetType().Name}");
                return;
            }

            var added = false;
            if (hook is IBeforePhaseHook || hook.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBeforePhaseHook<>)))
            {
                AddBeforeHook(phaseId, hook, beforeOrderOverride);
                added = true;
            }

            if (hook is IAfterPhaseHook || hook.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAfterPhaseHook<>)))
            {
                AddAfterHook(phaseId, hook, afterOrderOverride);
                added = true;
            }

            if (!added)
                Log.Warning($"[{Id}] 钩子 {hook.GetType().Name} 未实现 IBeforePhaseHook 或 IAfterPhaseHook");
        }

        #endregion

        /// <summary>
        /// 执行管线
        /// </summary>
        public virtual async UniTask ExecuteAsync(TContext context)
        {
            EnsureBuilt();

            if (_executions.Count == 0)
            {
                Log.Warning($"[{Id}] 没有可执行的阶段");
                return;
            }

            _isExecuting = true;
            try
            {
                foreach (var execution in _executions)
                {
                    if (context.Aborted)
                    {
                        if (!SilentMode)
                            Log.Warning($"[{Id}] 管线已中断，跳过阶段: {execution.PhaseId}");
                        break;
                    }

                    if (!ShouldExecutePhase(execution.Phase, context))
                        continue;

                    var hookResult = await ExecuteBeforeHooksAsync(execution, context);
                    if (hookResult == HookResult.Abort)
                    {
                        if (!SilentMode)
                            Log.Warning($"[{Id}] 阶段 {execution.PhaseId} 被前置钩子中断");
                        context.Aborted = true;
                        break;
                    }

                    if (hookResult == HookResult.SkipAll)
                    {
                        if (!SilentMode)
                            Log.Info($"[{Id}] 阶段 {execution.PhaseId} 被前置钩子跳过（含后置钩子）");
                        continue;
                    }

                    if (hookResult == HookResult.SkipPhase)
                    {
                        if (!SilentMode)
                            Log.Info($"[{Id}] 阶段 {execution.PhaseId} 被前置钩子跳过");
                        await ExecuteAfterHooksAsync(execution, context);
                        continue;
                    }

                    try
                    {
                        if (!SilentMode)
                            Log.Info($"[{Id}] 执行阶段: {execution.PhaseId}");
                        await execution.Phase.ExecuteAsync(context);
                    }
                    catch (Exception e)
                    {
                        Log.Exception(e);
                        context.Aborted = true;
                        break;
                    }

                    await ExecuteAfterHooksAsync(execution, context);
                }
            }
            finally
            {
                _isExecuting = false;
            }
        }

        /// <summary>
        /// 判断是否应该执行阶段（子类可重写）
        /// </summary>
        protected virtual bool ShouldExecutePhase(TPhase phase, TContext context)
        {
            return true;
        }

        private async UniTask<HookResult> ExecuteBeforeHooksAsync(PhaseExecution execution, TContext context)
        {
            foreach (var hook in execution.BeforeHooks)
            {
                try
                {
                    var result = await hook.Invoke(context);
                    if (result == HookResult.SkipHooks)
                        return HookResult.Continue;
                    if (result != HookResult.Continue)
                        return result;
                }
                catch (Exception e)
                {
                    Log.Error($"[{Id}] 钩子 {hook.Source?.GetType().Name} 执行失败: {e}");
                }
            }

            return HookResult.Continue;
        }

        private async UniTask ExecuteAfterHooksAsync(PhaseExecution execution, TContext context)
        {
            foreach (var hook in execution.AfterHooks)
            {
                try
                {
                    await hook.Invoke(context);
                }
                catch (Exception e)
                {
                    Log.Error($"[{Id}] 钩子 {hook.Source?.GetType().Name} 执行失败: {e}");
                }
            }
        }

        private void EnsureBuilt()
        {
            if (!_dirty)
                return;

            var manualPhases = CaptureManualPhases();
            var manualHooks = CaptureManualHooks();

            _executions.Clear();

            if (_registry != null && _registryEntry != null)
                BuildFromRegistry();

            RestoreManualPhases(manualPhases);
            RestoreManualHooks(manualHooks);
            SortExecutions();
            SortHooks();

            _dirty = false;
        }

        private void BuildFromRegistry()
        {
            if (_registry == null || _registryEntry == null)
                return;

            var phases = _registry.GetOrderedPhases(Id);
            foreach (var entry in phases)
            {
                var type = entry.GetRuntimeType();
                if (type == null) continue;

                if (Activator.CreateInstance(type) is not TPhase phase)
                {
                    Log.Warning($"[{Id}] 阶段 {entry.typeName} 未实现 {typeof(TPhase).Name}");
                    continue;
                }

                var phaseId = string.IsNullOrEmpty(entry.phaseId)
                    ? PipelineReflection.GetPhaseId(type)
                    : entry.phaseId;

                _executions.Add(new PhaseExecution
                {
                    Phase = phase,
                    PhaseId = phaseId,
                    InterfaceTypeName = entry.interfaceTypeName,
                    Order = entry.order,
                    IsManual = false
                });
            }

            var registryHooks = _registryEntry.hooks.Where(h => h.enabled).ToList();
            ApplyHooksFromRegistry(registryHooks);
        }

        private void ApplyHooksFromRegistry(List<HookEntry> hooks)
        {
            var hookInstances = new Dictionary<string, object>();

            foreach (var entry in hooks)
            {
                var hookType = entry.GetRuntimeType();
                if (hookType == null) continue;

                if (!hookInstances.TryGetValue(entry.typeName, out var instance))
                {
                    try
                    {
                        instance = Activator.CreateInstance(hookType);
                        hookInstances[entry.typeName] = instance;
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"[{Id}] 创建钩子 {entry.typeName} 失败: {e.Message}");
                        continue;
                    }
                }

                ApplyHookInstance(entry, instance);
            }
        }

        private void ApplyHookInstance(HookEntry entry, object instance)
        {
            var targets = entry.targets == null || entry.targets.Count == 0
                ? null
                : entry.targets.Select(t => t.phaseId).ToList();

            if (targets == null || targets.Count == 0)
                return;

            if (targets.Any(string.IsNullOrEmpty))
                return;

            foreach (var execution in _executions)
            {
                if (!TargetsMatchPhase(execution, targets))
                    continue;

                if (entry.isBefore)
                {
                    var invoker = CreateBeforeInvoker(instance);
                    if (invoker == null)
                    {
                        Log.Warning($"[{Id}] 前置钩子 {instance.GetType().Name} 未实现 IBeforePhaseHook");
                        break;
                    }
                    execution.BeforeHooks.Add(invoker);
                }
                else
                {
                    var invoker = CreateAfterInvoker(instance);
                    if (invoker == null)
                    {
                        Log.Warning($"[{Id}] 后置钩子 {instance.GetType().Name} 未实现 IAfterPhaseHook");
                        break;
                    }
                    execution.AfterHooks.Add(invoker);
                }
            }
        }

        private static bool TargetsMatchPhase(PhaseExecution execution, List<string> targets)
        {
            if (targets == null || targets.Count == 0)
                return true;

            var phaseType = execution.Phase.GetType();
            foreach (var target in targets)
            {
                if (PipelineReflection.MatchesPhaseTarget(phaseType, execution.PhaseId, execution.InterfaceTypeName, target))
                    return true;
            }

            return false;
        }

        private static BeforeHookInvoker CreateBeforeInvoker(object hook)
        {
            if (hook is IBeforePhaseHook<TContext> typed)
            {
                return new BeforeHookInvoker
                {
                    Order = typed.Order,
                    Invoke = typed.OnBeforeAsync,
                    Source = hook,
                    IsManual = false
                };
            }

            if (hook is IBeforePhaseHook basic)
            {
                return new BeforeHookInvoker
                {
                    Order = basic.Order,
                    Invoke = context => basic.OnBeforeAsync(context),
                    Source = hook,
                    IsManual = false
                };
            }

            return null;
        }

        private static AfterHookInvoker CreateAfterInvoker(object hook)
        {
            if (hook is IAfterPhaseHook<TContext> typed)
            {
                return new AfterHookInvoker
                {
                    Order = typed.Order,
                    Invoke = typed.OnAfterAsync,
                    Source = hook,
                    IsManual = false
                };
            }

            if (hook is IAfterPhaseHook basic)
            {
                return new AfterHookInvoker
                {
                    Order = basic.Order,
                    Invoke = context => basic.OnAfterAsync(context),
                    Source = hook,
                    IsManual = false
                };
            }

            return null;
        }

        private static BeforeHookInvoker CloneBeforeInvoker(BeforeHookInvoker source)
        {
            return new BeforeHookInvoker
            {
                Order = source.Order,
                Invoke = source.Invoke,
                Source = source.Source,
                IsManual = source.IsManual
            };
        }

        private static AfterHookInvoker CloneAfterInvoker(AfterHookInvoker source)
        {
            return new AfterHookInvoker
            {
                Order = source.Order,
                Invoke = source.Invoke,
                Source = source.Source,
                IsManual = source.IsManual
            };
        }

        private void SortExecutions()
        {
            _executions.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        private void SortHooks()
        {
            foreach (var execution in _executions)
                SortHooks(execution);
        }

        private static void SortHooks(PhaseExecution execution)
        {
            execution.BeforeHooks = execution.BeforeHooks.OrderBy(h => h.Order).ToList();
            execution.AfterHooks = execution.AfterHooks.OrderBy(h => h.Order).ToList();
        }

        private List<PhaseExecution> CaptureManualPhases()
        {
            var result = new List<PhaseExecution>();
            foreach (var execution in _executions)
            {
                if (!execution.IsManual)
                    continue;

                result.Add(new PhaseExecution
                {
                    Phase = execution.Phase,
                    PhaseId = execution.PhaseId,
                    InterfaceTypeName = execution.InterfaceTypeName,
                    Order = execution.Order,
                    IsManual = true
                });
            }
            return result;
        }

        private Dictionary<string, ManualHookSet> CaptureManualHooks()
        {
            var result = new Dictionary<string, ManualHookSet>(StringComparer.OrdinalIgnoreCase);
            foreach (var execution in _executions)
            {
                if (string.IsNullOrEmpty(execution.PhaseId))
                    continue;

                foreach (var hook in execution.BeforeHooks)
                {
                    if (!hook.IsManual)
                        continue;

                    if (!result.TryGetValue(execution.PhaseId, out var set))
                    {
                        set = new ManualHookSet();
                        result[execution.PhaseId] = set;
                    }
                    set.Before.Add(hook);
                }

                foreach (var hook in execution.AfterHooks)
                {
                    if (!hook.IsManual)
                        continue;

                    if (!result.TryGetValue(execution.PhaseId, out var set))
                    {
                        set = new ManualHookSet();
                        result[execution.PhaseId] = set;
                    }
                    set.After.Add(hook);
                }
            }
            return result;
        }

        private void RestoreManualPhases(List<PhaseExecution> manualPhases)
        {
            if (manualPhases == null || manualPhases.Count == 0)
                return;

            foreach (var execution in manualPhases)
                _executions.Add(execution);
        }

        private void RestoreManualHooks(Dictionary<string, ManualHookSet> manualHooks)
        {
            if (manualHooks == null || manualHooks.Count == 0)
                return;

            foreach (var execution in _executions)
            {
                if (string.IsNullOrEmpty(execution.PhaseId))
                    continue;

                if (!manualHooks.TryGetValue(execution.PhaseId, out var set))
                    continue;

                foreach (var hook in set.Before)
                    execution.BeforeHooks.Add(CloneBeforeInvoker(hook));
                foreach (var hook in set.After)
                    execution.AfterHooks.Add(CloneAfterInvoker(hook));
            }
        }

        private sealed class ManualHookSet
        {
            public List<BeforeHookInvoker> Before = new();
            public List<AfterHookInvoker> After = new();
        }

        private static string GetSpecificPhaseInterfaceName(Type phaseType)
        {
            foreach (var iface in phaseType.GetInterfaces())
            {
                if (iface == typeof(IPhase)) continue;
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IPhase<>)) continue;
                if (typeof(IPhase).IsAssignableFrom(iface))
                    return iface.FullName;
            }
            return null;
        }
    }
}
