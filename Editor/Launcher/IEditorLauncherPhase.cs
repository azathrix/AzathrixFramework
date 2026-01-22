using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;

namespace Azathrix.Framework.Editor.Launcher
{
    /// <summary>
    /// 编辑器启动阶段接口
    /// </summary>
    public interface IEditorLauncherPhase : IPhase<LauncherContext>
    {
    }

    /// <summary>
    /// EditorSetup阶段 (Order: 0) - 编辑器初始化
    /// </summary>
    public interface IEditorSetupPhase : IEditorLauncherPhase { }

    /// <summary>
    /// EditorScan阶段 (Order: 200) - 编辑器扫描
    /// </summary>
    public interface IEditorScanPhase : IEditorLauncherPhase { }

    /// <summary>
    /// EditorRegister阶段 (Order: 300) - 编辑器注册
    /// </summary>
    public interface IEditorRegisterPhase : IEditorLauncherPhase { }

    /// <summary>
    /// EditorInit阶段 (Order: 500) - 编辑器初始化完成
    /// </summary>
    public interface IEditorInitPhase : IEditorLauncherPhase { }
}
