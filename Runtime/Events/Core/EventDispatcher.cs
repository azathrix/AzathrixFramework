using System;
using System.Collections.Generic;
using Azathrix.Framework.Events.Internal;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// 高性能事件分发器
    /// 支持：即时事件、Post事件、异步事件、消息事件、拦截器、返回值查询
    ///
    /// <para><b>基本用法：</b></para>
    /// <code>
    /// // 1. 创建分发器
    /// var dispatcher = new EventDispatcher();
    ///
    /// // 2. 定义事件结构体
    /// public struct PlayerDamageEvent
    /// {
    ///     public int Damage;
    ///     public string Source;
    /// }
    ///
    /// // 3. 订阅事件
    /// dispatcher.Subscribe&lt;PlayerDamageEvent&gt;(ref evt => {
    ///     Debug.Log($"受到 {evt.Damage} 点伤害");
    /// }).AddTo(gameObject);  // 绑定生命周期
    ///
    /// // 4. 分发事件
    /// dispatcher.Dispatch(new PlayerDamageEvent { Damage = 10, Source = "Enemy" });
    /// </code>
    ///
    /// <para><b>链式配置：</b></para>
    /// <code>
    /// dispatcher.Subscribe&lt;MyEvent&gt;(handler)
    ///     .Where(ref evt => evt.Value > 0)  // 过滤
    ///     .Priority(100)                     // 优先级（越大越先执行）
    ///     .Once()                            // 只触发一次
    ///     .Throttle(100)                     // 节流100ms
    ///     .Debounce(200)                     // 防抖200ms
    ///     .Delay(500)                        // 延迟500ms执行
    ///     .Timeout(5000)                     // 5秒后自动取消
    ///     .Sticky()                          // 立即收到最后一个值
    ///     .AddTo(gameObject);                // 绑定生命周期
    /// </code>
    ///
    /// <para><b>事件类型：</b></para>
    /// <list type="bullet">
    /// <item>Dispatch - 即时同步分发</item>
    /// <item>Post - 延迟到帧结束分发（线程安全）</item>
    /// <item>DispatchAsync - 异步分发</item>
    /// <item>DispatchSticky - 粘性事件（新订阅者立即收到最后值）</item>
    /// <item>Query - 带返回值的查询事件</item>
    /// <item>Message - 基于字符串ID的消息事件</item>
    /// </list>
    /// </summary>
    public sealed partial class EventDispatcher : IDisposable
    {
        /// <summary>
        /// 事件分发器版本
        /// </summary>
        public const string Version = "1.0.1";

        private uint _idGenerator;
        private bool _disposed;

        // 清理委托列表（用于 Dispose 时清理所有泛型通道）
        private readonly List<Action> _cleanupActions = new();

        // 存储 debounce/timeout 清理映射
        internal readonly Dictionary<uint, uint> _runnerCleanups = new();

        /// <summary>
        /// 泛型通道缓存（通过静态泛型类实现零GC访问）
        /// </summary>
        /// <remarks>
        /// 利用C#泛型类的静态字段特性，每个T类型都有独立的静态字典，
        /// 避免了Dictionary&lt;Type, object&gt;的装箱开销
        /// </remarks>
        private static class ChannelCache<T> where T : struct
        {
            public static readonly Dictionary<EventDispatcher, EventChannel<T>> Channels = new();
        }

        /// <summary>
        /// 创建事件分发器实例
        /// </summary>
        /// <remarks>
        /// 创建时自动注册到DispatcherRunner，在PostLateUpdate时机自动Flush Post事件
        /// </remarks>
        public EventDispatcher()
        {
            DispatcherRunner.Register(this);
        }

        /// <summary>
        /// 生成唯一ID
        /// </summary>
        internal uint GenerateId() => ++_idGenerator;

        /// <summary>
        /// 获取或创建事件通道
        /// </summary>
        internal EventChannel<T> GetOrCreateChannel<T>() where T : struct
        {
            if (!ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel = new EventChannel<T>(this);
                ChannelCache<T>.Channels[this] = channel;
                // 注册清理委托
                _cleanupActions.Add(() => ChannelCache<T>.Channels.Remove(this));
            }

            return channel;
        }

        /// <summary>
        /// 设置订阅优先级
        /// </summary>
        internal void SetPriority(uint id, Type eventType, int priority)
        {
            // 通过反射调用泛型方法（仅在配置时使用，不影响运行时性能）
            var method = typeof(EventDispatcher).GetMethod(nameof(SetPriorityInternal),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var generic = method?.MakeGenericMethod(eventType);
            generic?.Invoke(this, new object[] {id, priority});
        }

        private void SetPriorityInternal<T>(uint id, int priority) where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.SetPriority(id, priority);
            }
        }

        /// <summary>
        /// 设置一次性订阅
        /// </summary>
        internal void SetOnce(uint id, Type eventType)
        {
            var method = typeof(EventDispatcher).GetMethod(nameof(SetOnceInternal),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var generic = method?.MakeGenericMethod(eventType);
            generic?.Invoke(this, new object[] {id});
        }

        private void SetOnceInternal<T>(uint id) where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.SetOnce(id);
            }
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        internal void Unsubscribe(uint id, Type eventType)
        {
            // 清理 debounce/timeout 回调
            if (_runnerCleanups.TryGetValue(id, out var runnerId))
            {
                DebounceRunner.Unregister(runnerId);
                _runnerCleanups.Remove(id);
            }

            var method = typeof(EventDispatcher).GetMethod(nameof(UnsubscribeInternal),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var generic = method?.MakeGenericMethod(eventType);
            generic?.Invoke(this, new object[] {id});
        }

        private void UnsubscribeInternal<T>(uint id) where T : struct
        {
            // 检查普通事件通道
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Unsubscribe(id);
            }

            // 检查异步事件通道
            if (AsyncChannelCache<T>.Channels.TryGetValue(this, out var asyncChannel))
            {
                asyncChannel.Unsubscribe(id);
            }
        }

        /// <summary>
        /// 直接取消订阅（泛型版本，无反射，性能更好）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="id">订阅ID</param>
        /// <example>
        /// <code>
        /// var id = dispatcher.Subscribe&lt;MyEvent&gt;(handler).Id;
        /// // 稍后取消
        /// dispatcher.Unsubscribe&lt;MyEvent&gt;(id);
        /// </code>
        /// </example>
        public void Unsubscribe<T>(uint id) where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Unsubscribe(id);
            }
        }

        /// <summary>
        /// 清理所有静态缓存和订阅，但保持分发器可用
        /// </summary>
        public void Clear()
        {
            foreach (var cleanup in _cleanupActions)
                cleanup();
            _cleanupActions.Clear();

            foreach (var runnerId in _runnerCleanups.Values)
                DebounceRunner.Unregister(runnerId);
            _runnerCleanups.Clear();

            _idGenerator = 0;
        }

        /// <summary>
        /// 释放分发器资源
        /// </summary>
        /// <remarks>
        /// 释放时会：
        /// 1. 从DispatcherRunner注销
        /// 2. 清理所有泛型通道缓存
        /// 3. 清理所有订阅
        /// </remarks>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            DispatcherRunner.Unregister(this);

            // 清理所有泛型通道缓存
            foreach (var cleanup in _cleanupActions)
            {
                cleanup();
            }

            _cleanupActions.Clear();
        }
    }
}