using System;
using NUnit.Framework;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// Query测试
    /// </summary>
    public class QueryTests
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

        public struct DamageCalcEvent
        {
            public int BaseDamage;
        }

        [Test]
        public void QueryFirst_Works()
        {
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) =>
            {
                return e.BaseDamage * 2;
            });

            var result = _dispatcher.QueryFirst<DamageCalcEvent, int>(
                new DamageCalcEvent { BaseDamage = 10 });

            Assert.AreEqual(20, result);
        }

        [Test]
        public void Query_Aggregate_Sum()
        {
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => 10);
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => 20);
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => 30);

            var result = _dispatcher.Query<DamageCalcEvent, int>(
                new DamageCalcEvent(),
                (a, b) => a + b,
                0);

            Assert.AreEqual(60, result);
        }

        [Test]
        public void Query_Aggregate_Max()
        {
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => 10);
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => 50);
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => 30);

            var result = _dispatcher.Query<DamageCalcEvent, int>(
                new DamageCalcEvent(),
                (a, b) => a > b ? a : b,
                int.MinValue);

            Assert.AreEqual(50, result);
        }

        [Test]
        public void Query_Priority_Works()
        {
            var order = new System.Collections.Generic.List<int>();

            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) =>
            {
                order.Add(1);
                return 1;
            }, 1);

            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) =>
            {
                order.Add(3);
                return 3;
            }, 3);

            _dispatcher.Query<DamageCalcEvent, int>(new DamageCalcEvent(), (a, b) => a + b, 0);

            Assert.AreEqual(3, order[0]);
            Assert.AreEqual(1, order[1]);
        }

        [Test]
        public void TryQueryFirst_NoSubscriber_ReturnsFalse()
        {
            var success = _dispatcher.TryQueryFirst<DamageCalcEvent, int>(
                new DamageCalcEvent(),
                out var result);

            Assert.IsFalse(success);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void TryQueryFirst_WithSubscriber_ReturnsTrue()
        {
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => 42);

            var success = _dispatcher.TryQueryFirst<DamageCalcEvent, int>(
                new DamageCalcEvent(),
                out var result);

            Assert.IsTrue(success);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void Query_Unsubscribe_Works()
        {
            var sub = _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => 100);

            var result1 = _dispatcher.QueryFirst<DamageCalcEvent, int>(new DamageCalcEvent());
            Assert.AreEqual(100, result1);

            sub.Unsubscribe();

            var success = _dispatcher.TryQueryFirst<DamageCalcEvent, int>(
                new DamageCalcEvent(),
                out var result2);
            Assert.IsFalse(success);
        }

        [Test]
        public void Query_Event_Modification()
        {
            // 验证 Query 处理器可以修改事件
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) =>
            {
                e.BaseDamage *= 2;
                return e.BaseDamage;
            }, 2);

            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) =>
            {
                return e.BaseDamage + 10;
            }, 1);

            var result = _dispatcher.Query<DamageCalcEvent, int>(
                new DamageCalcEvent { BaseDamage = 5 },
                (a, b) => a + b,
                0);

            // 第一个处理器: 5*2=10, 返回10
            // 第二个处理器: 10+10=20, 返回20
            // 聚合: 10+20=30
            Assert.AreEqual(30, result);
        }

        [Test]
        public void Query_With_Null_Aggregator_Throws()
        {
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => 1);

            Assert.Throws<NullReferenceException>(() =>
            {
                _dispatcher.Query<DamageCalcEvent, int>(new DamageCalcEvent(), null, 0);
            });
        }

        [Test]
        public void Query_Aggregator_Throws_Propagates()
        {
            _dispatcher.SubscribeQuery<DamageCalcEvent, int>((ref DamageCalcEvent e) => 1);

            Assert.Throws<InvalidOperationException>(() =>
            {
                _dispatcher.Query<DamageCalcEvent, int>(
                    new DamageCalcEvent(),
                    (a, b) => throw new InvalidOperationException("Aggregator failed"),
                    0);
            });
        }
    }
}
