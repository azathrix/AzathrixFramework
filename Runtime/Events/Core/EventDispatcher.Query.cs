using System;
using System.Collections.Generic;
using Azathrix.Framework.Events.Handlers;
using Azathrix.Framework.Events.Internal;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// 查询订阅结果
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <typeparam name="TResult">返回值类型</typeparam>
    public struct QuerySubscriptionResult<T, TResult> : IDisposable where T : struct
    {
        private readonly EventDispatcher _dispatcher;
        private readonly uint _id;

        internal QuerySubscriptionResult(EventDispatcher dispatcher, uint id)
        {
            _dispatcher = dispatcher;
            _id = id;
        }

        /// <summary>
        /// 订阅ID
        /// </summary>
        public uint Id => _id;

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => _dispatcher != null && _id != 0;

        /// <summary>
        /// 取消订阅
        /// </summary>
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
    /// 查询通道（内部使用）
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

        /// <summary>
        /// 订阅查询
        /// </summary>
        public uint Subscribe(QueryHandler<T, TResult> handler, int priority = 0)
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
        /// 查询并聚合结果
        /// </summary>
        public TResult Query(ref T evt, Func<TResult, TResult, TResult> aggregator, TResult initial)
        {
            return _handlers.Query(ref evt, aggregator, initial);
        }

        /// <summary>
        /// 查询第一个结果
        /// </summary>
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
    /// <remarks>
    /// Query事件允许订阅者返回值，适用于需要从多个模块收集数据的场景
    /// </remarks>
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
        /// <typeparam name="T">事件类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="handler">查询处理器</param>
        /// <param name="priority">优先级（越大越先执行）</param>
        /// <returns>查询订阅结果</returns>
        /// <example>
        /// <code>
        /// // 订阅伤害计算查询
        /// dispatcher.SubscribeQuery&lt;DamageCalcEvent, int&gt;(ref evt => {
        ///     return evt.BaseDamage * 2;  // 返回计算后的伤害
        /// });
        /// </code>
        /// </example>
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
        /// 查询并聚合所有订阅者的返回值
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="evt">事件数据</param>
        /// <param name="aggregator">聚合函数，用于合并多个返回值</param>
        /// <param name="initial">初始值</param>
        /// <returns>聚合后的结果</returns>
        /// <example>
        /// <code>
        /// // 计算总伤害（累加所有订阅者返回的伤害值）
        /// int totalDamage = dispatcher.Query&lt;DamageCalcEvent, int&gt;(
        ///     new DamageCalcEvent { BaseDamage = 10 },
        ///     (a, b) => a + b,  // 聚合函数：累加
        ///     0                  // 初始值
        /// );
        /// </code>
        /// </example>
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
        /// 查询第一个订阅者的返回值（按优先级）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="evt">事件数据</param>
        /// <param name="defaultValue">无订阅者时的默认值</param>
        /// <returns>第一个订阅者的返回值或默认值</returns>
        /// <example>
        /// <code>
        /// // 获取第一个处理器返回的伤害值
        /// int damage = dispatcher.QueryFirst&lt;DamageCalcEvent, int&gt;(
        ///     new DamageCalcEvent { BaseDamage = 10 },
        ///     defaultValue: 0
        /// );
        /// </code>
        /// </example>
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
        /// 尝试查询第一个订阅者的返回值
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="evt">事件数据</param>
        /// <param name="result">输出结果</param>
        /// <returns>是否有订阅者返回了结果</returns>
        /// <example>
        /// <code>
        /// if (dispatcher.TryQueryFirst&lt;DamageCalcEvent, int&gt;(evt, out var damage))
        /// {
        ///     Debug.Log($"伤害: {damage}");
        /// }
        /// </code>
        /// </example>
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
