using NUnit.Framework;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// Post事件测试
    /// </summary>
    public class PostTests
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
        public void Post_Not_Immediate()
        {
            int callCount = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            });

            _dispatcher.Post(new TestEvent());

            Assert.AreEqual(0, callCount); // Post不会立即执行
            Assert.AreEqual(1, _dispatcher.PendingPostCount);
        }

        [Test]
        public void Post_Flush_Works()
        {
            int callCount = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;
            });

            _dispatcher.Post(new TestEvent());
            _dispatcher.Post(new TestEvent());
            _dispatcher.Post(new TestEvent());

            Assert.AreEqual(0, callCount);
            Assert.AreEqual(3, _dispatcher.PendingPostCount);

            _dispatcher.Flush();

            Assert.AreEqual(3, callCount);
            Assert.AreEqual(0, _dispatcher.PendingPostCount);
        }

        [Test]
        public void Post_During_Dispatch()
        {
            int immediateCount = 0;
            int postCount = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                immediateCount++;
                _dispatcher.Post(new TestEventA());
            });

            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) =>
            {
                postCount++;
            });

            _dispatcher.Dispatch(new TestEvent());

            Assert.AreEqual(1, immediateCount);
            Assert.AreEqual(0, postCount); // Post的还没执行

            _dispatcher.Flush();
            Assert.AreEqual(1, postCount);
        }

        [Test]
        public void ClearPendingPosts_Works()
        {
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) => { });

            _dispatcher.Post(new TestEvent());
            _dispatcher.Post(new TestEvent());

            Assert.AreEqual(2, _dispatcher.PendingPostCount);

            _dispatcher.ClearPendingPosts();

            Assert.AreEqual(0, _dispatcher.PendingPostCount);
        }

        [Test]
        public void Post_With_Initializer()
        {
            int receivedValue = 0;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                receivedValue = e.Value;
            });

            _dispatcher.Post<TestEvent>((ref TestEvent e) => e.Value = 123);
            _dispatcher.Flush();

            Assert.AreEqual(123, receivedValue);
        }
    }
}
