using Azathrix.Framework.Core.Pipeline;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Launcher
{
    /// <summary>
    /// 运行时启动钩子接口
    /// </summary>
    public interface ILauncherHook : IHook<LauncherContext>
    {
    }

    /// <summary>
    /// 阶段执行前钩子
    /// </summary>
    public interface IBeforeLauncherPhaseHook<TPhase> : IBeforePhaseHook<TPhase, LauncherContext>
        where TPhase : ILauncherPhase
    {
    }

    /// <summary>
    /// 阶段执行后钩子
    /// </summary>
    public interface IAfterLauncherPhaseHook<TPhase> : IAfterPhaseHook<TPhase, LauncherContext>
        where TPhase : ILauncherPhase
    {
    }
}
