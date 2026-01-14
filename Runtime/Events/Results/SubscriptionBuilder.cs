using System;
using Azathrix.Framework.Events.Components;
using Azathrix.Framework.Events.Core;
using Azathrix.Framework.Events.Handlers;
using Azathrix.Framework.Events.Internal;
using UnityEngine;

namespace Azathrix.Framework.Events.Results
{
    /// <summary>
    /// 订阅构建器，支持链式配置
    /// 订阅在创建时立即生效，链式方法修改行为
    /// </summary>
    public sealed class SubscriptionBuilder<T> : IDisposable where T : struct
    {
        private readonly EventDispatcher _dispatcher;
        private readonly EventCallback<T> _originalHandler;
        private readonly Type _eventType;
        private readonly uint _subscriptionId;

        private bool _disposed;

        // 配置选项（运行时可修改）
        internal EventFilter<T> Filter;
        internal int ThrottleMs;
        internal int DebounceMs;
        internal int SkipCount;
        internal int DelayMs;
        internal bool OnceFlag;
        internal float LastTriggerTime = float.MinValue;
        internal T PendingEvent;
        internal float PendingTime;
        internal bool HasPending;
        private int _skipCounter;

        internal SubscriptionBuilder(EventDispatcher dispatcher, EventCallback<T> handler)
        {
            _dispatcher = dispatcher;
            _originalHandler = handler;
            _eventType = typeof(T);

            // 立即创建订阅
            var channel = dispatcher.GetOrCreateChannel<T>();
            _subscriptionId = channel.Subscribe(WrappedHandler);
        }

        private void WrappedHandler(ref T evt)
        {
            // 跳过前N个事件
            if (SkipCount > 0 && _skipCounter < SkipCount)
            {
                _skipCounter++;
                return;
            }

            // 过滤
            if (Filter != null && !Filter(ref evt))
                return;

            // 节流
            if (ThrottleMs > 0)
            {
                float now = Time.unscaledTime;
                if (now - LastTriggerTime < ThrottleMs / 1000f)
                    return;
                LastTriggerTime = now;
            }

            // 防抖：只记录，不立即执行
            if (DebounceMs > 0)
            {
                PendingEvent = evt;
                PendingTime = Time.unscaledTime;
                HasPending = true;
                return;
            }

            // 延迟执行
            if (DelayMs > 0)
            {
                // Once: 在调度延迟前取消，防止后续事件再次调度
                if (OnceFlag)
                {
                    Unsubscribe();
                }

                var captured = evt;
                DelayRunner.Schedule(DelayMs, () => _originalHandler(ref captured));
                return;
            }

            // Once: 在调用前取消，防止嵌套分发再次触发
            if (OnceFlag)
            {
                Unsubscribe();
            }

            _originalHandler(ref evt);
        }

        /// <summary>
        /// 设置过滤条件
        /// </summary>
        public SubscriptionBuilder<T> Where(EventFilter<T> filter)
        {
            Filter = filter;
            return this;
        }

        /// <summary>
        /// 设置优先级
        /// </summary>
        public SubscriptionBuilder<T> Priority(int priority)
        {
            _dispatcher.SetPriority(_subscriptionId, _eventType, priority);
            return this;
        }

        /// <summary>
        /// 设置为一次性订阅（实际处理后才取消）
        /// </summary>
        public SubscriptionBuilder<T> Once()
        {
            OnceFlag = true;
            return this;
        }

        /// <summary>
        /// 设置为 Sticky（立即收到最后一个值）
        /// </summary>
        public SubscriptionBuilder<T> Sticky()
        {
            var channel = _dispatcher.GetOrCreateChannel<T>();
            if (channel.HasSticky)
            {
                _originalHandler(ref channel.GetStickyValue());
            }
            return this;
        }

        /// <summary>
        /// 设置节流时间（毫秒）
        /// </summary>
        public SubscriptionBuilder<T> Throttle(int ms)
        {
            ThrottleMs = ms;
            return this;
        }

        /// <summary>
        /// 跳过前N个事件
        /// </summary>
        public SubscriptionBuilder<T> Skip(int count)
        {
            SkipCount = count;
            return this;
        }

        /// <summary>
        /// 延迟处理事件（毫秒）
        /// </summary>
        public SubscriptionBuilder<T> Delay(int ms)
        {
            DelayMs = ms;
            return this;
        }

        /// <summary>
        /// 设置防抖时间（毫秒）
        /// </summary>
        public SubscriptionBuilder<T> Debounce(int ms)
        {
            DebounceMs = ms;
            if (ms > 0)
            {
                float debounceSec = ms / 1000f;
                var runnerId = DebounceRunner.Register(() =>
                {
                    if (_disposed) return; // 已取消订阅，跳过

                    if (HasPending && Time.unscaledTime - PendingTime >= debounceSec)
                    {
                        HasPending = false;

                        // Once: 在调用前取消
                        if (OnceFlag)
                        {
                            Unsubscribe();
                        }

                        _originalHandler(ref PendingEvent);
                    }
                });
                _dispatcher._runnerCleanups[_subscriptionId] = runnerId;
            }
            return this;
        }

        /// <summary>
        /// 设置超时时间（毫秒后自动取消）
        /// </summary>
        public SubscriptionBuilder<T> Timeout(int ms)
        {
            if (ms > 0)
            {
                float expireTime = Time.unscaledTime + ms / 1000f;
                var runnerId = DebounceRunner.Register(() =>
                {
                    if (Time.unscaledTime >= expireTime)
                    {
                        Unsubscribe();
                    }
                });
                _dispatcher._runnerCleanups[_subscriptionId] = runnerId;
            }
            return this;
        }

        /// <summary>
        /// 绑定到 GameObject 生命周期
        /// </summary>
        public SubscriptionBuilder<T> AddTo(GameObject go)
        {
            if (go != null)
            {
                var destroyer = go.GetComponent<SubscriptionDestroyer>();
                if (destroyer == null)
                    destroyer = go.AddComponent<SubscriptionDestroyer>();
                destroyer.Add(new SubscriptionResult(_dispatcher, _subscriptionId, _eventType));
            }
            return this;
        }

        /// <summary>
        /// 绑定到 Component 的 GameObject 生命周期
        /// </summary>
        public SubscriptionBuilder<T> AddTo(Component component)
        {
            if (component != null)
                AddTo(component.gameObject);
            return this;
        }

        /// <summary>
        /// 添加到订阅收集器
        /// </summary>
        public SubscriptionBuilder<T> AddTo(SubscriptionCollector collector)
        {
            collector?.Add(new SubscriptionResult(_dispatcher, _subscriptionId, _eventType));
            return this;
        }

        /// <summary>
        /// 订阅ID
        /// </summary>
        public uint Id => _subscriptionId;

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => !_disposed;

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Unsubscribe()
        {
            if (_disposed) return;
            _disposed = true;
            _dispatcher.Unsubscribe(_subscriptionId, _eventType);
        }

        public void Dispose() => Unsubscribe();

        /// <summary>
        /// 转换为 SubscriptionResult
        /// </summary>
        public SubscriptionResult AsResult()
        {
            return new SubscriptionResult(_dispatcher, _subscriptionId, _eventType);
        }

        /// <summary>
        /// 隐式转换为 SubscriptionResult
        /// </summary>
        public static implicit operator SubscriptionResult(SubscriptionBuilder<T> builder)
        {
            return builder.AsResult();
        }
    }
}
