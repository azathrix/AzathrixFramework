using Azathrix.Framework.Events.Components;
using Azathrix.Framework.Events.Core;
using Azathrix.Framework.Events.Results;
using UnityEngine;

namespace Azathrix.Framework.Events.Extensions
{
    /// <summary>
    /// MessageSubscriptionResult 扩展方法
    /// </summary>
    public static class MessageSubscriptionExtensions
    {
        /// <summary>
        /// 绑定到GameObject生命周期
        /// </summary>
        public static MessageSubscriptionResult AddTo(this MessageSubscriptionResult result, GameObject go)
        {
            if (go == null) return result;

            var destroyer = go.GetComponent<MessageSubscriptionDestroyer>();
            if (destroyer == null)
                destroyer = go.AddComponent<MessageSubscriptionDestroyer>();

            destroyer.Add(result);
            return result;
        }

        /// <summary>
        /// 绑定到Component的GameObject生命周期
        /// </summary>
        public static MessageSubscriptionResult AddTo(this MessageSubscriptionResult result, Component component)
        {
            if (component == null) return result;
            return result.AddTo(component.gameObject);
        }

        /// <summary>
        /// 添加到消息订阅收集器
        /// </summary>
        public static MessageSubscriptionResult AddTo(this MessageSubscriptionResult result, MessageSubscriptionCollector collector)
        {
            collector?.Add(result);
            return result;
        }
    }
}
