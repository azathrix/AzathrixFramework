using Azathrix.Framework.Events.Extensions;
using Azathrix.Framework.Events.Results;
using NUnit.Framework;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// 收集器测试
    /// </summary>
    public class CollectorTests
    {
        private Framework.Events.Core.EventDispatcher _dispatcher;

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

        [Test]
        public void SubscriptionCollector_Dispose_Unsubscribes_All()
        {
            int callCount = 0;
            var collector = new SubscriptionCollector();

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).AddTo(collector);
            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) => callCount++).AddTo(collector);
            _dispatcher.Subscribe<TestEventB>((ref TestEventB e) => callCount++).AddTo(collector);

            Assert.AreEqual(3, collector.Count);

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEventA());
            _dispatcher.Dispatch(new TestEventB());
            Assert.AreEqual(3, callCount);

            collector.Dispose();

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEventA());
            _dispatcher.Dispatch(new TestEventB());
            Assert.AreEqual(3, callCount); // 不应增加
        }

        [Test]
        public void MessageSubscriptionCollector_Works()
        {
            int callCount = 0;
            var collector = new MessageSubscriptionCollector();

            _dispatcher.SubscribeMessage<string>("msg1", _ => callCount++).AddTo(collector);
            _dispatcher.SubscribeMessage<string>("msg2", _ => callCount++).AddTo(collector);

            _dispatcher.DispatchMessage("msg1", "test");
            _dispatcher.DispatchMessage("msg2", "test");
            Assert.AreEqual(2, callCount);

            collector.Dispose();

            _dispatcher.DispatchMessage("msg1", "test");
            _dispatcher.DispatchMessage("msg2", "test");
            Assert.AreEqual(2, callCount);
        }

        [Test]
        public void Multiple_Dispatchers_Independent()
        {
            var dispatcher1 = new Framework.Events.Core.EventDispatcher();
            var dispatcher2 = new Framework.Events.Core.EventDispatcher();

            int count1 = 0, count2 = 0;

            dispatcher1.Subscribe<TestEvent>((ref TestEvent e) => count1++);
            dispatcher2.Subscribe<TestEvent>((ref TestEvent e) => count2++);

            dispatcher1.Dispatch(new TestEvent());

            Assert.AreEqual(1, count1);
            Assert.AreEqual(0, count2);

            dispatcher2.Dispatch(new TestEvent());

            Assert.AreEqual(1, count1);
            Assert.AreEqual(1, count2);

            dispatcher1.Dispose();
            dispatcher2.Dispose();
        }
    }
}
