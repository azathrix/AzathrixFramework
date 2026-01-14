using System;
using System.Buffers;
using System.Collections.Concurrent;
using Azathrix.Framework.Events.Core;
using Azathrix.Framework.Events.Serialization;

namespace Azathrix.Framework.Events.Internal
{
    /// <summary>
    /// 消息处理器委托
    /// </summary>
    public delegate void MessageHandler<T>(T data);

    /// <summary>
    /// 消息订阅者信息
    /// </summary>
    internal struct MessageSubscriber<T>
    {
        public uint Id;
        public int Priority;
        public bool Removed;
        public MessageHandler<T> Handler;
    }

    /// <summary>
    /// 消息处理器列表
    /// </summary>
    internal sealed class MessageHandlerList<T>
    {
        private MessageSubscriber<T>[] _subscribers;
        private int _count;
        private bool _needsSort;
        private readonly object _lock = new();

        // 复用快照数组，避免每次分发都分配
        [ThreadStatic]
        private static MessageSubscriber<T>[] _snapshotCache;

        public MessageHandlerList(int initialCapacity = 8)
        {
            _subscribers = new MessageSubscriber<T>[initialCapacity];
            _count = 0;
        }

        public int Count
        {
            get
            {
                lock (_lock) return _count;
            }
        }

        public void Add(uint id, MessageHandler<T> handler, int priority = 0)
        {
            lock (_lock)
            {
                if (_count >= _subscribers.Length)
                {
                    var newArray = new MessageSubscriber<T>[_subscribers.Length * 2];
                    Array.Copy(_subscribers, newArray, _count);
                    _subscribers = newArray;
                }

                _subscribers[_count++] = new MessageSubscriber<T>
                {
                    Id = id,
                    Priority = priority,
                    Removed = false,
                    Handler = handler
                };
                _needsSort = true;
            }
        }

        public void Remove(uint id)
        {
            lock (_lock)
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
        }

        public void Dispatch(T data)
        {
            MessageSubscriber<T>[] snapshot;
            int count;

            lock (_lock)
            {
                if (_count == 0) return;

                if (_needsSort)
                {
                    SortByPriority();
                    _needsSort = false;
                }

                // 复用快照数组
                if (_snapshotCache == null || _snapshotCache.Length < _count)
                {
                    _snapshotCache = new MessageSubscriber<T>[Math.Max(_count, 16)];
                }
                snapshot = _snapshotCache;
                Array.Copy(_subscribers, snapshot, _count);
                count = _count;
            }

            // 在锁外分发
            for (int i = 0; i < count; i++)
            {
                ref var subscriber = ref snapshot[i];
                if (subscriber.Removed) continue;
                subscriber.Handler(data);
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
            lock (_lock)
            {
                for (int i = 0; i < _count; i++)
                    _subscribers[i] = default;
                _count = 0;
            }
        }
    }

    /// <summary>
    /// 消息通道（线程安全）
    /// </summary>
    internal sealed class MessageChannel
    {
        private readonly EventDispatcher _dispatcher;
        private readonly ConcurrentDictionary<string, object> _handlers = new();
        private readonly IMessageSerializer _serializer;

        public MessageChannel(EventDispatcher dispatcher, IMessageSerializer serializer)
        {
            _dispatcher = dispatcher;
            _serializer = serializer ?? new JsonMessageSerializer();
        }

        public uint Subscribe<T>(string messageId, MessageHandler<T> handler, int priority = 0)
        {
            var list = _handlers.GetOrAdd(messageId, _ => new MessageHandlerList<T>()) as MessageHandlerList<T>;
            if (list == null)
            {
                throw new InvalidOperationException($"Message '{messageId}' is already registered with a different type.");
            }

            var id = _dispatcher.GenerateId();
            list.Add(id, handler, priority);
            return id;
        }

        public void Unsubscribe<T>(string messageId, uint id)
        {
            if (_handlers.TryGetValue(messageId, out var obj) && obj is MessageHandlerList<T> list)
            {
                list.Remove(id);
            }
        }

        public void Dispatch<T>(string messageId, T data)
        {
            if (_handlers.TryGetValue(messageId, out var obj) && obj is MessageHandlerList<T> list)
            {
                list.Dispatch(data);
            }
        }

        /// <summary>
        /// 使用序列化分发（跨线程安全）
        /// </summary>
        public void DispatchSerialized<T>(string messageId, T data)
        {
            // 先尝试小 buffer，不够再扩容
            int bufferSize = 256;
            byte[] buffer = null;
            int length;

            while (true)
            {
                buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    length = _serializer.Serialize(data, buffer);
                    break; // 成功
                }
                catch (ArgumentException)
                {
                    // buffer 不够，扩容重试
                    ArrayPool<byte>.Shared.Return(buffer);
                    bufferSize *= 2;
                    if (bufferSize > 1024 * 1024) // 最大 1MB
                        throw new InvalidOperationException("Serialized data too large (>1MB)");
                }
            }

            try
            {
                // 分发时反序列化
                if (_handlers.TryGetValue(messageId, out var obj) && obj is MessageHandlerList<T> list)
                {
                    var deserialized = _serializer.Deserialize<T>(new ReadOnlySpan<byte>(buffer, 0, length));
                    list.Dispatch(deserialized);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public void Clear(string messageId)
        {
            if (_handlers.TryRemove(messageId, out _))
            {
                // 已移除
            }
        }

        public void ClearAll()
        {
            _handlers.Clear();
        }
    }
}
