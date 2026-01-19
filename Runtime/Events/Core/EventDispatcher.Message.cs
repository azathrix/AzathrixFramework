using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Azathrix.Framework.Events.Internal;
using Azathrix.Framework.Events.Serialization;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// 消息订阅结果
    /// </summary>
    public struct MessageSubscriptionResult : IDisposable
    {
        private readonly EventDispatcher _dispatcher;
        private readonly string _messageId;
        private readonly uint _id;
        private readonly Type _dataType;

        internal MessageSubscriptionResult(EventDispatcher dispatcher, string messageId, uint id, Type dataType)
        {
            _dispatcher = dispatcher;
            _messageId = messageId;
            _id = id;
            _dataType = dataType;
        }

        /// <summary>订阅ID</summary>
        public uint Id => _id;
        /// <summary>消息ID</summary>
        public string MessageId => _messageId;
        /// <summary>是否有效</summary>
        public bool IsValid => _dispatcher != null && _id != 0;

        /// <summary>取消订阅</summary>
        public void Unsubscribe()
        {
            _dispatcher?.UnsubscribeMessage(_messageId, _id, _dataType);
        }

        public void Dispose()
        {
            Unsubscribe();
        }
    }

    /// <summary>
    /// 消息队列接口
    /// </summary>
    internal interface IMessagePostQueue
    {
        int Count { get; }
        void Enqueue(string messageId, object data, Type dataType);
        void Flush(EventDispatcher dispatcher, MessageChannel channel);
        void Clear();
    }

    /// <summary>
    /// 线程安全的消息队列
    /// </summary>
    internal sealed class ThreadSafeMessageQueue : IMessagePostQueue
    {
        private readonly ConcurrentQueue<(string messageId, object data, Type dataType)> _queue = new();

        public int Count => _queue.Count;

        public void Enqueue(string messageId, object data, Type dataType)
        {
            _queue.Enqueue((messageId, data, dataType));
        }

        public void Flush(EventDispatcher dispatcher, MessageChannel channel)
        {
            while (_queue.TryDequeue(out var item))
            {
                var method = typeof(MessageChannel).GetMethod(nameof(MessageChannel.Dispatch));
                var generic = method?.MakeGenericMethod(item.dataType);
                generic?.Invoke(channel, new object[] { item.messageId, item.data });
            }
        }

        public void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
        }
    }

    /// <summary>
    /// 非线程安全的消息队列（零GC）
    /// </summary>
    internal sealed class FastMessageQueue : IMessagePostQueue
    {
        private readonly List<(string messageId, object data, Type dataType)> _queue = new(64);

        public int Count => _queue.Count;

        public void Enqueue(string messageId, object data, Type dataType)
        {
            _queue.Add((messageId, data, dataType));
        }

        public void Flush(EventDispatcher dispatcher, MessageChannel channel)
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                var item = _queue[i];
                var method = typeof(MessageChannel).GetMethod(nameof(MessageChannel.Dispatch));
                var generic = method?.MakeGenericMethod(item.dataType);
                generic?.Invoke(channel, new object[] { item.messageId, item.data });
            }
            _queue.Clear();
        }

        public void Clear()
        {
            _queue.Clear();
        }
    }

    /// <summary>
    /// EventDispatcher - 消息事件相关方法
    /// </summary>
    /// <remarks>
    /// 消息事件基于字符串ID，适用于：
    /// - 跨模块通信（不需要共享事件类型）
    /// - 动态消息（运行时确定消息类型）
    /// - 网络消息（与服务器通信）
    /// </remarks>
    public partial class EventDispatcher
    {
        private MessageChannel _messageChannel;
        private IMessagePostQueue _messagePostQueue;
        private readonly object _msgQueueLock = new();

        /// <summary>
        /// PostMessage是否使用线程安全模式（默认true）
        /// </summary>
        /// <remarks>
        /// 线程安全模式使用ConcurrentQueue，有少量GC开销。
        /// 非线程安全模式使用List，零GC但只能在主线程调用PostMessage。
        /// 必须在第一次PostMessage之前设置。
        /// </remarks>
        public bool MsgThreadSafe { get; set; } = true;

        private IMessagePostQueue GetMessagePostQueue()
        {
            if (_messagePostQueue != null) return _messagePostQueue;
            lock (_msgQueueLock)
            {
                return _messagePostQueue ??= MsgThreadSafe
                    ? new ThreadSafeMessageQueue()
                    : new FastMessageQueue();
            }
        }

        /// <summary>
        /// 设置消息序列化器
        /// </summary>
        public void SetMessageSerializer(IMessageSerializer serializer)
        {
            _messageChannel = new MessageChannel(this, serializer);
        }

        private MessageChannel GetMessageChannel()
        {
            return _messageChannel ??= new MessageChannel(this, null);
        }

        /// <summary>
        /// 订阅消息事件
        /// </summary>
        /// <typeparam name="T">消息数据类型</typeparam>
        /// <param name="messageId">消息ID</param>
        /// <param name="handler">消息处理器</param>
        /// <param name="priority">优先级</param>
        /// <returns>消息订阅结果</returns>
        /// <example>
        /// <code>
        /// dispatcher.SubscribeMessage&lt;PlayerData&gt;("player.login", data => {
        ///     Debug.Log($"玩家 {data.Name} 登录");
        /// });
        /// </code>
        /// </example>
        public MessageSubscriptionResult SubscribeMessage<T>(string messageId, MessageHandler<T> handler, int priority = 0)
        {
            var channel = GetMessageChannel();
            var id = channel.Subscribe(messageId, handler, priority);
            return new MessageSubscriptionResult(this, messageId, id, typeof(T));
        }

        /// <summary>
        /// 取消消息订阅
        /// </summary>
        internal void UnsubscribeMessage(string messageId, uint id, Type dataType)
        {
            var method = typeof(MessageChannel).GetMethod(nameof(MessageChannel.Unsubscribe));
            var generic = method?.MakeGenericMethod(dataType);
            generic?.Invoke(GetMessageChannel(), new object[] { messageId, id });
        }

        /// <summary>
        /// 分发消息事件
        /// </summary>
        /// <typeparam name="T">消息数据类型</typeparam>
        /// <param name="messageId">消息ID</param>
        /// <param name="data">消息数据</param>
        /// <example>
        /// <code>
        /// dispatcher.DispatchMessage("player.login", new PlayerData { Name = "Player1" });
        /// </code>
        /// </example>
        public void DispatchMessage<T>(string messageId, T data)
        {
            GetMessageChannel().Dispatch(messageId, data);
        }

        /// <summary>
        /// 分发消息事件（使用序列化，线程安全）
        /// </summary>
        /// <typeparam name="T">消息数据类型</typeparam>
        /// <param name="messageId">消息ID</param>
        /// <param name="data">消息数据</param>
        /// <remarks>
        /// 数据会被序列化后传输，适用于跨线程场景。
        /// 需要先调用 SetMessageSerializer 设置序列化器。
        /// </remarks>
        public void DispatchMessageSerialized<T>(string messageId, T data)
        {
            GetMessageChannel().DispatchSerialized(messageId, data);
        }

        /// <summary>
        /// Post消息事件（延迟到帧结束处理）
        /// </summary>
        /// <typeparam name="T">消息数据类型</typeparam>
        /// <param name="messageId">消息ID</param>
        /// <param name="data">消息数据</param>
        /// <remarks>
        /// 消息会被加入队列，在帧结束时统一分发。
        /// 线程安全性由MsgThreadSafe属性控制（默认true）。
        /// </remarks>
        public void PostMessage<T>(string messageId, T data)
        {
            GetMessagePostQueue().Enqueue(messageId, data, typeof(T));
        }

        /// <summary>
        /// Flush所有待处理的消息事件
        /// </summary>
        public void FlushMessages()
        {
            _messagePostQueue?.Flush(this, GetMessageChannel());
        }

        /// <summary>
        /// 获取待处理的消息数量
        /// </summary>
        public int PendingMessageCount => _messagePostQueue?.Count ?? 0;

        /// <summary>
        /// 清空指定消息的所有订阅
        /// </summary>
        public void ClearMessageSubscriptions(string messageId)
        {
            GetMessageChannel().Clear(messageId);
        }

        /// <summary>
        /// 清空所有消息订阅
        /// </summary>
        public void ClearAllMessageSubscriptions()
        {
            GetMessageChannel().ClearAll();
        }

        /// <summary>
        /// 清空所有待处理的消息
        /// </summary>
        public void ClearPendingMessages()
        {
            _messagePostQueue?.Clear();
        }
    }
}
