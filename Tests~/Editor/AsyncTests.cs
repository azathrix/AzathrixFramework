using System.Collections.Generic;
using Azathrix.Framework.Events.Results;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// 异步事件测试
    /// </summary>
    public class AsyncTests
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

        public struct AsyncTestEvent
        {
            public int Value;
        }

        [Test]
        public void SubscribeAsync_And_DispatchAsync_Works()
        {
            int received = 0;

            _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                await UniTask.CompletedTask;
                received = e.Value;
            });

            var task = _dispatcher.DispatchAsync(new AsyncTestEvent { Value = 42 });

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
            Assert.AreEqual(42, received);
        }

        [Test]
        public void DispatchAsync_Multiple_Handlers()
        {
            var order = new List<int>();

            _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                await UniTask.CompletedTask;
                order.Add(1);
            });

            _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                await UniTask.CompletedTask;
                order.Add(2);
            });

            var task = _dispatcher.DispatchAsync(new AsyncTestEvent());

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
            Assert.AreEqual(2, order.Count);
        }

        [Test]
        public void DispatchSequentialAsync_Executes_In_Priority_Order()
        {
            var order = new List<int>();

            _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                await UniTask.CompletedTask;
                order.Add(1);
            }, 1);

            _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                await UniTask.CompletedTask;
                order.Add(2);
            }, 2);

            var task = _dispatcher.DispatchSequentialAsync(new AsyncTestEvent());

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
            Assert.AreEqual(2, order.Count);
            // 顺序执行，按优先级（高优先级先执行）
            Assert.AreEqual(2, order[0]);
            Assert.AreEqual(1, order[1]);
        }

        [Test]
        public void DispatchAsync_No_Subscriber_Returns_Completed()
        {
            var task = _dispatcher.DispatchAsync(new AsyncTestEvent());
            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
        }

        [Test]
        public void DispatchAsync_With_Initializer()
        {
            int received = 0;

            _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                await UniTask.CompletedTask;
                received = e.Value;
            });

            var task = _dispatcher.DispatchAsync<AsyncTestEvent>((ref AsyncTestEvent e) => e.Value = 99);

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
            Assert.AreEqual(99, received);
        }

        [Test]
        public void SubscribeAsync_Unsubscribe_Works()
        {
            int callCount = 0;

            var sub = _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                await UniTask.CompletedTask;
                callCount++;
            });

            _dispatcher.DispatchAsync(new AsyncTestEvent());
            Assert.AreEqual(1, callCount);

            sub.Unsubscribe();

            _dispatcher.DispatchAsync(new AsyncTestEvent());
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void SubscribeAsync_Priority_Works()
        {
            var order = new List<int>();

            _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                order.Add(1);
                await UniTask.CompletedTask;
            }, 1);

            _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                order.Add(3);
                await UniTask.CompletedTask;
            }, 3);

            _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                order.Add(2);
                await UniTask.CompletedTask;
            }, 2);

            // 顺序执行才能验证优先级
            _dispatcher.DispatchSequentialAsync(new AsyncTestEvent());

            Assert.AreEqual(3, order[0]);
            Assert.AreEqual(2, order[1]);
            Assert.AreEqual(1, order[2]);
        }

        [Test]
        public void SubscribeAsync_Unsubscribe_Twice_NoThrow()
        {
            var sub = _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                await UniTask.CompletedTask;
            });

            Assert.DoesNotThrow(() =>
            {
                sub.Unsubscribe();
                sub.Unsubscribe();
            });
        }

        [Test]
        public void Unsubscribe_During_DispatchSequential_Skips_Removed()
        {
            int firstCount = 0;
            int secondCount = 0;
            SubscriptionResult secondSub = default;

            _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                firstCount++;
                secondSub.Unsubscribe();
                await UniTask.CompletedTask;
            }, 2);

            secondSub = _dispatcher.SubscribeAsync<AsyncTestEvent>(async e =>
            {
                secondCount++;
                await UniTask.CompletedTask;
            }, 1);

            var task = _dispatcher.DispatchSequentialAsync(new AsyncTestEvent());

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
            Assert.AreEqual(1, firstCount);
            Assert.AreEqual(0, secondCount);
        }
    }
}
