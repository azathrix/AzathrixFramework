using System;
using System.Collections.Generic;

namespace Azathrix.Framework.Events.Results
{
    /// <summary>
    /// 订阅收集器，用于批量管理订阅
    /// </summary>
    public sealed class SubscriptionCollector : IDisposable
    {
        private readonly List<SubscriptionResult> _subscriptions = new();
        private bool _disposed;

        /// <summary>
        /// 添加订阅
        /// </summary>
        public void Add(SubscriptionResult subscription)
        {
            if (_disposed) return;
            _subscriptions.Add(subscription);
        }

        /// <summary>
        /// 订阅数量
        /// </summary>
        public int Count => _subscriptions.Count;

        /// <summary>
        /// 取消所有订阅
        /// </summary>
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

        /// <summary>
        /// 清空（不取消订阅）
        /// </summary>
        public void Clear()
        {
            _subscriptions.Clear();
        }
    }
}
