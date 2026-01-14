using Azathrix.Framework.Events.Interceptors;
using Azathrix.Framework.Events.Results;
using NUnit.Framework;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// 套娃测试 - 在事件处理中进行各种操作
    /// </summary>
    public class NestedTests
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
        public void Subscribe_During_Dispatch_NewSubscriber_NotCalled()
        {
            // 在事件处理中订阅同类型事件，新订阅者不应收到当前事件
            int firstCallCount = 0;
            int secondCallCount = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                firstCallCount++;
                // 在处理中订阅
                _dispatcher.Subscribe<TestEvent>((ref TestEvent e2) =>
                {
                    secondCallCount++;
                });
            });

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(1, firstCallCount);
            Assert.AreEqual(0, secondCallCount); // 新订阅者不应被调用

            // 第二次分发，两个都应该被调用
            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(2, firstCallCount);
            Assert.AreEqual(1, secondCallCount);
        }

        [Test]
        public void Unsubscribe_Self_During_Dispatch()
        {
            // 在事件处理中取消自己的订阅
            int callCount = 0;
            SubscriptionResult sub = default;

            sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
                sub.Unsubscribe();
            });

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount); // 不应再被调用
        }

        [Test]
        public void Unsubscribe_Other_During_Dispatch()
        {
            // 在事件处理中取消其他订阅者
            int firstCallCount = 0;
            int secondCallCount = 0;
            SubscriptionResult secondSub = default;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                firstCallCount++;
                secondSub.Unsubscribe();
            }).Priority(2); // 高优先级先执行

            secondSub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                secondCallCount++;
            }).Priority(1);

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(1, firstCallCount);
            Assert.AreEqual(0, secondCallCount); // 被取消了，不应该被调用
        }

        [Test]
        public void Dispatch_Same_Event_During_Dispatch()
        {
            // 在事件处理中分发同类型事件（递归）
            int callCount = 0;
            int maxDepth = 3;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
                if (e.Value < maxDepth)
                {
                    _dispatcher.Dispatch(new TestEvent { Value = e.Value + 1 });
                }
            });

            _dispatcher.Dispatch(new TestEvent { Value = 1 });

            Assert.AreEqual(maxDepth, callCount);
        }

        [Test]
        public void Dispatch_Different_Event_During_Dispatch()
        {
            // 在事件处理中分发其他类型事件
            int eventACount = 0;
            int eventBCount = 0;

            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) =>
            {
                eventACount++;
                _dispatcher.Dispatch(new TestEventB { Count = 1 });
            });

            _dispatcher.Subscribe<TestEventB>((ref TestEventB e) =>
            {
                eventBCount++;
            });

            _dispatcher.Dispatch(new TestEventA { Message = "test" });

            Assert.AreEqual(1, eventACount);
            Assert.AreEqual(1, eventBCount);
        }

        [Test]
        public void Multi_Level_Nested_Events()
        {
            // 多层嵌套：A -> B -> TestEvent
            var order = new System.Collections.Generic.List<string>();

            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) =>
            {
                order.Add("A-start");
                _dispatcher.Dispatch(new TestEventB());
                order.Add("A-end");
            });

            _dispatcher.Subscribe<TestEventB>((ref TestEventB e) =>
            {
                order.Add("B-start");
                _dispatcher.Dispatch(new TestEvent());
                order.Add("B-end");
            });

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                order.Add("Test");
            });

            _dispatcher.Dispatch(new TestEventA());

            Assert.AreEqual(5, order.Count);
            Assert.AreEqual("A-start", order[0]);
            Assert.AreEqual("B-start", order[1]);
            Assert.AreEqual("Test", order[2]);
            Assert.AreEqual("B-end", order[3]);
            Assert.AreEqual("A-end", order[4]);
        }

        [Test]
        public void Once_With_Nested_Dispatch()
        {
            // Once订阅在套娃场景下的正确性
            int callCount = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
                if (e.Value == 0)
                {
                    _dispatcher.Dispatch(new TestEvent { Value = 1 });
                }
            }).Once();

            _dispatcher.Dispatch(new TestEvent { Value = 0 });

            // Once应该只触发一次
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Post_During_Dispatch()
        {
            // 在事件处理中 Post 事件
            int dispatchCount = 0;
            int postCount = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                dispatchCount++;
                _dispatcher.Post(new TestEventA());
            });

            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) =>
            {
                postCount++;
            });

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(1, dispatchCount);
            Assert.AreEqual(0, postCount); // Post 还没执行

            _dispatcher.Flush();
            Assert.AreEqual(1, postCount);
        }

        [Test]
        public void Interceptor_Subscribe_During_Dispatch()
        {
            // 在拦截器中订阅事件
            int handlerCount = 0;
            int newHandlerCount = 0;

            _dispatcher.AddInterceptor<TestEvent>((ref InterceptorContext<TestEvent> ctx) =>
            {
                _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
                {
                    newHandlerCount++;
                });
                return InterceptResult.Continue;
            });

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                handlerCount++;
            });

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(1, handlerCount);
            Assert.AreEqual(0, newHandlerCount); // 新订阅者不应在当前分发中被调用

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(2, handlerCount);
            Assert.AreEqual(1, newHandlerCount);
        }

        [Test]
        public void Circular_Event_With_Depth_Limit()
        {
            // 循环触发：A触发B，B触发A（需要深度限制）
            int countA = 0;
            int countB = 0;
            const int maxDepth = 5;

            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) =>
            {
                countA++;
                if (countA < maxDepth)
                {
                    _dispatcher.Dispatch(new TestEventB());
                }
            });

            _dispatcher.Subscribe<TestEventB>((ref TestEventB e) =>
            {
                countB++;
                if (countB < maxDepth)
                {
                    _dispatcher.Dispatch(new TestEventA());
                }
            });

            _dispatcher.Dispatch(new TestEventA());

            Assert.AreEqual(maxDepth, countA);
            Assert.AreEqual(maxDepth - 1, countB);
        }

        [Test]
        public void Subscribe_Different_Type_During_Dispatch()
        {
            // 在事件处理中订阅其他类型事件
            int countA = 0;
            int countB = 0;

            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) =>
            {
                countA++;
                _dispatcher.Subscribe<TestEventB>((ref TestEventB e2) =>
                {
                    countB++;
                });
            });

            _dispatcher.Dispatch(new TestEventA());
            Assert.AreEqual(1, countA);
            Assert.AreEqual(0, countB);

            _dispatcher.Dispatch(new TestEventB());
            Assert.AreEqual(1, countB);
        }

        [Test]
        public void SetPriority_During_Dispatch_TakesEffect_NextDispatch()
        {
            var order = new System.Collections.Generic.List<int>();
            SubscriptionResult subA = default;
            SubscriptionResult subB = default;

            subA = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                order.Add(1);
                subB.Priority(10);
            });

            subB = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                order.Add(2);
            });

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, order[0]);
            Assert.AreEqual(2, order[1]);

            order.Clear();
            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(2, order[0]);
            Assert.AreEqual(1, order[1]);
        }

        [Test]
        public void SetOnce_During_Dispatch_TakesEffect_NextDispatch()
        {
            int callCount = 0;
            SubscriptionResult target = default;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                target.Once();
            });

            target = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            });

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(2, callCount);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(2, callCount);
        }

        [Test]
        public void Subscribe_Then_Unsubscribe_During_Dispatch_DoesNotPersist()
        {
            int lateCount = 0;
            SubscriptionResult lateSub = default;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                lateSub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e2) =>
                {
                    lateCount++;
                });
                lateSub.Unsubscribe();
            });

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(0, lateCount);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(0, lateCount);
        }
    }
}
