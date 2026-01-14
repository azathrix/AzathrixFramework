using System;
using System.Collections.Generic;
using Azathrix.Framework.Events.Handlers;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Events.Internal
{
    /// <summary>
    /// 异步处理器信息
    /// </summary>
    internal struct AsyncSubscriber<T> where T : struct
    {
        public uint Id;
        public int Priority;
        public bool Removed;
        public AsyncEventHandler<T> Handler;
    }

    /// <summary>
    /// 异步处理器列表
    /// </summary>
    internal sealed class AsyncHandlerList<T> where T : struct
    {
        private AsyncSubscriber<T>[] _subscribers;
        private int _count;
        private bool _needsSort;

        public AsyncHandlerList(int initialCapacity = 8)
        {
            _subscribers = new AsyncSubscriber<T>[initialCapacity];
            _count = 0;
        }

        public int Count => _count;

        public void Add(uint id, AsyncEventHandler<T> handler, int priority = 0)
        {
            if (_count >= _subscribers.Length)
            {
                var newArray = new AsyncSubscriber<T>[_subscribers.Length * 2];
                Array.Copy(_subscribers, newArray, _count);
                _subscribers = newArray;
            }

            _subscribers[_count++] = new AsyncSubscriber<T>
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
        /// 异步分发（等待所有处理器完成）
        /// </summary>
        public async UniTask DispatchAsync(T evt)
        {
            if (_count == 0) return;

            if (_needsSort)
            {
                SortByPriority();
                _needsSort = false;
            }

            // 收集所有任务
            var tasks = new List<UniTask>(_count);
            for (int i = 0; i < _count; i++)
            {
                var subscriber = _subscribers[i];
                if (subscriber.Removed) continue;

                tasks.Add(subscriber.Handler(evt));
            }

            // 等待所有任务完成
            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// 异步分发（顺序执行）
        /// </summary>
        public async UniTask DispatchSequentialAsync(T evt)
        {
            if (_count == 0) return;

            if (_needsSort)
            {
                SortByPriority();
                _needsSort = false;
            }

            for (int i = 0; i < _count; i++)
            {
                var subscriber = _subscribers[i];
                if (subscriber.Removed) continue;

                await subscriber.Handler(evt);
            }
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
