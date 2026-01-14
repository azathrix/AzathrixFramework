using System.Collections.Concurrent;
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
    /// 泛型Post事件项
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
    /// 线程安全的Post事件队列
    /// </summary>
    internal sealed class PostQueue
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
}
