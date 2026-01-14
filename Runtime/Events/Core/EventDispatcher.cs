using System;
using System.Collections.Generic;
using Azathrix.Framework.Events.Internal;

namespace Azathrix.Framework.Events.Core
{
    /// <summary>
    /// 高性能事件分发器
    /// 支持：即时事件、Post事件、异步事件、消息事件、拦截器、返回值查询
    /// </summary>
    public sealed partial class EventDispatcher : IDisposable
    {
        /// <summary>
        /// 事件分发器版本
        /// </summary>
        public const string Version = "1.0.0";

        private uint _idGenerator;
        private bool _disposed;

        // 清理委托列表（用于 Dispose 时清理所有泛型通道）
        private readonly List<Action> _cleanupActions = new();

        // 存储 debounce/timeout 清理映射
        internal readonly Dictionary<uint, uint> _runnerCleanups = new();

        // 泛型通道缓存（通过静态泛型类实现零GC访问）
        private static class ChannelCache<T> where T : struct
        {
            public static readonly Dictionary<EventDispatcher, EventChannel<T>> Channels = new();
        }

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
        /// 直接取消订阅（泛型版本，无反射）
        /// </summary>
        public void Unsubscribe<T>(uint id) where T : struct
        {
            if (ChannelCache<T>.Channels.TryGetValue(this, out var channel))
            {
                channel.Unsubscribe(id);
            }
        }

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