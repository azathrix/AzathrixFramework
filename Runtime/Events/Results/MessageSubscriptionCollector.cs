using System;
using System.Collections.Generic;
using Azathrix.Framework.Events.Core;

namespace Azathrix.Framework.Events.Results
{
    /// <summary>
    /// 消息订阅收集器
    /// </summary>
    public sealed class MessageSubscriptionCollector : IDisposable
    {
        private readonly List<MessageSubscriptionResult> _subscriptions = new();
        private bool _disposed;

        public void Add(MessageSubscriptionResult subscription)
        {
            if (_disposed) return;
            _subscriptions.Add(subscription);
        }

        public int Count => _subscriptions.Count;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var subscription in _subscriptions)
            {
                subscription.Unsubscribe();
            }
            _subscriptions.Clear();
        }

        public void Clear()
        {
            _subscriptions.Clear();
        }
    }
}
