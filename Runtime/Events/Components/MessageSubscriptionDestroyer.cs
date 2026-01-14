using System.Collections.Generic;
using Azathrix.Framework.Events.Core;
using UnityEngine;

namespace Azathrix.Framework.Events.Components
{
    /// <summary>
    /// 消息订阅自动取消组件
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MessageSubscriptionDestroyer : MonoBehaviour
    {
        private readonly List<MessageSubscriptionResult> _subscriptions = new();

        public void Add(MessageSubscriptionResult subscription)
        {
            _subscriptions.Add(subscription);
        }

        public void Remove(MessageSubscriptionResult subscription)
        {
            _subscriptions.Remove(subscription);
        }

        public int Count => _subscriptions.Count;

        private void OnDestroy()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Unsubscribe();
            }
            _subscriptions.Clear();
        }
    }
}
