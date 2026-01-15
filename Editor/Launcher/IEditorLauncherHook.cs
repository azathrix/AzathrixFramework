using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;

namespace Azathrix.Framework.Editor.Launcher
{
    /// <summary>
    /// 编辑器启动钩子接口
    /// </summary>
    public interface IEditorLauncherHook : IHook<LauncherContext>
    {
    }

    /// <summary>
    /// 编辑器阶段执行前钩子
    /// </summary>
    public interface IBeforeEditorPhaseHook : IBeforePhaseHook<LauncherContext>
    {
    }

    /// <summary>
    /// 编辑器阶段执行后钩子
    /// </summary>
    public interface IAfterEditorPhaseHook : IAfterPhaseHook<LauncherContext>
    {
    }
}
