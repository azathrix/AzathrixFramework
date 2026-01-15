using System;

namespace Azathrix.Framework.Core.Pipeline
{
    /// <summary>
    /// 标记为自动注册到管线注册表
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RegisterAttribute : Attribute
    {
        public string PipelineId { get; }

        public RegisterAttribute(string pipelineId = null)
        {
            PipelineId = pipelineId;
        }
    }
}
