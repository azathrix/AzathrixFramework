using Azathrix.Framework.Interfaces;

namespace Azathrix.Framework.Core
{
    /// <summary>
    /// 框架初始化上下文，用于配置框架启动时的各项参数
    /// </summary>
    public class SetupContext
    {
        /// <summary>
        /// 资源加载器，用于加载游戏资源
        /// </summary>
        public IResourcesLoader ResourcesLoader { get; set; }

        /// <summary>
        /// 日志记录器，用于输出框架日志
        /// </summary>
        public ILogger Logger { get; set; }
    }
}
