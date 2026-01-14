using System;
using Azathrix.Framework.Events.Handlers;

namespace Azathrix.Framework.Events.Internal
{
    /// <summary>
    /// 查询处理器信息
    /// </summary>
    internal struct QuerySubscriber<T, TResult> where T : struct
    {
        public uint Id;
        public int Priority;
        public bool Removed;
        public QueryHandler<T, TResult> Handler;
    }

    /// <summary>
    /// 查询处理器列表
    /// </summary>
    internal sealed class QueryHandlerList<T, TResult> where T : struct
    {
        private QuerySubscriber<T, TResult>[] _subscribers;
        private int _count;
        private bool _needsSort;

        public QueryHandlerList(int initialCapacity = 8)
        {
            _subscribers = new QuerySubscriber<T, TResult>[initialCapacity];
            _count = 0;
        }

        public int Count => _count;

        public void Add(uint id, QueryHandler<T, TResult> handler, int priority = 0)
        {
            if (_count >= _subscribers.Length)
            {
                var newArray = new QuerySubscriber<T, TResult>[_subscribers.Length * 2];
                Array.Copy(_subscribers, newArray, _count);
                _subscribers = newArray;
            }

            _subscribers[_count++] = new QuerySubscriber<T, TResult>
            {
                Id = id,
                Priority = priority,
                Removed = false,
                Handler = handler
            };
            _needsSort = true;
        }

        public void Remove(uint id)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_subscribers[i].Id == id)
                {
                    _subscribers[i] = _subscribers[--_count];
                    _subscribers[_count] = default;
                    _needsSort = true;
                    return;
                }
            }
        }

        /// <summary>
        /// 查询并聚合结果
        /// </summary>
        public TResult Query(ref T evt, Func<TResult, TResult, TResult> aggregator, TResult initial)
        {
            if (_count == 0) return initial;

            if (_needsSort)
            {
                SortByPriority();
                _needsSort = false;
            }

            var result = initial;
            for (int i = 0; i < _count; i++)
            {
                ref var subscriber = ref _subscribers[i];
                if (subscriber.Removed) continue;

                var value = subscriber.Handler(ref evt);
                result = aggregator(result, value);
            }

            return result;
        }

        /// <summary>
        /// 查询第一个结果
        /// </summary>
        public (bool hasResult, TResult result) QueryFirst(ref T evt)
        {
            if (_count == 0) return (false, default);

            if (_needsSort)
            {
                SortByPriority();
                _needsSort = false;
            }

            for (int i = 0; i < _count; i++)
            {
                ref var subscriber = ref _subscribers[i];
                if (subscriber.Removed) continue;

                return (true, subscriber.Handler(ref evt));
            }

            return (false, default);
        }

        private void SortByPriority()
        {
            for (int i = 1; i < _count; i++)
            {
                var key = _subscribers[i];
                int j = i - 1;

                while (j >= 0 && _subscribers[j].Priority < key.Priority)
                {
                    _subscribers[j + 1] = _subscribers[j];
                    j--;
                }
                _subscribers[j + 1] = key;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _count; i++)
                _subscribers[i] = default;
            _count = 0;
        }
    }
}
