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

        public static void Unregister(EventDispatcher dispatcher)
        {
            lock (_lock)
            {
                _instances.Remove(dispatcher);
            }
        }

        private static async UniTaskVoid StartFlushLoop()
        {
            while (true)
            {
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                FlushAll();
            }
        }

        private static void FlushAll()
        {
            // 复制列表避免在遍历时修改
            EventDispatcher[] dispatchers;
            lock (_lock)
            {
                if (_instances.Count == 0) return;
                dispatchers = _instances.ToArray();
            }

            foreach (var dispatcher in dispatchers)
            {
                dispatcher.Flush();
            }
        }
    }
}
