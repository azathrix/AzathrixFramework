using System.Collections;
using Azathrix.Framework.Events.Components;
using Azathrix.Framework.Events.Extensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Azathrix.EventDispatcher.Tests.Tests.PlayMode
{
    /// <summary>
    /// Unity组件测试（需要Unity运行时）
    /// </summary>
    public class UnityComponentTests
    {
        private Framework.Events.Core.EventDispatcher _dispatcher;
        private GameObject _testObject;

        [SetUp]
        public void Setup()
        {
            _dispatcher = new Framework.Events.Core.EventDispatcher();
            _testObject = new GameObject("TestObject");
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher?.Dispose();
            if (_testObject != null)
                Object.DestroyImmediate(_testObject);
        }

        private static IEnumerator DestroyAndWait(GameObject go)
        {
            if (go == null)
                yield break;

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AddTo_GameObject_Works()
        {
            int callCount = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            }).AddTo(_testObject);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);

            // 销毁 GameObject 应该自动取消订阅
            yield return DestroyAndWait(_testObject);
            _testObject = null;

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount); // 不应增加
        }

        [UnityTest]
        public IEnumerator AddTo_Component_Works()
        {
            int callCount = 0;
            var component = _testObject.AddComponent<BoxCollider>();

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            }).AddTo(component);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);

            yield return DestroyAndWait(_testObject);
            _testObject = null;

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);
        }

        [UnityTest]
        public IEnumerator AddTo_Multiple_Subscriptions()
        {
            int count1 = 0, count2 = 0, count3 = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => count1++).AddTo(_testObject);
            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) => count2++).AddTo(_testObject);
            _dispatcher.Subscribe<TestEventB>((ref TestEventB e) => count3++).AddTo(_testObject);

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEventA());
            _dispatcher.Dispatch(new TestEventB());

            Assert.AreEqual(1, count1);
            Assert.AreEqual(1, count2);
            Assert.AreEqual(1, count3);

            yield return DestroyAndWait(_testObject);
            _testObject = null;

            _dispatcher.Dispatch(new TestEvent());
            _dispatcher.Dispatch(new TestEventA());
            _dispatcher.Dispatch(new TestEventB());

            Assert.AreEqual(1, count1);
            Assert.AreEqual(1, count2);
            Assert.AreEqual(1, count3);
        }

        [Test]
        public void AddTo_Null_GameObject_NoError()
        {
            int callCount = 0;

            // 传入 null 不应报错
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            }).AddTo((GameObject)null);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount); // 订阅仍然有效
        }

        [Test]
        public void AddTo_Null_Component_NoError()
        {
            int callCount = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            }).AddTo((Component)null);

            _dispatcher.Dispatch(new TestEvent());
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void SubscriptionDestroyer_Component_Added()
        {
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { }).AddTo(_testObject);

            var destroyer = _testObject.GetComponent<SubscriptionDestroyer>();
            Assert.IsNotNull(destroyer);
        }

        [Test]
        public void SubscriptionDestroyer_Reused()
        {
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { }).AddTo(_testObject);
            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) => { }).AddTo(_testObject);

            var destroyers = _testObject.GetComponents<SubscriptionDestroyer>();
            Assert.AreEqual(1, destroyers.Length); // 应该只有一个
        }

        [UnityTest]
        public IEnumerator MessageSubscription_AddTo_GameObject()
        {
            int callCount = 0;

            _dispatcher.SubscribeMessage<string>("test.msg", data =>
            {
                callCount++;
            }).AddTo(_testObject);

            _dispatcher.DispatchMessage("test.msg", "hello");
            Assert.AreEqual(1, callCount);

            yield return DestroyAndWait(_testObject);
            _testObject = null;

            _dispatcher.DispatchMessage("test.msg", "hello");
            Assert.AreEqual(1, callCount);
        }
    }
}
