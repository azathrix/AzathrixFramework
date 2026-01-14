using Azathrix.Framework.Events.Handlers;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// EventDispatcher - 分发相关方法
    /// </summary>
    public partial class EventDispatcher
    {
        /// <summary>
        /// 分发事件
        /// </summary>
        public void Dispatch<T>(T evt, object sender = null) where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Dispatch(ref evt, sender);
            }
        }

        /// <summary>
        /// 分发事件（ref版本，避免复制）
        /// </summary>
        public void Dispatch<T>(ref T evt, object sender = null) where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Dispatch(ref evt, sender);
            }
        }

        /// <summary>
        /// 分发默认事件
        /// </summary>
        public void Dispatch<T>(object sender = null) where T : struct
        {
            var evt = new T();
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Dispatch(ref evt, sender);
            }
        }

        /// <summary>
        /// 分发事件（使用初始化器）
        /// </summary>
        public void Dispatch<T>(EventInitializer<T> initializer, object sender = null) where T : struct
        {
            var evt = new T();
            initializer(ref evt);
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Dispatch(ref evt, sender);
            }
        }

        /// <summary>
        /// 分发 Sticky 事件（保存值，新订阅者可立即收到）
        /// </summary>
        public void DispatchSticky<T>(T evt, object sender = null) where T : struct
        {
            var channel = GetOrCreateChannel<T>();
            channel.SetSticky(ref evt);
            channel.Dispatch(ref evt, sender);
        }

        /// <summary>
        /// 分发 Sticky 事件（ref版本）
        /// </summary>
        public void DispatchSticky<T>(ref T evt, object sender = null) where T : struct
        {
            var channel = GetOrCreateChannel<T>();
            channel.SetSticky(ref evt);
            channel.Dispatch(ref evt, sender);
        }

        /// <summary>
        /// 清除 Sticky 值
        /// </summary>
        public void ClearSticky<T>() where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.ClearSticky();
            }
        }
    }
}
