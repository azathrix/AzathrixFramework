using Azathrix.Framework.Events.Components;
using Azathrix.Framework.Events.Results;
using UnityEngine;

namespace Azathrix.Framework.Events.Extensions
{
    /// <summary>
    /// SubscriptionResult 扩展方法
    /// </summary>
    /// <remarks>
    /// 提供便捷的生命周期绑定方法
    /// </remarks>
    public static class SubscriptionExtensions
    {
        /// <summary>
        /// 绑定到GameObject生命周期，GameObject销毁时自动取消订阅
        /// </summary>
        /// <param name="result">订阅结果</param>
        /// <param name="go">要绑定的GameObject</param>
        /// <returns>订阅结果（支持链式调用）</returns>
        /// <example>
        /// <code>
        /// dispatcher.SubscribeAsync&lt;MyEvent&gt;(handler).AddTo(gameObject);
        /// </code>
        /// </example>
        public static SubscriptionResult AddTo(this SubscriptionResult result, GameObject go)
        {
            if (go == null) return result;

            var destroyer = go.GetComponent<SubscriptionDestroyer>();
            if (destroyer == null)
                destroyer = go.AddComponent<SubscriptionDestroyer>();

            destroyer.Add(result);
            return result;
        }

        /// <summary>
        /// 绑定到Component的GameObject生命周期
        /// </summary>
        public static SubscriptionResult AddTo(this SubscriptionResult result, Component component)
        {
            if (component == null) return result;
            return result.AddTo(component.gameObject);
        }

        /// <summary>
        /// 添加到订阅收集器
        /// </summary>
        public static SubscriptionResult AddTo(this SubscriptionResult result, SubscriptionCollector collector)
        {
            collector?.Add(result);
            return result;
        }
    }
}
