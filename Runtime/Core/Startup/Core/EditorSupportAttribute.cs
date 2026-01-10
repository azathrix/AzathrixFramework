using System;

namespace Azathrix.Framework.Core.Startup
{
    /// <summary>
    /// 标记阶段支持编辑器模式执行
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EditorSupportAttribute : Attribute { }
}
