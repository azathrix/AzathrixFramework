namespace Azathrix.Framework.Events.Interceptors
{
    /// <summary>
    /// 拦截结果
    /// </summary>
    public enum InterceptResult
    {
        /// <summary>
        /// 继续执行
        /// </summary>
        Continue,

        /// <summary>
        /// 取消事件分发
        /// </summary>
        Cancel
    }

    /// <summary>
    /// 拦截器上下文
    /// </summary>
    public ref struct InterceptorContext<T> where T : struct
    {
        public T Event;
        public readonly object Sender;

        public InterceptorContext(ref T evt, object sender)
        {
            Event = evt;
            Sender = sender;
        }
    }

    /// <summary>
    /// 拦截器处理器委托
    /// </summary>
    public delegate InterceptResult InterceptorHandler<T>(ref InterceptorContext<T> ctx) where T : struct;
}
