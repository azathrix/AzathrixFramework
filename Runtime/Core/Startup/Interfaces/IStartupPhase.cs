using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Startup
{
    /// <summary>
    /// 启动阶段基础接口
    /// </summary>
    public interface IStartupPhase
    {
        /// <summary>
        /// 阶段 ID（用于 Hook 匹配）
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 执行顺序（数值越小越先执行）
        /// </summary>
        int Order { get; }

        UniTask ExecuteAsync(PhaseContext context);
    }
}
