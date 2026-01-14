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
        public SubscriptionBuilder<T> Subscribe<T>(EventCallback<T> handler) where T : struct
        {
            return new SubscriptionBuilder<T>(this, handler);
        }

        /// <summary>
        /// 添加拦截器
        /// </summary>
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
