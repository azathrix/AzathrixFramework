using System;
using System.Collections.Generic;
using Azathrix.Framework.Events.Handlers;
using Azathrix.Framework.Events.Internal;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// 查询订阅结果
    /// </summary>
    public struct QuerySubscriptionResult<T, TResult> : IDisposable where T : struct
    {
        private readonly EventDispatcher _dispatcher;
        private readonly uint _id;

        internal QuerySubscriptionResult(EventDispatcher dispatcher, uint id)
        {
            _dispatcher = dispatcher;
            _id = id;
        }

        public uint Id => _id;
        public bool IsValid => _dispatcher != null && _id != 0;

        public void Unsubscribe()
        {
            _dispatcher?.UnsubscribeQuery<T, TResult>(_id);
        }

        public void Dispose()
        {
            Unsubscribe();
        }
    }

    /// <summary>
    /// 查询通道
    /// </summary>
    internal sealed class QueryChannel<T, TResult> where T : struct
    {
        private readonly EventDispatcher _dispatcher;
        private readonly QueryHandlerList<T, TResult> _handlers;

        public QueryChannel(EventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _handlers = new QueryHandlerList<T, TResult>();
        }

        public uint Subscribe(QueryHandler<T, TResult> handler, int priority = 0)
        {
            var id = _dispatcher.GenerateId();
            _handlers.Add(id, handler, priority);
            return id;
        }

        public void Unsubscribe(uint id)
        {
            _handlers.Remove(id);
        }

        public TResult Query(ref T evt, Func<TResult, TResult, TResult> aggregator, TResult initial)
        {
            return _handlers.Query(ref evt, aggregator, initial);
        }

        public (bool hasResult, TResult result) QueryFirst(ref T evt)
        {
            return _handlers.QueryFirst(ref evt);
        }

        public int Count => _handlers.Count;

        public void Clear()
        {
            _handlers.Clear();
        }
    }

    /// <summary>
    /// EventDispatcher - Query相关方法
    /// </summary>
    public partial class EventDispatcher
    {
        // 查询通道缓存
        private static class QueryChannelCache<T, TResult> where T : struct
        {
            public static readonly Dictionary<EventDispatcher, QueryChannel<T, TResult>> Channels = new();
        }

        private QueryChannel<T, TResult> GetOrCreateQueryChannel<T, TResult>() where T : struct
        {
            if (!QueryChannelCache<T, TResult>.Channels.TryGetValue(this, out var channel))
            {
                channel = new QueryChannel<T, TResult>(this);
                QueryChannelCache<T, TResult>.Channels[this] = channel;
                _cleanupActions.Add(() => QueryChannelCache<T, TResult>.Channels.Remove(this));
            }
            return channel;
        }

        /// <summary>
        /// 订阅查询事件
        /// </summary>
        public QuerySubscriptionResult<T, TResult> SubscribeQuery<T, TResult>(QueryHandler<T, TResult> handler, int priority = 0) where T : struct
        {
            var channel = GetOrCreateQueryChannel<T, TResult>();
            var id = channel.Subscribe(handler, priority);
            return new QuerySubscriptionResult<T, TResult>(this, id);
        }

        /// <summary>
        /// 取消查询订阅
        /// </summary>
        internal void UnsubscribeQuery<T, TResult>(uint id) where T : struct
        {
            if (QueryChannelCache<T, TResult>.Channels.TryGetValue(this, out var channel))
            {
                channel.Unsubscribe(id);
            }
        }

        /// <summary>
        /// 查询并聚合结果
        /// </summary>
        public TResult Query<T, TResult>(T evt, Func<TResult, TResult, TResult> aggregator, TResult initial = default) where T : struct
        {
            if (!QueryChannelCache<T, TResult>.Channels.TryGetValue(this, out var channel))
                return initial;

            return channel.Query(ref evt, aggregator, initial);
        }

        /// <summary>
        /// 查询并聚合结果（ref版本）
        /// </summary>
        public TResult Query<T, TResult>(ref T evt, Func<TResult, TResult, TResult> aggregator, TResult initial = default) where T : struct
        {
            if (!QueryChannelCache<T, TResult>.Channels.TryGetValue(this, out var channel))
                return initial;

            return channel.Query(ref evt, aggregator, initial);
        }

        /// <summary>
        /// 查询第一个结果
        /// </summary>
        public TResult QueryFirst<T, TResult>(T evt, TResult defaultValue = default) where T : struct
        {
            if (!QueryChannelCache<T, TResult>.Channels.TryGetValue(this, out var channel))
                return defaultValue;

            var (hasResult, result) = channel.QueryFirst(ref evt);
            return hasResult ? result : defaultValue;
        }

        /// <summary>
        /// 查询第一个结果（ref版本）
        /// </summary>
        public TResult QueryFirst<T, TResult>(ref T evt, TResult defaultValue = default) where T : struct
        {
            if (!QueryChannelCache<T, TResult>.Channels.TryGetValue(this, out var channel))
                return defaultValue;

            var (hasResult, result) = channel.QueryFirst(ref evt);
            return hasResult ? result : defaultValue;
        }

        /// <summary>
        /// 尝试查询第一个结果
        /// </summary>
        public bool TryQueryFirst<T, TResult>(T evt, out TResult result) where T : struct
        {
            result = default;
            if (!QueryChannelCache<T, TResult>.Channels.TryGetValue(this, out var channel))
                return false;

            var (hasResult, value) = channel.QueryFirst(ref evt);
            if (hasResult)
            {
                result = value;
                return true;
            }
            return false;
        }
    }
}
