using Azathrix.Framework.Events.Components;
using Azathrix.Framework.Events.Results;
using UnityEngine;

namespace Azathrix.Framework.Events.Extensions
{
    /// <summary>
    /// SubscriptionResult 扩展方法
    /// </summary>
    public static class SubscriptionExtensions
    {
        /// <summary>
        /// 绑定到GameObject生命周期，GameObject销毁时自动取消订阅
        /// </summary>
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
