using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Events.Internal
{
    /// <summary>
    /// 防抖/超时检查运行器
    /// </summary>
    internal static class DebounceRunner
    {
        private static readonly Dictionary<uint, Action> _callbacks = new();
        private static uint _idGenerator;
        private static bool _initialized;
        private static readonly object _lock = new();

        public static uint Register(Action callback)
        {
            lock (_lock)
            {
                if (!_initialized)
                {
                    _initialized = true;
                    StartLoop().Forget();
                }

                var id = ++_idGenerator;
                _callbacks[id] = callback;
                return id;
            }
        }

        public static void Unregister(uint id)
        {
            lock (_lock)
            {
                _callbacks.Remove(id);
            }
        }

        private static async UniTaskVoid StartLoop()
        {
            while (true)
            {
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                Tick();
            }
        }

        private static void Tick()
        {
            Action[] callbacks;
            lock (_lock)
            {
                if (_callbacks.Count == 0) return;
                callbacks = new Action[_callbacks.Count];
                _callbacks.Values.CopyTo(callbacks, 0);
            }

            foreach (var callback in callbacks)
            {
                try
                {
                    callback();
                }
                catch (Exception)
                {
                    // 忽略回调异常
                }
            }
        }
    }
}
