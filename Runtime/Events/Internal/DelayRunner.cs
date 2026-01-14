using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azathrix.Framework.Events.Internal
{
    /// <summary>
    /// 延迟执行运行器
    /// </summary>
    internal static class DelayRunner
    {
        private struct DelayedAction
        {
            public float ExecuteTime;
            public Action Callback;
        }

        private static readonly List<DelayedAction> _pending = new();
        private static bool _initialized;
        private static readonly object _lock = new();

        public static void Schedule(int delayMs, Action callback)
        {
            lock (_lock)
            {
                if (!_initialized)
                {
                    _initialized = true;
                    StartLoop().Forget();
                }

                _pending.Add(new DelayedAction
                {
                    ExecuteTime = Time.unscaledTime + delayMs / 1000f,
                    Callback = callback
                });
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
            if (_pending.Count == 0) return;

            float now = Time.unscaledTime;
            List<Action> toExecute = null;

            lock (_lock)
            {
                for (int i = _pending.Count - 1; i >= 0; i--)
                {
                    if (now >= _pending[i].ExecuteTime)
                    {
                        toExecute ??= new List<Action>();
                        toExecute.Add(_pending[i].Callback);
                        _pending.RemoveAt(i);
                    }
                }
            }

            if (toExecute != null)
            {
                foreach (var action in toExecute)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception)
                    {
                        // 忽略回调异常
                    }
                }
            }
        }
    }
}
