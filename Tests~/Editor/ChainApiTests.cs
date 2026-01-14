using NUnit.Framework;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// 链式 API 测试（Skip、Where、Sticky、Priority、Once）
    /// 注：Delay、Throttle、Debounce、Timeout 需要时间相关测试，放在 PlayMode
    /// </summary>
    public class ChainApiTests
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

        #region Skip Tests

        [Test]
        public void Skip_Skips_First_N_Events()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Skip(3);

            _dispatcher.Dispatch(new TestEvent()); // 跳过
            _dispatcher.Dispatch(new TestEvent()); // 跳过
            _dispatcher.Dispatch(new TestEvent()); // 跳过
            Assert.AreEqual(0, callCount);

            _dispatcher.Dispatch(new TestEvent()); // 第4个，开始处理
            Assert.AreEqual(1, callCount);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(2, callCount);
        }

        [Test]
        public void Skip_Zero_NoEffect()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Skip(0);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Skip_Negative_TreatedAsZero()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Skip(-5);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);
        }

        #endregion

        #region Where Tests

        [Test]
        public void Where_Filters_Events()
        {
            int sum = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum += e.Value)
                .Where((ref TestEvent e) => e.Value > 5);

            _dispatcher.Dispatch(new TestEvent { Value = 3 }); // 过滤掉
            _dispatcher.Dispatch(new TestEvent { Value = 10 }); // 通过
            _dispatcher.Dispatch(new TestEvent { Value = 2 }); // 过滤掉
            _dispatcher.Dispatch(new TestEvent { Value = 7 }); // 通过

            Assert.AreEqual(17, sum); // 10 + 7
        }

        [Test]
        public void Where_Null_Filter_NoEffect()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Where(null);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Where_Can_Modify_Event()
        {
            int received = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => received = e.Value)
                .Where((ref TestEvent e) =>
                {
                    e.Value *= 2; // 在过滤器中修改
                    return true;
                });

            _dispatcher.Dispatch(new TestEvent { Value = 5 });
            Assert.AreEqual(10, received);
        }

        #endregion

        #region Sticky Tests

        [Test]
        public void Sticky_Receives_Last_Value_Immediately()
        {
            _dispatcher.DispatchSticky(new TestEvent { Value = 42 });

            int received = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => received = e.Value).Sticky();

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Sticky_No_Value_NoEffect()
        {
            int received = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => received = e.Value).Sticky();

            Assert.AreEqual(0, received);
        }

        [Test]
        public void Sticky_Still_Receives_New_Events()
        {
            _dispatcher.DispatchSticky(new TestEvent { Value = 42 });

            int callCount = 0;
            int lastValue = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
                lastValue = e.Value;
            }).Sticky();

            Assert.AreEqual(1, callCount); // Sticky 触发一次
            Assert.AreEqual(42, lastValue);

            _dispatcher.Dispatch(new TestEvent { Value = 100 });
            Assert.AreEqual(2, callCount);
            Assert.AreEqual(100, lastValue);
        }

        #endregion

        #region Chain Combination Tests

        [Test]
        public void Skip_And_Where_Combined()
        {
            int sum = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum += e.Value)
                .Skip(2)
                .Where((ref TestEvent e) => e.Value > 5);

            _dispatcher.Dispatch(new TestEvent { Value = 10 }); // 跳过
            _dispatcher.Dispatch(new TestEvent { Value = 20 }); // 跳过
            _dispatcher.Dispatch(new TestEvent { Value = 3 }); // 过滤掉
            _dispatcher.Dispatch(new TestEvent { Value = 8 }); // 通过

            Assert.AreEqual(8, sum);
        }

        [Test]
        public void Priority_With_Where()
        {
            var order = new System.Collections.Generic.List<int>();

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => order.Add(1))
                .Where((ref TestEvent e) => e.Value > 0)
                .Priority(1);

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => order.Add(2))
                .Priority(2);

            _dispatcher.Dispatch(new TestEvent { Value = 5 });

            Assert.AreEqual(2, order[0]); // 优先级高的先执行
            Assert.AreEqual(1, order[1]);
        }

        [Test]
        public void Once_With_Skip()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                .Skip(2)
                .Once();

            _dispatcher.Dispatch(new TestEvent()); // 跳过
            _dispatcher.Dispatch(new TestEvent()); // 跳过
            _dispatcher.Dispatch(new TestEvent()); // 处理，然后 Once 取消
            _dispatcher.Dispatch(new TestEvent()); // 已取消

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Once_With_Where()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                .Where((ref TestEvent e) => e.Value > 5)
                .Once();

            _dispatcher.Dispatch(new TestEvent { Value = 3 }); // 过滤掉，Once 不触发
            _dispatcher.Dispatch(new TestEvent { Value = 10 }); // 通过，Once 触发并取消
            _dispatcher.Dispatch(new TestEvent { Value = 20 }); // 已取消

            Assert.AreEqual(1, callCount);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Unsubscribe_After_Skip_Works()
        {
            int callCount = 0;
            var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Skip(2);

            _dispatcher.Dispatch(new TestEvent()); // 跳过
            sub.Unsubscribe();

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Chain_Order_Does_Not_Matter()
        {
            int sum1 = 0, sum2 = 0;

            // 顺序1: Skip -> Where -> Priority
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum1 += e.Value)
                .Skip(1)
                .Where((ref TestEvent e) => e.Value > 5)
                .Priority(1);

            // 顺序2: Priority -> Where -> Skip
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum2 += e.Value)
                .Priority(1)
                .Where((ref TestEvent e) => e.Value > 5)
                .Skip(1);

            _dispatcher.Dispatch(new TestEvent { Value = 10 }); // 跳过
            _dispatcher.Dispatch(new TestEvent { Value = 3 }); // 过滤
            _dispatcher.Dispatch(new TestEvent { Value = 8 }); // 通过

            Assert.AreEqual(sum1, sum2);
        }

        [Test]
        public void AsResult_Returns_Valid_Result()
        {
            var builder = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });
            var result = builder.AsResult();

            Assert.AreEqual(builder.Id, result.Id);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Implicit_Conversion_To_Result()
        {
            Framework.Events.Results.SubscriptionResult result =
                _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Multiple_Skip_Calls_Uses_Last_Value()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                .Skip(5)
                .Skip(2); // 后面的覆盖前面的

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(0, callCount);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Multiple_Where_Calls_Uses_Last_Filter()
        {
            int sum = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum += e.Value)
                .Where((ref TestEvent e) => e.Value > 10)
                .Where((ref TestEvent e) => e.Value > 5); // 后面的覆盖前面的

            _dispatcher.Dispatch(new TestEvent { Value = 7 });
            Assert.AreEqual(7, sum); // 通过 >5 过滤
        }

        [Test]
        public void Dispose_After_Unsubscribe_NoError()
        {
            var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });
            sub.Unsubscribe();

            Assert.DoesNotThrow(() => sub.Dispose());
            Assert.DoesNotThrow(() => sub.Unsubscribe());
        }

        [Test]
        public void IsValid_After_Unsubscribe()
        {
            var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });
            Assert.IsTrue(sub.IsValid);

            sub.Unsubscribe();
            Assert.IsFalse(sub.IsValid);
        }

        [Test]
        public void Dispatch_After_Dispatcher_Dispose_NoError()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++);
            _dispatcher.Dispose();

            Assert.DoesNotThrow(() => _dispatcher.Dispatch(new TestEvent()));
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Sticky_With_Where_Bypasses_Filter()
        {
            // Sticky 直接调用原始处理器，不经过 Where 过滤
            _dispatcher.DispatchSticky(new TestEvent { Value = 3 });

            int received = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => received = e.Value)
                .Where((ref TestEvent e) => e.Value > 5)
                .Sticky();

            // Sticky 绕过 Where，直接触发
            Assert.AreEqual(3, received);
        }

        [Test]
        public void Sticky_With_Skip_Bypasses_Skip()
        {
            // Sticky 直接调用原始处理器，不经过 Skip
            _dispatcher.DispatchSticky(new TestEvent { Value = 42 });

            int received = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => received = e.Value)
                .Skip(5)
                .Sticky();

            // Sticky 绕过 Skip，直接触发
            Assert.AreEqual(42, received);
        }

        [Test]
        public void Sticky_With_Once_Does_Not_Unsubscribe()
        {
            // Sticky 直接调用原始处理器，不触发 Once 取消
            _dispatcher.DispatchSticky(new TestEvent { Value = 42 });

            int callCount = 0;
            var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                .Once()
                .Sticky();

            Assert.AreEqual(1, callCount); // Sticky 触发
            Assert.IsTrue(sub.IsValid); // 订阅仍然有效

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(2, callCount); // 正常事件触发 Once
            Assert.IsFalse(sub.IsValid); // Once 取消订阅
        }

        [Test]
        public void Multiple_Priority_Calls_Uses_Last_Value()
        {
            var order = new System.Collections.Generic.List<int>();

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => order.Add(1))
                .Priority(100)
                .Priority(1); // 后面的覆盖前面的

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => order.Add(2))
                .Priority(2);

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(2, order[0]); // 优先级2先执行
            Assert.AreEqual(1, order[1]); // 优先级1后执行
        }

        [Test]
        public void AddTo_Null_GameObject_NoError()
        {
            int callCount = 0;
            Assert.DoesNotThrow(() =>
            {
                _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                    .AddTo((UnityEngine.GameObject)null);
            });

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void AddTo_Null_Collector_NoError()
        {
            int callCount = 0;
            Assert.DoesNotThrow(() =>
            {
                _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                    .AddTo((Framework.Events.Results.SubscriptionCollector)null);
            });

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);
        }

        #endregion
    }
}
