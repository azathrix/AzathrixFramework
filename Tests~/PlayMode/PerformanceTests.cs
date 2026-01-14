using System.Diagnostics;
using NUnit.Framework;
using Unity.PerformanceTesting;
using Debug = UnityEngine.Debug;

namespace Azathrix.EventDispatcher.Tests.Tests.PlayMode
{
    /// <summary>
    /// 性能测试 - 输出详细耗时数据
    /// </summary>
    public class PerformanceTests
    {
        private Framework.Events.Core.EventDispatcher _dispatcher;
        private const int IterationCount = 100000;

        [SetUp]
        public void Setup()
        {
            _dispatcher = new Framework.Events.Core.EventDispatcher();
            // 预热
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { }).Unsubscribe();
            _dispatcher.Dispatch(new TestEvent());
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher?.Dispose();
        }

        [Test, Performance]
        public void Perf_Dispatch_100K()
        {
            int sum = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum += e.Value);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < IterationCount; i++)
            {
                _dispatcher.Dispatch(new TestEvent { Value = 1 });
            }
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / IterationCount;
            Debug.Log($"Dispatch {IterationCount} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("Dispatch.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(IterationCount, sum);
        }

        [Test, Performance]
        public void Perf_Dispatch_Ref_100K()
        {
            int sum = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum += e.Value);

            var evt = new TestEvent { Value = 1 };
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < IterationCount; i++)
            {
                _dispatcher.Dispatch(ref evt);
            }
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / IterationCount;
            Debug.Log($"Dispatch ref {IterationCount} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("Dispatch.Ref.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(IterationCount, sum);
        }

        [Test, Performance]
        public void Perf_Dispatch_10_Subscribers_100K()
        {
            int sum = 0;
            for (int i = 0; i < 10; i++)
            {
                _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum++);
            }

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < IterationCount; i++)
            {
                _dispatcher.Dispatch(new TestEvent());
            }
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / IterationCount;
            Debug.Log($"Dispatch to 10 subscribers {IterationCount} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("Dispatch.10Subs.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(IterationCount * 10, sum);
        }

        [Test, Performance]
        public void Perf_Dispatch_100_Subscribers_10K()
        {
            const int count = 10000;
            int sum = 0;
            for (int i = 0; i < 100; i++)
            {
                _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum++);
            }

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                _dispatcher.Dispatch(new TestEvent());
            }
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / count;
            Debug.Log($"Dispatch to 100 subscribers {count} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("Dispatch.100Subs.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(count * 100, sum);
        }

        [Test, Performance]
        public void Perf_Subscribe_Unsubscribe_10K()
        {
            const int count = 10000;

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });
                sub.Unsubscribe();
            }
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / count;
            Debug.Log($"Subscribe/Unsubscribe {count} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("SubUnsub.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(0, _dispatcher.GetSubscriberCount<TestEvent>());
        }

        [Test, Performance]
        public void Perf_Post_And_Flush_100K()
        {
            int sum = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum++);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < IterationCount; i++)
            {
                _dispatcher.Post(new TestEvent());
            }
            _dispatcher.Flush();
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / IterationCount;
            Debug.Log($"Post+Flush {IterationCount} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("PostFlush.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(IterationCount, sum);
        }

        [Test, Performance]
        public void Perf_Interceptor_100K()
        {
            int sum = 0;
            _dispatcher.AddInterceptor<TestEvent>((ref Framework.Events.Interceptors.InterceptorContext<TestEvent> ctx) =>
            {
                ctx.Event.Value *= 2;
                return Framework.Events.Interceptors.InterceptResult.Continue;
            });
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum += e.Value);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < IterationCount; i++)
            {
                _dispatcher.Dispatch(new TestEvent { Value = 1 });
            }
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / IterationCount;
            Debug.Log($"Dispatch with interceptor {IterationCount} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("Interceptor.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(IterationCount * 2, sum);
        }

        [Test, Performance]
        public void Perf_Priority_Dispatch_100K()
        {
            int sum = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum++).Priority(1);
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum++).Priority(2);
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum++).Priority(3);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < IterationCount; i++)
            {
                _dispatcher.Dispatch(new TestEvent());
            }
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / IterationCount;
            Debug.Log($"Dispatch with priority {IterationCount} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("Priority.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(IterationCount * 3, sum);
        }

        [Test, Performance]
        public void Perf_Where_Filter_100K()
        {
            int sum = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum++)
                .Where((ref TestEvent e) => e.Value > 0);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < IterationCount; i++)
            {
                _dispatcher.Dispatch(new TestEvent { Value = 1 });
            }
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / IterationCount;
            Debug.Log($"Dispatch with Where filter {IterationCount} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("Where.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(IterationCount, sum);
        }

        [Test, Performance]
        public void Perf_Message_Dispatch_10K()
        {
            const int count = 10000;
            int sum = 0;
            _dispatcher.SubscribeMessage<int>("perf.test", v => sum += v);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                _dispatcher.DispatchMessage("perf.test", 1);
            }
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / count;
            Debug.Log($"Message dispatch {count} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("Message.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(count, sum);
        }

        [Test, Performance]
        public void Perf_Query_100K()
        {
            _dispatcher.SubscribeQuery<TestEvent, int>((ref TestEvent e) => e.Value * 2);

            int sum = 0;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < IterationCount; i++)
            {
                sum += _dispatcher.Query<TestEvent, int>(new TestEvent { Value = 1 }, (a, b) => a + b);
            }
            sw.Stop();

            double nsPerOp = sw.Elapsed.TotalMilliseconds * 1000000 / IterationCount;
            Debug.Log($"Query {IterationCount} times: {sw.Elapsed.TotalMilliseconds:F2}ms ({nsPerOp:F0}ns/op)");
            Measure.Custom(new SampleGroup("Query.NsPerOp", SampleUnit.Nanosecond), nsPerOp);

            Assert.AreEqual(IterationCount * 2, sum);
        }
    }
}
