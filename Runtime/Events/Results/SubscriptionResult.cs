using System;
using Azathrix.Framework.Events.Core;

namespace Azathrix.Framework.Events.Results
{
    /// <summary>
    /// 订阅结果，支持链式配置和取消订阅
    /// </summary>
    public struct SubscriptionResult : IDisposable
    {
        private readonly EventDispatcher _dispatcher;
        private readonly uint _id;
        private readonly Type _eventType;

        internal SubscriptionResult(EventDispatcher dispatcher, uint id, Type eventType)
        {
            _dispatcher = dispatcher;
            _id = id;
            _eventType = eventType;
        }

        /// <summary>
        /// 订阅ID
        /// </summary>
        public uint Id => _id;

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => _dispatcher != null && _id != 0;

        /// <summary>
        /// 设置优先级（数值越大越先执行）
        /// </summary>
        public SubscriptionResult Priority(int priority)
        {
            _dispatcher?.SetPriority(_id, _eventType, priority);
            return this;
        }

        /// <summary>
        /// 设置为一次性订阅（触发一次后自动取消）
        /// </summary>
        public SubscriptionResult Once()
        {
            _dispatcher?.SetOnce(_id, _eventType);
            return this;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Unsubscribe()
        {
            _dispatcher?.Unsubscribe(_id, _eventType);
        }

        /// <summary>
        /// 取消订阅（IDisposable实现）
        /// </summary>
        public void Dispose()
        {
            Unsubscribe();
        }
    }
}
