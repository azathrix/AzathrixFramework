using Azathrix.Framework.Core.Pipeline;

namespace Azathrix.Framework.Core.Launcher
{
    /// <summary>
    /// 运行时启动阶段接口
    /// </summary>
    public interface ILauncherPhase : IPhase<LauncherContext>
    {
    }

    /// <summary>
    /// Setup阶段 (Order: 0) - 初始化基础设施
    /// </summary>
    public interface ISetupPhase : ILauncherPhase { }

    /// <summary>
    /// Scan阶段 (Order: 200) - 扫描和发现
    /// </summary>
    public interface IScanPhase : ILauncherPhase { }

    /// <summary>
    /// Register阶段 (Order: 300) - 注册服务和系统
    /// </summary>
    public interface IRegisterPhase : ILauncherPhase { }

    /// <summary>
    /// Start阶段 (Order: 500) - 启动完成
    /// </summary>
    public interface IStartPhase : ILauncherPhase { }
}
