using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Azathrix.EventDispatcher.Tests.Tests.PlayMode
{
    /// <summary>
    /// 压力测试
    /// </summary>
    public class StressTests
    {
        private Framework.Events.Core.EventDispatcher _dispatcher;
        private const int DispatchCount = 10000;

        [SetUp]
        public void Setup()
        {
            _dispatcher = new Framework.Events.Core.EventDispatcher();
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher?.Dispose();
        }

        [Test, Performance]
        public void Stress_10000_Subscriptions()
        {
            Measure.Method(() =>
                {
                    var dispatcher = new Framework.Events.Core.EventDispatcher();
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });
                    }

                    Assert.AreEqual(DispatchCount, dispatcher.GetSubscriberCount<TestEvent>());
                    dispatcher.Dispose();
                })
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(1)
                .Run();
        }

        [Test, Performance]
        public void Stress_10000_Dispatches()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            });

            Measure.Method(() =>
                {
                    callCount = 0;
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        _dispatcher.Dispatch(new TestEvent { Value = i });
                    }

                    Assert.AreEqual(DispatchCount, callCount);
                })
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(1)
                .Run();
        }

        [Test, Performance]
        public void Stress_10000_Subscribers_Single_Dispatch()
        {
            int callCount = 0;

            for (int i = 0; i < DispatchCount; i++)
            {
                _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
                {
                    callCount++;
                });
            }

            Measure.Method(() =>
                {
                    callCount = 0;
                    _dispatcher.Dispatch(new TestEvent());
                    Assert.AreEqual(DispatchCount, callCount);
                })
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(1)
                .Run();
        }

        [Test, Performance]
        public void Stress_High_Frequency_Dispatch()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            });

            Measure.Method(() =>
                {
                    callCount = 0;
                    for (int frame = 0; frame < 100; frame++)
                    {
                        for (int i = 0; i < 1000; i++)
                        {
                            _dispatcher.Dispatch(new TestEvent());
                        }
                    }

                    Assert.AreEqual(100000, callCount);
                })
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(1)
                .Run();
        }

        [Test, Performance]
        public void Stress_10000_Posts_And_Flush()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            });

            Measure.Method(() =>
                {
                    callCount = 0;
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        _dispatcher.Post(new TestEvent());
                    }

                    _dispatcher.Flush();
                    Assert.AreEqual(DispatchCount, callCount);
                })
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(1)
                .Run();
        }

        [Test, Performance]
        public void Stress_Subscribe_Unsubscribe_Cycle()
        {
            Measure.Method(() =>
                {
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });
                        sub.Unsubscribe();
                    }

                    Assert.AreEqual(0, _dispatcher.GetSubscriberCount<TestEvent>());
                })
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(1)
                .Run();
        }

        [Test, Performance]
        public void Stress_Multiple_Event_Types()
        {
            int countA = 0, countB = 0, countTest = 0;

            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) => countA++);
            _dispatcher.Subscribe<TestEventB>((ref TestEventB e) => countB++);
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => countTest++);

            Measure.Method(() =>
                {
                    countA = 0;
                    countB = 0;
                    countTest = 0;
                    for (int i = 0; i < DispatchCount; i++)
                    {
                        _dispatcher.Dispatch(new TestEventA());
                        _dispatcher.Dispatch(new TestEventB());
                        _dispatcher.Dispatch(new TestEvent());
                    }

                    Assert.AreEqual(DispatchCount, countA);
                    Assert.AreEqual(DispatchCount, countB);
                    Assert.AreEqual(DispatchCount, countTest);
                })
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(1)
                .Run();
        }
    }
}
