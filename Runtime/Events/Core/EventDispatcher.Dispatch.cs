using Azathrix.Framework.Events.Handlers;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// EventDispatcher - 分发相关方法
    /// </summary>
    public partial class EventDispatcher
    {
        /// <summary>
        /// 分发事件（即时同步执行所有订阅者）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件数据</param>
        /// <param name="sender">发送者（可选）</param>
        /// <example>
        /// <code>
        /// dispatcher.Dispatch(new PlayerDamageEvent { Damage = 10 });
        /// </code>
        /// </example>
        public void Dispatch<T>(T evt, object sender = null) where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Dispatch(ref evt, sender);
            }
        }

        /// <summary>
        /// 分发事件（ref版本，避免struct复制，性能更好）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件数据引用</param>
        /// <param name="sender">发送者（可选）</param>
        /// <example>
        /// <code>
        /// var evt = new PlayerDamageEvent { Damage = 10 };
        /// dispatcher.Dispatch(ref evt);  // 避免复制
        /// </code>
        /// </example>
        public void Dispatch<T>(ref T evt, object sender = null) where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Dispatch(ref evt, sender);
            }
        }

        /// <summary>
        /// 分发默认事件（使用struct默认值）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="sender">发送者（可选）</param>
        /// <example>
        /// <code>
        /// // 适用于不需要携带数据的事件
        /// dispatcher.Dispatch&lt;GameStartEvent&gt;();
        /// </code>
        /// </example>
        public void Dispatch<T>(object sender = null) where T : struct
        {
            var evt = new T();
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Dispatch(ref evt, sender);
            }
        }

        /// <summary>
        /// 分发事件（使用初始化器，避免创建临时变量）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="initializer">事件初始化器</param>
        /// <param name="sender">发送者（可选）</param>
        /// <example>
        /// <code>
        /// dispatcher.Dispatch&lt;PlayerDamageEvent&gt;(ref evt => {
        ///     evt.Damage = 10;
        ///     evt.Source = "Enemy";
        /// });
        /// </code>
        /// </example>
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
        /// 分发 Sticky 事件（保存值，新订阅者可立即收到最后一个值）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件数据</param>
        /// <param name="sender">发送者（可选）</param>
        /// <remarks>
        /// Sticky事件会保存最后一个值，当新订阅者使用.Sticky()配置时，
        /// 会立即收到这个保存的值，适用于状态类事件
        /// </remarks>
        /// <example>
        /// <code>
        /// // 分发Sticky事件
        /// dispatcher.DispatchSticky(new GameStateEvent { State = GameState.Playing });
        ///
        /// // 新订阅者立即收到最后的状态
        /// dispatcher.Subscribe&lt;GameStateEvent&gt;(ref evt => {
        ///     Debug.Log($"当前状态: {evt.State}");
        /// }).Sticky();  // 立即收到 Playing 状态
        /// </code>
        /// </example>
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
