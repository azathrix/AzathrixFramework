using Azathrix.Framework.Events.Interceptors;
using Azathrix.Framework.Events.Results;
using NUnit.Framework;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// 拦截器测试
    /// </summary>
    public class InterceptorTests
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
        public void Interceptor_Can_Cancel_Event()
        {
            int handlerCallCount = 0;

            _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                return InterceptResult.Cancel;
            });

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                handlerCallCount++;
            });

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(0, handlerCallCount);
        }

        [Test]
        public void Interceptor_Can_Modify_Event()
        {
            int receivedValue = 0;

            _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                ctx.Event.Value *= 2;
                return InterceptResult.Continue;
            });

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                receivedValue = e.Value;
            });

            _dispatcher.Dispatch(new TestEvent { Value = 10 });

            Assert.AreEqual(20, receivedValue);
        }

        [Test]
        public void Interceptor_Priority_Works()
        {
            var order = new System.Collections.Generic.List<int>();

            _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                order.Add(1);
                return InterceptResult.Continue;
            }, 1);

            _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                order.Add(3);
                return InterceptResult.Continue;
            }, 3);

            _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                order.Add(2);
                return InterceptResult.Continue;
            }, 2);

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(3, order[0]);
            Assert.AreEqual(2, order[1]);
            Assert.AreEqual(1, order[2]);
        }

        [Test]
        public void Interceptor_Chain_Stops_On_Cancel()
        {
            var order = new System.Collections.Generic.List<int>();

            _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                order.Add(1);
                return InterceptResult.Cancel; // 取消
            }, 3);

            _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                order.Add(2); // 不应该执行
                return InterceptResult.Continue;
            }, 1);

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(1, order.Count);
            Assert.AreEqual(1, order[0]);
        }

        [Test]
        public void Interceptor_Conditional_Cancel()
        {
            int handlerCallCount = 0;

            _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                // 只取消 Value > 100 的事件
                if (ctx.Event.Value > 100)
                    return InterceptResult.Cancel;
                return InterceptResult.Continue;
            });

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                handlerCallCount++;
            });

            _dispatcher.Dispatch(new TestEvent { Value = 50 });
            Assert.AreEqual(1, handlerCallCount);

            _dispatcher.Dispatch(new TestEvent { Value = 150 });
            Assert.AreEqual(1, handlerCallCount); // 被拦截
        }

        [Test]
        public void RemoveInterceptor_Works()
        {
            int handlerCallCount = 0;

            var interceptor = _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                return InterceptResult.Cancel;
            });

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                handlerCallCount++;
            });

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(0, handlerCallCount); // 被拦截

            // 移除拦截器
            _dispatcher.RemoveInterceptor<TestEvent>(interceptor.Id);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, handlerCallCount); // 不再被拦截
        }

        [Test]
        public void Interceptor_Unsubscribe_Via_Result()
        {
            int handlerCallCount = 0;

            var interceptor = _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                return InterceptResult.Cancel;
            });

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                handlerCallCount++;
            });

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(0, handlerCallCount);

            // 通过 SubscriptionResult.Unsubscribe 移除
            interceptor.Unsubscribe();

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, handlerCallCount);
        }

        [Test]
        public void RemoveInterceptor_During_Dispatch_Skips_Removed()
        {
            var order = new System.Collections.Generic.List<int>();
            SubscriptionResult high = default;
            SubscriptionResult low = default;

            low = _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                order.Add(2);
                return InterceptResult.Continue;
            }, 1);

            high = _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                order.Add(1);
                _dispatcher.RemoveInterceptor<TestEvent>(low.Id);
                return InterceptResult.Continue;
            }, 2);

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(1, order.Count);
            Assert.AreEqual(1, order[0]);
        }
    }
}
