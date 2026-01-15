using System;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Pipeline
{
    /// <summary>
    /// 钩子执行结果
    /// </summary>
    public enum HookResult
    {
        /// <summary>继续执行（执行后续钩子、阶段和后置钩子）</summary>
        Continue,
        /// <summary>跳过后续前置钩子，直接执行阶段和后置钩子</summary>
        SkipHooks,
        /// <summary>跳过当前阶段（不执行阶段，但执行后置钩子）</summary>
        SkipPhase,
        /// <summary>跳过当前阶段和后置钩子（直接进入下一个阶段）</summary>
        SkipAll,
        /// <summary>中断整个管线（不再执行任何阶段和钩子）</summary>
        Abort
    }

    /// <summary>
    /// 阶段执行前钩子（非泛型）
    /// </summary>
    public interface IBeforePhaseHook
    {
        int Order { get; }
        UniTask<HookResult> OnBeforeAsync(PipelineContext context);
    }

    /// <summary>
    /// 阶段执行后钩子（非泛型）
    /// </summary>
    public interface IAfterPhaseHook
    {
        int Order { get; }
        UniTask OnAfterAsync(PipelineContext context);
    }

    /// <summary>
    /// 阶段执行前钩子（带上下文）
    /// </summary>
    public interface IBeforePhaseHook<in TContext>
        where TContext : PipelineContext
    {
        int Order { get; }
        UniTask<HookResult> OnBeforeAsync(TContext context);
    }

    /// <summary>
    /// 阶段执行后钩子（带上下文）
    /// </summary>
    public interface IAfterPhaseHook<in TContext>
        where TContext : PipelineContext
    {
        int Order { get; }
        UniTask OnAfterAsync(TContext context);
    }

    /// <summary>
    /// Hook 目标特性（可多次标记）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class HookTargetAttribute : Attribute
    {
        public string PipelineId { get; }
        public string PhaseId { get; }

        public HookTargetAttribute(string pipelineId, string phaseId = null)
        {
            PipelineId = pipelineId;
            PhaseId = phaseId;
        }
    }

    /// <summary>
    /// 全局钩子（包含前置与后置）
    /// </summary>
    public interface IHook : IBeforePhaseHook, IAfterPhaseHook
    {
    }

    /// <summary>
    /// 全局钩子（包含前置与后置，带上下文）
    /// </summary>
    public interface IHook<in TContext> : IBeforePhaseHook<TContext>, IAfterPhaseHook<TContext>
        where TContext : PipelineContext
    {
    }
}
