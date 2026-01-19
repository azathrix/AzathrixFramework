namespace Azathrix.Framework.Events.Interceptors
{
    /// <summary>
    /// 拦截结果
    /// </summary>
    public enum InterceptResult
    {
        /// <summary>
        /// 继续执行后续拦截器和事件处理器
        /// </summary>
        Continue,

        /// <summary>
        /// 取消事件分发，不再执行后续拦截器和处理器
        /// </summary>
        Cancel
    }

    /// <summary>
    /// 拦截器上下文，包含事件数据和发送者信息
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <remarks>
    /// 使用ref struct避免堆分配，提高性能
    /// </remarks>
    public ref struct InterceptorContext<T> where T : struct
    {
        /// <summary>
        /// 事件数据（可修改）
        /// </summary>
        public T Event;

        /// <summary>
        /// 事件发送者
        /// </summary>
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
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="ctx">拦截器上下文</param>
    /// <returns>拦截结果</returns>
    /// <example>
    /// <code>
    /// // 伤害减免拦截器
    /// dispatcher.AddInterceptor&lt;PlayerDamageEvent&gt;(ref ctx => {
    ///     ctx.Event.Damage = (int)(ctx.Event.Damage * 0.8f);  // 减免20%
    ///     return InterceptResult.Continue;
    /// });
    ///
    /// // 伤害免疫拦截器
    /// dispatcher.AddInterceptor&lt;PlayerDamageEvent&gt;(ref ctx => {
    ///     if (isInvincible) return InterceptResult.Cancel;  // 取消事件
    ///     return InterceptResult.Continue;
    /// }, priority: 100);  // 高优先级先执行
    /// </code>
    /// </example>
    public delegate InterceptResult InterceptorHandler<T>(ref InterceptorContext<T> ctx) where T : struct;
}
