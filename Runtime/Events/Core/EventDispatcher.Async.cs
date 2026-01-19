using System.Collections.Generic;
using Azathrix.Framework.Events.Handlers;
using Azathrix.Framework.Events.Internal;
using Azathrix.Framework.Events.Results;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// 异步事件通道（内部使用）
    /// </summary>
    internal sealed class AsyncEventChannel<T> where T : struct
    {
        private readonly EventDispatcher _dispatcher;
        private readonly AsyncHandlerList<T> _handlers;

        public AsyncEventChannel(EventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _handlers = new AsyncHandlerList<T>();
        }

        /// <summary>
        /// 订阅异步事件
        /// </summary>
        public uint Subscribe(AsyncEventHandler<T> handler, int priority = 0)
        {
            var id = _dispatcher.GenerateId();
            _handlers.Add(id, handler, priority);
            return id;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Unsubscribe(uint id)
        {
            _handlers.Remove(id);
        }

        /// <summary>
        /// 并行分发（所有处理器同时执行）
        /// </summary>
        public UniTask DispatchAsync(T evt)
        {
            return _handlers.DispatchAsync(evt);
        }

        /// <summary>
        /// 顺序分发（按优先级依次执行）
        /// </summary>
        public UniTask DispatchSequentialAsync(T evt)
        {
            return _handlers.DispatchSequentialAsync(evt);
        }

        public int Count => _handlers.Count;

        public void Clear()
        {
            _handlers.Clear();
        }
    }

    /// <summary>
    /// EventDispatcher - 异步事件相关方法
    /// </summary>
    /// <remarks>
    /// 异步事件支持await，适用于需要等待处理完成的场景
    /// </remarks>
    public partial class EventDispatcher
    {
        // 异步通道缓存
        private static class AsyncChannelCache<T> where T : struct
        {
            public static readonly Dictionary<EventDispatcher, AsyncEventChannel<T>> Channels = new();
        }

        private AsyncEventChannel<T> GetOrCreateAsyncChannel<T>() where T : struct
        {
            if (!AsyncChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel = new AsyncEventChannel<T>(this);
                AsyncChannelCache<T>.Channels[this] = channel;
                _cleanupActions.Add(() => AsyncChannelCache<T>.Channels.Remove(this));
            }
            return channel;
        }

        /// <summary>
        /// 订阅异步事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">异步事件处理器</param>
        /// <param name="priority">优先级（越大越先执行）</param>
        /// <returns>订阅结果</returns>
        /// <example>
        /// <code>
        /// dispatcher.SubscribeAsync&lt;DataLoadEvent&gt;(async evt => {
        ///     await LoadDataAsync(evt.Path);
        ///     Debug.Log("数据加载完成");
        /// });
        /// </code>
        /// </example>
        public SubscriptionResult SubscribeAsync<T>(AsyncEventHandler<T> handler, int priority = 0) where T : struct
        {
            var channel = GetOrCreateAsyncChannel<T>();
            var id = channel.Subscribe(handler, priority);
            return new SubscriptionResult(this, id, typeof(T));
        }

        /// <summary>
        /// 异步分发事件（并行执行所有处理器，等待全部完成）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件数据</param>
        /// <returns>等待所有处理器完成的Task</returns>
        /// <example>
        /// <code>
        /// await dispatcher.DispatchAsync(new DataLoadEvent { Path = "data.json" });
        /// Debug.Log("所有处理器都已完成");
        /// </code>
        /// </example>
        public UniTask DispatchAsync<T>(T evt) where T : struct
        {
            if (!AsyncChannelCache<T>.Channels.TryGetValue(this, out var channel))
                return UniTask.CompletedTask;

            return channel.DispatchAsync(evt);
        }

        /// <summary>
        /// 异步分发事件（顺序执行处理器，按优先级依次执行）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件数据</param>
        /// <returns>等待所有处理器完成的Task</returns>
        /// <remarks>
        /// 与DispatchAsync不同，此方法会按优先级顺序依次执行处理器，
        /// 前一个完成后才执行下一个
        /// </remarks>
        public UniTask DispatchSequentialAsync<T>(T evt) where T : struct
        {
            if (!AsyncChannelCache<T>.Channels.TryGetValue(this, out var channel))
                return UniTask.CompletedTask;

            return channel.DispatchSequentialAsync(evt);
        }

        /// <summary>
        /// 异步分发默认事件
        /// </summary>
        public UniTask DispatchAsync<T>() where T : struct
        {
            return DispatchAsync(new T());
        }

        /// <summary>
        /// 异步分发事件（使用初始化器）
        /// </summary>
        public UniTask DispatchAsync<T>(EventInitializer<T> initializer) where T : struct
        {
            var evt = new T();
            initializer(ref evt);
            return DispatchAsync(evt);
        }
    }
}
