using Azathrix.Framework.Events.Handlers;
using Azathrix.Framework.Events.Interceptors;
using Azathrix.Framework.Events.Internal;
using Azathrix.Framework.Events.Results;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// EventDispatcher - 订阅相关方法
    /// </summary>
    public partial class EventDispatcher
    {
        /// <summary>
        /// 订阅事件（返回 Builder 支持链式配置）
        /// </summary>
        /// <typeparam name="T">事件类型（必须是struct）</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <returns>订阅构建器，支持链式配置</returns>
        /// <example>
        /// <code>
        /// // 基本订阅
        /// dispatcher.Subscribe&lt;PlayerDamageEvent&gt;(ref evt => {
        ///     Debug.Log($"受到 {evt.Damage} 点伤害");
        /// });
        ///
        /// // 链式配置
        /// dispatcher.Subscribe&lt;PlayerDamageEvent&gt;(ref evt => { ... })
        ///     .Where(ref evt => evt.Damage > 10)  // 只处理伤害大于10的事件
        ///     .Priority(100)                       // 高优先级
        ///     .Once()                              // 只触发一次
        ///     .AddTo(gameObject);                  // 绑定生命周期
        /// </code>
        /// </example>
        public SubscriptionBuilder<T> Subscribe<T>(EventCallback<T> handler) where T : struct
        {
            return new SubscriptionBuilder<T>(this, handler);
        }

        /// <summary>
        /// 添加拦截器（在事件分发前执行，可修改或取消事件）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">拦截器处理器</param>
        /// <param name="priority">优先级（越大越先执行）</param>
        /// <returns>订阅结果，可用于取消拦截器</returns>
        /// <example>
        /// <code>
        /// // 添加伤害减免拦截器
        /// dispatcher.AddInterceptor&lt;PlayerDamageEvent&gt;(ref ctx => {
        ///     ctx.Event.Damage = (int)(ctx.Event.Damage * 0.8f);  // 减免20%伤害
        ///     return InterceptResult.Continue;  // 继续分发
        /// }, priority: 100);
        ///
        /// // 添加伤害免疫拦截器
        /// dispatcher.AddInterceptor&lt;PlayerDamageEvent&gt;(ref ctx => {
        ///     if (isInvincible) return InterceptResult.Cancel;  // 取消事件
        ///     return InterceptResult.Continue;
        /// }, priority: 200);  // 更高优先级，先执行
        /// </code>
        /// </example>
        public SubscriptionResult AddInterceptor<T>(InterceptorHandler<T> handler, int priority = 0) where T : struct
        {
            var channel = GetOrCreateChannel<T>();
            var id = channel.AddInterceptor(handler, priority);
            return new SubscriptionResult(this, id, typeof(T));
        }

        /// <summary>
        /// 移除拦截器
        /// </summary>
        public void RemoveInterceptor<T>(uint id) where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.RemoveInterceptor(id);
            }
        }

        /// <summary>
        /// 获取订阅者数量
        /// </summary>
        public int GetSubscriberCount<T>() where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                return channel.SubscriberCount;
            }
            return 0;
        }

        /// <summary>
        /// 清空指定类型的所有订阅
        /// </summary>
        public void ClearSubscriptions<T>() where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Clear();
            }
        }
    }
}
