using System;
using System.Collections.Generic;

namespace Azathrix.Framework.Events.Internal
{
    /// <summary>
    /// 订阅者信息
    /// </summary>
    internal struct Subscriber<T> where T : struct
    {
        public uint Id;
        public int Priority;
        public bool Once;
        public bool Removed;
        public int Version;
        public Handlers.EventCallback<T> Handler;
    }

    /// <summary>
    /// 高性能处理器列表
    /// 支持在遍历时安全地添加/删除订阅者
    /// </summary>
    internal sealed class HandlerList<T> where T : struct
    {
        private Subscriber<T>[] _subscribers;
        private int _count;
        private int _version;
        private int _dispatchDepth;  // 嵌套分发深度计数器
        private bool _needsSort;
        private bool _needsCleanup;

        // 预分配的待处理操作列表
        private readonly List<PendingOp> _pendingOps = new(8);

        private struct PendingOp
        {
            public enum OpType { Add, SetPriority, SetOnce, Remove }
            public OpType Type;
            public uint Id;
            public int Priority;
            public Subscriber<T> Subscriber;
        }

        public HandlerList(int initialCapacity = 16)
        {
            _subscribers = new Subscriber<T>[initialCapacity];
            _count = 0;
            _version = 0;
        }

        public int Count => _count;

        /// <summary>
        /// 添加订阅者
        /// </summary>
        public void Add(uint id, Handlers.EventCallback<T> handler, int priority = 0)
        {
            var subscriber = new Subscriber<T>
            {
                Id = id,
                Priority = priority,
                Once = false,
                Removed = false,
                Version = _version,
                Handler = handler
            };

            if (_dispatchDepth > 0)
            {
                _pendingOps.Add(new PendingOp
                {
                    Type = PendingOp.OpType.Add,
                    Subscriber = subscriber
                });
                return;
            }

            AddInternal(subscriber);
        }

        private void AddInternal(Subscriber<T> subscriber)
        {
            // 扩容
            if (_count >= _subscribers.Length)
            {
                var newArray = new Subscriber<T>[_subscribers.Length * 2];
                Array.Copy(_subscribers, newArray, _count);
                _subscribers = newArray;
            }

            _subscribers[_count++] = subscriber;
            _needsSort = true;
        }

        /// <summary>
        /// 移除订阅者
        /// </summary>
        public void Remove(uint id)
        {
            if (_dispatchDepth > 0)
            {
                // 在分发期间，先标记为已删除（这样当前分发不会再调用它）
                for (int i = 0; i < _count; i++)
                {
                    if (_subscribers[i].Id == id)
                    {
                        _subscribers[i].Removed = true;
                        _needsCleanup = true;
                        return;
                    }
                }
                // 也检查 pending 中是否有待添加的，如果有则标记移除
                for (int i = 0; i < _pendingOps.Count; i++)
                {
                    if (_pendingOps[i].Type == PendingOp.OpType.Add && _pendingOps[i].Subscriber.Id == id)
                    {
                        var op = _pendingOps[i];
                        op.Type = PendingOp.OpType.Remove;
                        _pendingOps[i] = op;
                        return;
                    }
                }
                return;
            }

            RemoveInternal(id);
        }

        private void RemoveInternal(uint id)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_subscribers[i].Id == id)
                {
                    // 直接移除（用最后一个元素覆盖）
                    _subscribers[i] = _subscribers[--_count];
                    _subscribers[_count] = default;
                    _needsSort = true;
                    return;
                }
            }
        }

        /// <summary>
        /// 设置优先级
        /// </summary>
        public void SetPriority(uint id, int priority)
        {
            if (_dispatchDepth > 0)
            {
                _pendingOps.Add(new PendingOp
                {
                    Type = PendingOp.OpType.SetPriority,
                    Id = id,
                    Priority = priority
                });
                return;
            }

            for (int i = 0; i < _count; i++)
            {
                if (_subscribers[i].Id == id)
                {
                    _subscribers[i].Priority = priority;
                    _needsSort = true;
                    return;
                }
            }
        }

        /// <summary>
        /// 设置一次性
        /// </summary>
        public void SetOnce(uint id)
        {
            if (_dispatchDepth > 0)
            {
                _pendingOps.Add(new PendingOp
                {
                    Type = PendingOp.OpType.SetOnce,
                    Id = id
                });
                return;
            }

            for (int i = 0; i < _count; i++)
            {
                if (_subscribers[i].Id == id)
                {
                    _subscribers[i].Once = true;
                    return;
                }
            }
        }

        /// <summary>
        /// 开始分发周期
        /// </summary>
        public void BeginDispatch()
        {
            _dispatchDepth++;
            if (_dispatchDepth == 1)
            {
                _version++;
            }
        }

        /// <summary>
        /// 结束分发周期
        /// </summary>
        public void EndDispatch()
        {
            _dispatchDepth--;
            if (_dispatchDepth == 0)
            {
                ProcessPendingOps();
                CleanupIfNeeded();
            }
        }

        /// <summary>
        /// 分发事件（内部方法，不管理分发状态）
        /// </summary>
        public void DispatchInternal(ref T evt)
        {
            if (_count == 0) return;

            // 排序（按优先级降序）
            if (_needsSort)
            {
                SortByPriority();
                _needsSort = false;
            }

            for (int i = 0; i < _count; i++)
            {
                ref var subscriber = ref _subscribers[i];

                // 跳过已删除的或本次分发期间新增的
                if (subscriber.Removed || subscriber.Version >= _version)
                    continue;

                // 处理一次性订阅：在调用前就标记为已删除，防止嵌套分发时再次调用
                if (subscriber.Once)
                {
                    subscriber.Removed = true;
                    _needsCleanup = true;
                }

                subscriber.Handler(ref evt);
            }
        }

        /// <summary>
        /// 分发事件
        /// </summary>
        public void Dispatch(ref T evt)
        {
            if (_count == 0) return;

            // 排序（按优先级降序）
            if (_needsSort)
            {
                SortByPriority();
                _needsSort = false;
            }

            _dispatchDepth++;
            if (_dispatchDepth == 1)
            {
                _version++;
            }

            try
            {
                for (int i = 0; i < _count; i++)
                {
                    ref var subscriber = ref _subscribers[i];

                    // 跳过已删除的或本次分发期间新增的
                    if (subscriber.Removed || subscriber.Version >= _version)
                        continue;

                    // 处理一次性订阅：在调用前就标记为已删除，防止嵌套分发时再次调用
                    if (subscriber.Once)
                    {
                        subscriber.Removed = true;
                        _needsCleanup = true;
                    }

                    subscriber.Handler(ref evt);
                }
            }
            finally
            {
                _dispatchDepth--;
                if (_dispatchDepth == 0)
                {
                    ProcessPendingOps();
                    CleanupIfNeeded();
                }
            }
        }

        /// <summary>
        /// 插入排序（对于小列表效率高，且稳定）
        /// </summary>
        private void SortByPriority()
        {
            for (int i = 1; i < _count; i++)
            {
                var key = _subscribers[i];
                int j = i - 1;

                // 按优先级降序排列
                while (j >= 0 && _subscribers[j].Priority < key.Priority)
                {
                    _subscribers[j + 1] = _subscribers[j];
                    j--;
                }
                _subscribers[j + 1] = key;
            }
        }

        /// <summary>
        /// 处理待处理操作
        /// </summary>
        private void ProcessPendingOps()
        {
            if (_pendingOps.Count == 0) return;

            foreach (var op in _pendingOps)
            {
                switch (op.Type)
                {
                    case PendingOp.OpType.Add:
                        AddInternal(op.Subscriber);
                        break;
                    case PendingOp.OpType.SetPriority:
                        SetPriority(op.Id, op.Priority);
                        break;
                    case PendingOp.OpType.SetOnce:
                        SetOnce(op.Id);
                        break;
                    case PendingOp.OpType.Remove:
                        // 已标记为 Remove 的 Add 操作，跳过（不添加）
                        break;
                }
            }

            _pendingOps.Clear();
        }

        /// <summary>
        /// 清理已删除的订阅者
        /// </summary>
        private void CleanupIfNeeded()
        {
            if (!_needsCleanup) return;
            _needsCleanup = false;

            int writeIndex = 0;
            for (int readIndex = 0; readIndex < _count; readIndex++)
            {
                if (!_subscribers[readIndex].Removed)
                {
                    if (writeIndex != readIndex)
                        _subscribers[writeIndex] = _subscribers[readIndex];
                    writeIndex++;
                }
            }

            // 清理尾部
            for (int i = writeIndex; i < _count; i++)
                _subscribers[i] = default;

            _count = writeIndex;
        }

        /// <summary>
        /// 清空所有订阅者
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _count; i++)
                _subscribers[i] = default;
            _count = 0;
            _pendingOps.Clear();
        }
    }
}
