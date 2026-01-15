using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Pipeline
{
    /// <summary>
    /// 管线阶段基础接口
    /// </summary>
    public interface IPhase
    {
        /// <summary>
        /// 阶段ID（用于Hook匹配和日志）
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 执行顺序（数值越小越先执行）
        /// </summary>
        int Order { get; }
    }

    /// <summary>
    /// 带上下文的阶段接口
    /// </summary>
    public interface IPhase<in TContext> : IPhase
    {
        UniTask ExecuteAsync(TContext context);
    }
}
