using System;
using System.Collections.Concurrent;
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

        public uint Id => _id;
        public string MessageId => _messageId;
        public bool IsValid => _dispatcher != null && _id != 0;

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
    /// EventDispatcher - 消息事件相关方法
    /// </summary>
    public partial class EventDispatcher
    {
        private MessageChannel _messageChannel;
        private readonly ConcurrentQueue<(string messageId, object data, Type dataType)> _pendingMessages = new();

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
        public void DispatchMessage<T>(string messageId, T data)
        {
            GetMessageChannel().Dispatch(messageId, data);
        }

        /// <summary>
        /// 分发消息事件（使用序列化，线程安全）
        /// </summary>
        public void DispatchMessageSerialized<T>(string messageId, T data)
        {
            GetMessageChannel().DispatchSerialized(messageId, data);
        }

        /// <summary>
        /// Post消息事件（延迟到帧结束处理，线程安全）
        /// </summary>
        public void PostMessage<T>(string messageId, T data)
        {
            _pendingMessages.Enqueue((messageId, data, typeof(T)));
        }

        /// <summary>
        /// Flush所有待处理的消息事件
        /// </summary>
        public void FlushMessages()
        {
            while (_pendingMessages.TryDequeue(out var item))
            {
                var method = typeof(MessageChannel).GetMethod(nameof(MessageChannel.Dispatch));
                var generic = method?.MakeGenericMethod(item.dataType);
                generic?.Invoke(GetMessageChannel(), new object[] { item.messageId, item.data });
            }
        }

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
            while (_pendingMessages.TryDequeue(out _)) { }
        }
    }
}
