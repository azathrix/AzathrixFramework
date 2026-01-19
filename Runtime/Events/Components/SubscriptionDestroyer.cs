using System.Collections.Generic;
using Azathrix.Framework.Events.Results;
using UnityEngine;

namespace Azathrix.Framework.Events.Components
{
    /// <summary>
    /// 自动取消订阅组件，绑定到GameObject生命周期
    /// </summary>
    /// <remarks>
    /// 当GameObject销毁时，自动取消所有绑定的订阅。
    /// 通常通过 .AddTo(gameObject) 自动添加，无需手动创建。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 方式1：通过AddTo自动绑定
    /// dispatcher.Subscribe&lt;MyEvent&gt;(handler).AddTo(gameObject);
    ///
    /// // 方式2：手动添加
    /// var destroyer = gameObject.AddComponent&lt;SubscriptionDestroyer&gt;();
    /// destroyer.Add(subscription);
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    public sealed class SubscriptionDestroyer : MonoBehaviour
    {
        private readonly List<SubscriptionResult> _subscriptions = new();

        /// <summary>
        /// 添加订阅
        /// </summary>
        public void Add(SubscriptionResult subscription)
        {
            _subscriptions.Add(subscription);
        }

        /// <summary>
        /// 移除订阅（不取消）
        /// </summary>
        public void Remove(SubscriptionResult subscription)
        {
            _subscriptions.Remove(subscription);
        }

        /// <summary>
        /// 订阅数量
        /// </summary>
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
