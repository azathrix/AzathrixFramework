using Azathrix.Framework.Events.Handlers;
using Azathrix.Framework.Events.Internal;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// EventDispatcher - Post事件相关方法
    /// </summary>
    public partial class EventDispatcher
    {
        private readonly PostQueue _postQueue = new();

        /// <summary>
        /// Post事件（延迟到帧结束处理，线程安全）
        /// </summary>
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
