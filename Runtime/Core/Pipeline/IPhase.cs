using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Pipeline
{
    /// <summary>
    /// 管线阶段基础接口
    /// </summary>
    public interface IPhase
    {
        /// <summary>
        /// 执行顺序（数值越小越先执行）
        /// </summary>
        int Order { get; }
    }

    /// <summary>
    /// 带上下文的阶段接口
    /// </summary>
    public interface IPhase<in TContext> : IPhase
    {
        UniTask ExecuteAsync(TContext context);
    }

    /// <summary>
    /// 阶段ID特性
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface, Inherited = true)]
    public class PhaseIdAttribute : System.Attribute
    {
        public string Id { get; }
        public PhaseIdAttribute(string id) => Id = id;
    }

    /// <summary>
    /// 阶段显示名称特性
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface, Inherited = true)]
    public class PhaseDisplayNameAttribute : System.Attribute
    {
        public string DisplayName { get; }
        public PhaseDisplayNameAttribute(string displayName) => DisplayName = displayName;
    }
}
