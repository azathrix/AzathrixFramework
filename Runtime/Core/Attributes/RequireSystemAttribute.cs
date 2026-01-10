using System;

namespace Azathrix.Framework.Core.Attributes
{
    /// <summary>
    /// 标记系统依赖，确保依赖的系统先初始化
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireSystemAttribute : Attribute
    {
        public Type DependencyType { get; }

        public RequireSystemAttribute(Type dependencyType)
        {
            DependencyType = dependencyType;
        }
    }
}
