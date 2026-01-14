using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Azathrix.EventDispatcher.Tests.Tests.PlayMode
{
    /// <summary>
    /// 时间相关链式 API 测试（Delay、Throttle、Debounce、Timeout）
    /// </summary>
    public class TimeBasedApiTests
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

        #region Delay Tests

        [UnityTest]
        public IEnumerator Delay_Delays_Event_Processing()
        {
            int received = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => received = e.Value).Delay(100);

            _dispatcher.Dispatch(new TestEvent { Value = 42 });
            Assert.AreEqual(0, received);

            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(42, received);
        }

        [UnityTest]
        public IEnumerator Delay_Multiple_Events_All_Delayed()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Delay(50);

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(0, callCount);

            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(3, callCount);
        }

        [Test]
        public void Delay_Zero_NoDelay()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Delay(0);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);
        }

        #endregion

        #region Throttle Tests

        [UnityTest]
        public IEnumerator Throttle_Limits_Event_Rate()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Throttle(100);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);

            yield return new WaitForSeconds(0.15f);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(2, callCount);
        }

        [Test]
        public void Throttle_Zero_NoThrottle()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Throttle(0);

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(3, callCount);
        }

        #endregion

        #region Debounce Tests

        [UnityTest]
        public IEnumerator Debounce_Waits_For_Quiet_Period()
        {
            int received = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => received = e.Value).Debounce(100);

            _dispatcher.Dispatch(new TestEvent { Value = 1 });
            _dispatcher.Dispatch(new TestEvent { Value = 2 });
            _dispatcher.Dispatch(new TestEvent { Value = 3 });
            Assert.AreEqual(0, received);

            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(3, received);
        }

        [UnityTest]
        public IEnumerator Debounce_Resets_On_New_Event()
        {
            int received = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => received = e.Value).Debounce(100);

            _dispatcher.Dispatch(new TestEvent { Value = 1 });
            yield return new WaitForSeconds(0.05f);

            _dispatcher.Dispatch(new TestEvent { Value = 2 }); // 重置计时器
            yield return new WaitForSeconds(0.05f);

            Assert.AreEqual(0, received); // 还没到

            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(2, received);
        }

        #endregion

        #region Timeout Tests

        [UnityTest]
        public IEnumerator Timeout_Auto_Unsubscribes()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Timeout(100);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);

            yield return new WaitForSeconds(0.15f);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);
        }

        [UnityTest]
        public IEnumerator Timeout_Works_Before_Expiry()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++).Timeout(200);

            _dispatcher.Dispatch(new TestEvent());
            yield return new WaitForSeconds(0.05f);
            _dispatcher.Dispatch(new TestEvent());
            yield return new WaitForSeconds(0.05f);
            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(3, callCount);
        }

        #endregion

        #region Combined Tests

        [UnityTest]
        public IEnumerator Throttle_And_Where_Combined()
        {
            int sum = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => sum += e.Value)
                .Where((ref TestEvent e) => e.Value > 5)
                .Throttle(100);

            _dispatcher.Dispatch(new TestEvent { Value = 10 });
            _dispatcher.Dispatch(new TestEvent { Value = 3 }); // 过滤
            _dispatcher.Dispatch(new TestEvent { Value = 20 }); // 节流
            Assert.AreEqual(10, sum);

            yield return new WaitForSeconds(0.15f);

            _dispatcher.Dispatch(new TestEvent { Value = 15 });
            Assert.AreEqual(25, sum);
        }

        [UnityTest]
        public IEnumerator Delay_With_Skip()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                .Skip(2)
                .Delay(50);

            _dispatcher.Dispatch(new TestEvent()); // 跳过
            _dispatcher.Dispatch(new TestEvent()); // 跳过
            _dispatcher.Dispatch(new TestEvent()); // 延迟处理

            Assert.AreEqual(0, callCount);

            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(1, callCount);
        }

        [UnityTest]
        public IEnumerator Delay_With_Once()
        {
            int callCount = 0;
            var sub = _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                .Delay(50)
                .Once();

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent()); // 应该被忽略，因为 Once 已经在延迟前取消

            yield return new WaitForSeconds(0.1f);
            // 由于当前实现 Delay 绕过 Once，可能会有2次调用
            // 这个测试用于验证当前行为
            Assert.AreEqual(1, callCount);
        }

        [UnityTest]
        public IEnumerator Throttle_With_Once()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                .Throttle(100)
                .Once();

            _dispatcher.Dispatch(new TestEvent()); // 通过节流，触发 Once
            _dispatcher.Dispatch(new TestEvent()); // 被节流
            _dispatcher.Dispatch(new TestEvent()); // 被节流

            Assert.AreEqual(1, callCount);

            yield return new WaitForSeconds(0.15f);

            _dispatcher.Dispatch(new TestEvent()); // 已经被 Once 取消
            Assert.AreEqual(1, callCount);
        }

        [UnityTest]
        public IEnumerator Debounce_With_Once()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                .Debounce(100)
                .Once();

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(0, callCount);

            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(1, callCount); // 防抖后触发一次

            // 再次分发，应该被 Once 取消
            _dispatcher.Dispatch(new TestEvent());
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(1, callCount);
        }

        [UnityTest]
        public IEnumerator Timeout_With_Once()
        {
            int callCount = 0;
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => callCount++)
                .Timeout(200)
                .Once();

            _dispatcher.Dispatch(new TestEvent()); // Once 触发并取消
            Assert.AreEqual(1, callCount);

            yield return new WaitForSeconds(0.25f);

            _dispatcher.Dispatch(new TestEvent()); // 已被 Once 取消
            Assert.AreEqual(1, callCount);
        }

        #endregion
    }
}
