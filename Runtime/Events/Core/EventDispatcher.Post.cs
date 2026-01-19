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
        private readonly PostQueue _postQueue = new();

        /// <summary>
        /// Post事件（延迟到帧结束处理，线程安全）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件数据</param>
        /// <param name="sender">发送者（可选）</param>
        /// <remarks>
        /// 事件会被加入队列，在PostLateUpdate时机统一分发。
        /// 适用于：
        /// - 从子线程发送事件
        /// - 避免在Update中频繁分发
        /// - 需要批量处理的场景
        /// </remarks>
        /// <example>
        /// <code>
        /// // 从任意线程安全地发送事件
        /// Task.Run(() => {
        ///     dispatcher.Post(new DataLoadedEvent { Data = loadedData });
        /// });
        /// </code>
        /// </example>
        public void Post<T>(T evt, object sender = null) where T : struct
        {
            _postQueue.Enqueue(evt, sender);
        }

        /// <summary>
        /// Post默认事件
        /// </summary>
        public void Post<T>(object sender = null) where T : struct
        {
            _postQueue.Enqueue(new T(), sender);
        }

        /// <summary>
        /// Post事件（使用初始化器）
        /// </summary>
        public void Post<T>(EventInitializer<T> initializer, object sender = null) where T : struct
        {
            var evt = new T();
            initializer(ref evt);
            _postQueue.Enqueue(evt, sender);
        }

        /// <summary>
        /// 手动Flush所有Post事件
        /// </summary>
        public void Flush()
        {
            _postQueue.Flush(this);
        }

        /// <summary>
        /// 获取待处理的Post事件数量
        /// </summary>
        public int PendingPostCount => _postQueue.Count;

        /// <summary>
        /// 清空所有待处理的Post事件
        /// </summary>
        public void ClearPendingPosts()
        {
            _postQueue.Clear();
        }
    }
}
