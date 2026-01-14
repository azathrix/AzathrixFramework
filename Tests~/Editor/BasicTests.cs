using NUnit.Framework;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// 测试用事件定义
    /// </summary>
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

    /// <summary>
    /// 基础功能测试
    /// </summary>
    public class BasicTests
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
        public void Subscribe_And_Dispatch_Works()
        {
            int received = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                received = e.Value;
            });

            _dispatcher.Dispatch(new TestEvent { Value = 42 });

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_Works()
        {
            int callCount = 0;
            var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            });

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);

            sub.Unsubscribe();
            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount); // 不应该再增加
        }

        [Test]
        public void Once_Works()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            }).Once();

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Priority_Works()
        {
            var order = new System.Collections.Generic.List<int>();

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => order.Add(1)).Priority(1);
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => order.Add(3)).Priority(3);
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => order.Add(2)).Priority(2);

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(3, order[0]); // 优先级高的先执行
            Assert.AreEqual(2, order[1]);
            Assert.AreEqual(1, order[2]);
        }

        [Test]
        public void Multiple_Subscribers_Works()
        {
            int sum = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum += 1);
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum += 10);
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum += 100);

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(111, sum);
        }

        [Test]
        public void Dispatch_With_Initializer_Works()
        {
            int received = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                received = e.Value;
            });

            _dispatcher.Dispatch<TestEvent>((ref TestEvent e) => e.Value = 99);

            Assert.AreEqual(99, received);
        }

        [Test]
        public void Ref_Event_Modification_Works()
        {
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                e.Value *= 2;
            }).Priority(2);

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                e.Value += 10;
            }).Priority(1);

            var evt = new TestEvent { Value = 5 };
            _dispatcher.Dispatch(ref evt);

            // 先 *2 = 10, 再 +10 = 20
            Assert.AreEqual(20, evt.Value);
        }

        [Test]
        public void SetPriority_After_Subscribe()
        {
            var order = new System.Collections.Generic.List<int>();

            var sub1 = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => order.Add(1));
            var sub2 = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => order.Add(2));

            // 默认优先级相同，按订阅顺序
            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, order[0]);
            Assert.AreEqual(2, order[1]);

            order.Clear();

            // 动态修改优先级
            sub2.Priority(10);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(2, order[0]); // sub2 优先级更高，先执行
            Assert.AreEqual(1, order[1]);
        }

        [Test]
        public void ClearSubscriptions_Works()
        {
            int callCount = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++);
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++);
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++);

            Assert.AreEqual(3, _dispatcher.GetSubscriberCount<TestEvent>());

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(3, callCount);

            _dispatcher.ClearSubscriptions<TestEvent>();

            Assert.AreEqual(0, _dispatcher.GetSubscriberCount<TestEvent>());

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(3, callCount); // 不应增加
        }

        [Test]
        public void SubscriptionResult_IsValid()
        {
            var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });
            Assert.IsTrue(sub.IsValid);
            Assert.AreNotEqual(0u, sub.Id);
        }

        [Test]
        public void Dispose_Pattern_Works()
        {
            int callCount = 0;

            using (var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++))
            {
                _dispatcher.Dispatch(new TestEvent());
                Assert.AreEqual(1, callCount);
            }

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount); // using 结束后自动取消
        }

        [Test]
        public void Dispose_Idempotent_NoThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _dispatcher.Dispose();
                _dispatcher.Dispose();
            });
        }

        [Test]
        public void Dispatch_After_Dispose_NoThrow()
        {
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });
            _dispatcher.Dispose();

            Assert.DoesNotThrow(() =>
            {
                _dispatcher.Dispatch(new TestEvent());
            });
        }
    }
}
