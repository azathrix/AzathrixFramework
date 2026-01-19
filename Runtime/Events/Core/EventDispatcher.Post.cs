using System.Threading;
using Azathrix.Framework.Events.Handlers;
using Azathrix.Framework.Events.Internal;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// EventDispatcher - Post事件相关方法
    /// </summary>
    /// <remarks>
    /// Post事件会延迟到帧结束（PostLateUpdate）时统一处理，
    /// 适用于需要线程安全或避免在Update中频繁分发的场景
    /// </remarks>
    public partial class EventDispatcher
    {
        private IPostQueue _postQueue;
        private readonly object _postQueueLock = new();

        /// <summary>
        /// Post事件是否使用线程安全模式（默认false）
        /// </summary>
        /// <remarks>
        /// 线程安全模式使用ConcurrentQueue，有少量GC开销。
        /// 非线程安全模式使用List，零GC但只能在主线程调用Post。
        /// 必须在第一次Post之前设置。
        /// </remarks>
        public bool PostThreadSafe { get; set; } = false;

        private IPostQueue GetPostQueue()
        {
            if (_postQueue != null) return _postQueue;
            lock (_postQueueLock)
            {
                return _postQueue ??= PostQueueFactory.Create(PostThreadSafe);
            }
        }

        /// <summary>
        /// Post事件（延迟到帧结束处理）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件数据</param>
        /// <param name="sender">发送者（可选）</param>
        /// <remarks>
        /// 事件会被加入队列，在PostLateUpdate时机统一分发。
        /// 适用于：
        /// - 从子线程发送事件（需PostThreadSafe=true）
        /// - 避免在Update中频繁分发
        /// - 需要批量处理的场景
        /// </remarks>
        /// <example>
        /// <code>
        /// // 高性能模式（默认，仅主线程）
        /// dispatcher.Post(new MyEvent());  // 零GC
        ///
        /// // 线程安全模式
        /// dispatcher.PostThreadSafe = true;
        /// Task.Run(() => dispatcher.Post(new DataLoadedEvent { Data = data }));
        /// </code>
        /// </example>
        public void Post<T>(T evt, object sender = null) where T : struct
        {
            GetPostQueue().Enqueue(evt, sender);
        }

        /// <summary>
        /// Post默认事件
        /// </summary>
        public void Post<T>(object sender = null) where T : struct
        {
            GetPostQueue().Enqueue(new T(), sender);
        }

        /// <summary>
        /// Post事件（使用初始化器）
        /// </summary>
        public void Post<T>(EventInitializer<T> initializer, object sender = null) where T : struct
        {
            var evt = new T();
            initializer(ref evt);
            GetPostQueue().Enqueue(evt, sender);
        }

        /// <summary>
        /// 手动Flush所有Post事件
        /// </summary>
        public void Flush()
        {
            _postQueue?.Flush(this);
        }

        /// <summary>
        /// 获取待处理的Post事件数量
        /// </summary>
        public int PendingPostCount => _postQueue?.Count ?? 0;

        /// <summary>
        /// 清空所有待处理的Post事件
        /// </summary>
        public void ClearPendingPosts()
        {
            _postQueue?.Clear();
        }
    }
}
