using System;

namespace Azathrix.Framework.Core.Pipeline
{
    /// <summary>
    /// 管线接口
    /// </summary>
    public interface IPipeline
    {
        /// <summary>
        /// 管线ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 显示名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 阶段基类型
        /// </summary>
        Type PhaseType { get; }

        /// <summary>
        /// 钩子基类型
        /// </summary>
        Type HookType { get; }

        /// <summary>
        /// 上下文类型
        /// </summary>
        Type ContextType { get; }
    }

    /// <summary>
    /// 管线ID特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PipelineIdAttribute : Attribute
    {
        public string Id { get; }
        public PipelineIdAttribute(string id) => Id = id;
    }

    /// <summary>
    /// 管线显示名称特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PipelineDisplayNameAttribute : Attribute
    {
        public string DisplayName { get; }
        public PipelineDisplayNameAttribute(string displayName) => DisplayName = displayName;
    }
}
