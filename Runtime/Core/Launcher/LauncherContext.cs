using System;
using Azathrix.Framework.Core.Pipeline;

namespace Azathrix.Framework.Core.Launcher
{
    /// <summary>
    /// 启动上下文
    /// </summary>
    public class LauncherContext : PipelineContext
    {
        /// <summary>
        /// 静默模式（不输出日志）
        /// </summary>
        public bool SilentMode { get; set; }

        /// <summary>
        /// 扫描到的系统类型（Scan阶段填充）
        /// </summary>
        public Type[] ScannedSystemTypes { get; set; }
    }
}
