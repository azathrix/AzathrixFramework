using System;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Startup
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
    /// 通用阶段钩子（可匹配多个阶段）
    /// </summary>
    public interface IStartupHook
    {
        int Order { get; }

        /// <summary>
        /// 判断是否匹配指定阶段
        /// </summary>
        bool Match(string phaseId, Type phaseType);

        /// <summary>
        /// 阶段执行前调用
        /// </summary>
        UniTask<HookResult> OnBeforeAsync(string phaseId, PhaseContext context);

        /// <summary>
        /// 阶段执行后调用
        /// </summary>
        UniTask OnAfterAsync(string phaseId, PhaseContext context);
    }

    /// <summary>
    /// 阶段执行前钩子（类型匹配）
    /// </summary>
    public interface IBeforePhaseHook<TPhase> where TPhase : IStartupPhase
    {
        int Order { get; }
        UniTask<HookResult> OnBeforeAsync(PhaseContext context);
    }

    /// <summary>
    /// 阶段执行后钩子（类型匹配）
    /// </summary>
    public interface IAfterPhaseHook<TPhase> where TPhase : IStartupPhase
    {
        int Order { get; }
        UniTask OnAfterAsync(PhaseContext context);
    }
}
