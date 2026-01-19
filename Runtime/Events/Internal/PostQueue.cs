using System.Collections.Concurrent;
using System.Collections.Generic;
using Azathrix.Framework.Events.Core;

namespace Azathrix.Framework.Events.Internal
{
    /// <summary>
    /// Post事件项基类
    /// </summary>
    internal abstract class PostItem
    {
        public abstract void Dispatch(EventDispatcher dispatcher);
    }

    /// <summary>
    /// 泛型Post事件项（线程安全池）
    /// </summary>
    internal sealed class PostItem<T> : PostItem where T : struct
    {
        private static readonly ConcurrentBag<PostItem<T>> _pool = new();

        public T Event;
        public object Sender;

        public override void Dispatch(EventDispatcher dispatcher)
        {
            dispatcher.Dispatch(ref Event, Sender);
            Return(this);
        }

        public static PostItem<T> Rent(T evt, object sender)
        {
            if (!_pool.TryTake(out var item))
            {
                item = new PostItem<T>();
            }
            item.Event = evt;
            item.Sender = sender;
            return item;
        }

        public static void Return(PostItem<T> item)
        {
            item.Event = default;
            item.Sender = null;
            _pool.Add(item);
        }
    }

    /// <summary>
    /// 快速Post事件项（非线程安全池，零GC）
    /// </summary>
    internal sealed class FastPostItem<T> : PostItem where T : struct
    {
        private static readonly Stack<FastPostItem<T>> _pool = new(256);

        public T Event;
        public object Sender;

        public override void Dispatch(EventDispatcher dispatcher)
        {
            dispatcher.Dispatch(ref Event, Sender);
            Return(this);
        }

        public static FastPostItem<T> Rent(T evt, object sender)
        {
            var item = _pool.Count > 0 ? _pool.Pop() : new FastPostItem<T>();
            item.Event = evt;
            item.Sender = sender;
            return item;
        }

        public static void Return(FastPostItem<T> item)
        {
            item.Event = default;
            item.Sender = null;
            _pool.Push(item);
        }
    }

    /// <summary>
    /// Post事件队列接口
    /// </summary>
    internal interface IPostQueue
    {
        int Count { get; }
        void Enqueue<T>(T evt, object sender = null) where T : struct;
        void Flush(EventDispatcher dispatcher);
        void Clear();
    }

    /// <summary>
    /// 线程安全的Post事件队列
    /// </summary>
    internal sealed class ThreadSafePostQueue : IPostQueue
    {
        private readonly ConcurrentQueue<PostItem> _queue = new();

        public int Count => _queue.Count;

        public void Enqueue<T>(T evt, object sender = null) where T : struct
        {
            _queue.Enqueue(PostItem<T>.Rent(evt, sender));
        }

        public void Flush(EventDispatcher dispatcher)
        {
            while (_queue.TryDequeue(out var item))
            {
                item.Dispatch(dispatcher);
            }
        }

        public void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
        }
    }

    /// <summary>
    /// 非线程安全的Post事件队列（零GC）
    /// </summary>
    internal sealed class FastPostQueue : IPostQueue
    {
        private readonly List<PostItem> _queue = new(256);

        public int Count => _queue.Count;

        public void Enqueue<T>(T evt, object sender = null) where T : struct
        {
            _queue.Add(FastPostItem<T>.Rent(evt, sender));
        }

        public void Flush(EventDispatcher dispatcher)
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                _queue[i].Dispatch(dispatcher);
            }
            _queue.Clear();
        }

        public void Clear()
        {
            _queue.Clear();
        }
    }

    /// <summary>
    /// Post队列工厂（根据配置创建）
    /// </summary>
    internal static class PostQueueFactory
    {
        public static IPostQueue Create(bool threadSafe)
        {
            return threadSafe ? new ThreadSafePostQueue() : new FastPostQueue();
        }
    }
}
