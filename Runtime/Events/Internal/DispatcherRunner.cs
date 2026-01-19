using System.Collections.Generic;
using Azathrix.Framework.Events.Core;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Events.Internal
{
    /// <summary>
    /// 使用UniTask PlayerLoop在帧结束时自动Flush Post事件
    /// </summary>
    internal static class DispatcherRunner
    {
        private static readonly List<EventDispatcher> _instances = new();
        private static bool _initialized;
        private static readonly object _lock = new();
        private static EventDispatcher[] _buffer = new EventDispatcher[8];

        /// <summary>
        /// 注册EventDispatcher实例，首次注册时启动Flush循环
        /// </summary>
        public static void Register(EventDispatcher dispatcher)
        {
            lock (_lock)
            {
                if (!_initialized)
                {
                    _initialized = true;
                    StartFlushLoop().Forget();
                }
                _instances.Add(dispatcher);
            }
        }

        /// <summary>
        /// 注销EventDispatcher实例
        /// </summary>
        public static void Unregister(EventDispatcher dispatcher)
        {
            lock (_lock)
            {
                _instances.Remove(dispatcher);
            }
        }

        /// <summary>
        /// 启动Flush循环，在PostLateUpdate时机执行
        /// </summary>
        private static async UniTaskVoid StartFlushLoop()
        {
            while (true)
            {
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                FlushAll();
            }
        }

        /// <summary>
        /// 刷新所有已注册的EventDispatcher
        /// </summary>
        private static void FlushAll()
        {
            // 复用数组避免每帧ToArray产生GC
            int count;
            lock (_lock)
            {
                count = _instances.Count;
                if (count == 0) return;

                // 容量不足时2倍扩容
                if (_buffer.Length < count)
                    _buffer = new EventDispatcher[count * 2];

                _instances.CopyTo(_buffer);
            }

            for (int i = 0; i < count; i++)
            {
                _buffer[i].Flush();
                _buffer[i] = null; // 清除引用避免内存泄漏
            }
        }
    }
}
