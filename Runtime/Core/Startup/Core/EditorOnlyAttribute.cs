using System;

namespace Azathrix.Framework.Core.Startup
{
    /// <summary>
    /// 标记阶段仅在编辑器模式执行（不会在运行时执行）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EditorOnlyAttribute : Attribute { }
}
