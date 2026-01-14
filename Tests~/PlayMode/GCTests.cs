using System;
using System.Collections;
using Azathrix.Framework.Events.Interceptors;
using NUnit.Framework;
using Unity.Profiling;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace Azathrix.EventDispatcher.Tests.Tests.PlayMode
{
    // PlayMode 测试用事件定义
    public struct TestEvent
    {
        public int Value;
    }

    public struct TestEventA
    {
        public string Message;
    }

    public struct TestEventB
    {
        public int Count;
    }

    // 包含引用类型的事件结构体
    public struct EventWithReference
    {
        public int Value;
        public string Message;      // 引用类型
        public int[] Data;          // 引用类型
    }

    /// <summary>
    /// GC分配测试
    /// 注意：GC 检测在 Unity 测试环境中可能不完全准确
    /// 建议配合 Unity Profiler 的 Deep Profile 模式验证
    /// </summary>
    public class GCTests
    {
        private Framework.Events.Core.EventDispatcher _dispatcher;
        private const int DispatchCount = 10000;
        private const int SubscribeCount = 1000;
        private const long NoGcThresholdBytes = 100000;
        private bool _profilerWasEnabled;
        private int _sum;
        private int _messageCount;

        [SetUp]
        public void Setup()
        {
            _profilerWasEnabled = Profiler.enabled;
            Profiler.enabled = true;
            _dispatcher = new Framework.Events.Core.EventDispatcher();
            // 预热：确保所有静态初始化完成
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { }).Unsubscribe();
            _dispatcher.Dispatch(new TestEvent());
            GC.Collect();
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher?.Dispose();
            Profiler.enabled = _profilerWasEnabled;
        }

        private void AccumulateTestEvent(ref TestEvent e)
        {
            _sum += e.Value;
        }

        private void ModifyTestEvent(ref TestEvent e)
        {
            e.Value *= 2;
        }

        private InterceptResult InterceptDouble(ref InterceptorContext<TestEvent> ctx)
        {
            ctx.Event.Value *= 2;
            return InterceptResult.Continue;
        }

        private void AccumulateEventWithReference(ref EventWithReference e)
        {
            _sum += e.Message?.Length ?? 0;
            if (e.Data != null)
            {
                for (int i = 0; i < e.Data.Length; i++)
                {
                    _sum += e.Data[i];
                }
            }
        }

        private void OnStringMessage(string _)
        {
            _messageCount++;
        }

        private void OnEventMessage(TestEvent _)
        {
            _messageCount++;
        }

        private static IEnumerator MeasureGcAllocBytes(string sampleName, string label, Action action, Action<long> assert)
        {
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 1))
            {
                if (!recorder.Valid)
                {
                    Assert.Inconclusive("GC allocation metrics are not supported on this runtime.");
                    yield break;
                }

                // 让 recorder 至少经历一帧
                yield return null;

                // 基线帧（空操作），用于抵消编辑器噪音
                yield return null;
                var baseline = recorder.LastValue;

                action();

                // 下一帧读取上一帧的 GC.Alloc
                yield return null;

                var allocated = recorder.LastValue;
                var netAllocated = allocated - baseline;
                if (netAllocated < 0) netAllocated = 0;
                Debug.Log($"{label} allocated: {netAllocated} bytes (raw {allocated}, baseline {baseline})");
                Measure.Custom(new SampleGroup(sampleName, SampleUnit.Byte), netAllocated);
                assert(netAllocated);
            }
        }

        [UnityTest, Performance]
        public IEnumerator Dispatch_NoGC()
        {
            _sum = 0;
            _dispatcher.Subscribe<TestEvent>(AccumulateTestEvent);
            var events = new TestEvent[DispatchCount];
            for (int i = 0; i < DispatchCount; i++)
            {
                events[i] = new TestEvent { Value = i };
            }

            return MeasureGcAllocBytes(
                "GC.Dispatch.NoGC",
                $"Dispatch {DispatchCount} times",
                () =>
                {
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        _dispatcher.Dispatch(events[i]);
                    }
                },
                allocated => Assert.LessOrEqual(allocated, NoGcThresholdBytes));
        }

        [UnityTest, Performance]
        public IEnumerator Dispatch_Ref_NoGC()
        {
            _sum = 0;
            _dispatcher.Subscribe<TestEvent>(AccumulateTestEvent);
            var events = new TestEvent[DispatchCount];
            for (int i = 0; i < DispatchCount; i++)
            {
                events[i] = new TestEvent { Value = i };
            }

            return MeasureGcAllocBytes(
                "GC.Dispatch.Ref.NoGC",
                $"Dispatch ref {DispatchCount} times",
                () =>
                {
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        _dispatcher.Dispatch(ref events[i]);
                    }
                },
                allocated => Assert.LessOrEqual(allocated, NoGcThresholdBytes));
        }

        [UnityTest, Performance]
        public IEnumerator Subscribe_Unsubscribe_WithClosure()
        {
            // 预热
            _sum = 0;
            var warmup = _dispatcher.Subscribe<TestEvent>(AccumulateTestEvent);
            warmup.Unsubscribe();

            return MeasureGcAllocBytes(
                "GC.Subscribe.WithClosure",
                $"Subscribe/Unsubscribe {SubscribeCount} times (with closure)",
                () =>
                {
                    // 订阅和取消（捕获变量，强制创建闭包）
                    for (int i = 0; i < SubscribeCount; i++)
                    {
                        int captured = i;
                        var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { _sum = captured + e.Value; });
                        sub.Unsubscribe();
                    }
                },
                allocated => Assert.Greater(allocated, 0));
        }

        [UnityTest, Performance]
        public IEnumerator Multiple_Dispatch_NoGC()
        {
            _sum = 0;
            for (int i = 0; i < 10; i++)
            {
                _dispatcher.Subscribe<TestEvent>(AccumulateTestEvent);
            }

            var events = new TestEvent[DispatchCount];
            for (int i = 0; i < DispatchCount; i++)
            {
                events[i] = new TestEvent { Value = i };
            }

            return MeasureGcAllocBytes(
                "GC.Dispatch.Multiple.NoGC",
                $"Dispatch to 10 subscribers {DispatchCount} times",
                () =>
                {
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        _dispatcher.Dispatch(events[i]);
                    }
                },
                allocated => Assert.LessOrEqual(allocated, NoGcThresholdBytes));
        }

        [UnityTest, Performance]
        public IEnumerator Event_Modification_NoGC()
        {
            _sum = 0;
            _dispatcher.Subscribe<TestEvent>(ModifyTestEvent).Priority(2);
            _dispatcher.Subscribe<TestEvent>(AccumulateTestEvent).Priority(1);

            var events = new TestEvent[DispatchCount];
            for (int i = 0; i < DispatchCount; i++)
            {
                events[i] = new TestEvent { Value = i };
            }

            return MeasureGcAllocBytes(
                "GC.Event.Modification.NoGC",
                $"Event modification {DispatchCount} times",
                () =>
                {
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        _dispatcher.Dispatch(ref events[i]);
                    }
                },
                allocated => Assert.LessOrEqual(allocated, NoGcThresholdBytes));
        }

        [UnityTest, Performance]
        public IEnumerator Interceptor_NoGC()
        {
            _dispatcher.AddInterceptor<TestEvent>(InterceptDouble);

            _sum = 0;
            _dispatcher.Subscribe<TestEvent>(AccumulateTestEvent);
            var events = new TestEvent[DispatchCount];
            for (int i = 0; i < DispatchCount; i++)
            {
                events[i] = new TestEvent { Value = i };
            }

            return MeasureGcAllocBytes(
                "GC.Interceptor.NoGC",
                $"Interceptor {DispatchCount} times",
                () =>
                {
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        _dispatcher.Dispatch(events[i]);
                    }
                },
                allocated => Assert.LessOrEqual(allocated, NoGcThresholdBytes));
        }

        [UnityTest, Performance]
        public IEnumerator EventWithReference_ReuseObjects_NoGC()
        {
            _dispatcher.Subscribe<EventWithReference>((ref EventWithReference e) => { }).Unsubscribe();
            _dispatcher.Dispatch(new EventWithReference());

            string cachedMessage = "test message";
            int[] cachedData = new int[] { 1, 2, 3 };

            _sum = 0;
            _dispatcher.Subscribe<EventWithReference>(AccumulateEventWithReference);
            var events = new EventWithReference[DispatchCount];
            for (int i = 0; i < DispatchCount; i++)
            {
                events[i] = new EventWithReference
                {
                    Value = i,
                    Message = cachedMessage,
                    Data = cachedData
                };
            }

            return MeasureGcAllocBytes(
                "GC.EventWithReference.Reuse",
                $"EventWithReference (reuse) {DispatchCount} times",
                () =>
                {
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        _dispatcher.Dispatch(events[i]);
                    }
                },
                allocated => Assert.LessOrEqual(allocated, NoGcThresholdBytes));
        }

        [UnityTest, Performance]
        public IEnumerator EventWithReference_Precreated_NoGC()
        {
            _sum = 0;
            _dispatcher.Subscribe<EventWithReference>(AccumulateEventWithReference);
            var events = new EventWithReference[DispatchCount];
            for (int i = 0; i < DispatchCount; i++)
            {
                events[i] = new EventWithReference
                {
                    Value = i,
                    Message = "message " + i,
                    Data = new int[] { i, i + 1 }
                };
            }

            return MeasureGcAllocBytes(
                "GC.EventWithReference.Precreated",
                $"EventWithReference (precreated) {DispatchCount} times",
                () =>
                {
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        _dispatcher.Dispatch(events[i]);
                    }
                },
                allocated => Assert.LessOrEqual(allocated, NoGcThresholdBytes));
        }

        [UnityTest, Performance]
        public IEnumerator Message_Dispatch_Allocates()
        {
            const string messageKey = "gc.test";
            const string payload = "data";
            _messageCount = 0;
            _dispatcher.SubscribeMessage<string>(messageKey, OnStringMessage);

            return MeasureGcAllocBytes(
                "GC.Message.Dispatch",
                "Message dispatch",
                () => { _dispatcher.DispatchMessage(messageKey, payload); },
                allocated => Assert.LessOrEqual(allocated, NoGcThresholdBytes));
        }

        [UnityTest, Performance]
        public IEnumerator Message_DispatchSerialized_Allocates()
        {
            const string messageKey = "gc.serialized";
            _messageCount = 0;
            _dispatcher.SubscribeMessage<TestEvent>(messageKey, OnEventMessage);
            var payload = new TestEvent { Value = 7 };

            return MeasureGcAllocBytes(
                "GC.Message.Serialized",
                "Message serialized dispatch",
                () => { _dispatcher.DispatchMessageSerialized(messageKey, payload); },
                allocated => Assert.LessOrEqual(allocated, NoGcThresholdBytes));
        }
    }
}
