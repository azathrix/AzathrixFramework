using System;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Pipeline
{
    /// <summary>
    /// 钩子执行结果
    /// </summary>
    public enum HookResult
    {
        /// <summary>继续执行</summary>
        Continue,
        /// <summary>跳过当前阶段，继续后续阶段</summary>
        SkipPhase,
        /// <summary>中断整个管线</summary>
        Abort
    }

    /// <summary>
    /// 管线钩子基础接口
    /// </summary>
    public interface IHook
    {
        /// <summary>
        /// 执行顺序
        /// </summary>
        int Order { get; }

        /// <summary>
        /// 判断是否匹配指定阶段
        /// </summary>
        bool Match(string phaseId, Type phaseType);
    }

    /// <summary>
    /// 带上下文的钩子接口
    /// </summary>
    public interface IHook<in TContext> : IHook
    {
        /// <summary>
        /// 阶段执行前调用
        /// </summary>
        UniTask<HookResult> OnBeforeAsync(string phaseId, TContext context);

        /// <summary>
        /// 阶段执行后调用
        /// </summary>
        UniTask OnAfterAsync(string phaseId, TContext context);
    }

    /// <summary>
    /// 阶段执行前钩子（类型匹配）
    /// </summary>
    public interface IBeforePhaseHook<TPhase, in TContext>
        where TPhase : IPhase
    {
        int Order { get; }
        UniTask<HookResult> OnBeforeAsync(TContext context);
    }

    /// <summary>
    /// 阶段执行后钩子（类型匹配）
    /// </summary>
    public interface IAfterPhaseHook<TPhase, in TContext>
        where TPhase : IPhase
    {
        int Order { get; }
        UniTask OnAfterAsync(TContext context);
    }
}
