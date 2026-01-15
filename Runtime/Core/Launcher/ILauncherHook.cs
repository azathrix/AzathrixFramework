using Azathrix.Framework.Core.Pipeline;

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
    public interface IBeforeLauncherPhaseHook : IBeforePhaseHook<LauncherContext>
    {
    }

    /// <summary>
    /// 阶段执行后钩子
    /// </summary>
    public interface IAfterLauncherPhaseHook : IAfterPhaseHook<LauncherContext>
    {
    }
}
