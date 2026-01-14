using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// 线程安全测试
    /// </summary>
    public class ThreadSafetyTests
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
        public void Post_From_Multiple_Threads()
        {
            int callCount = 0;
            var lockObj = new object();

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                lock (lockObj) callCount++;
            });

            const int threadCount = 10;
            const int postsPerThread = 100;
            var tasks = new Task[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < postsPerThread; i++)
                    {
                        _dispatcher.Post(new TestEvent());
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.AreEqual(threadCount * postsPerThread, _dispatcher.PendingPostCount);

            _dispatcher.Flush();

            Assert.AreEqual(threadCount * postsPerThread, callCount);
            Assert.AreEqual(0, _dispatcher.PendingPostCount);
        }

        [Test]
        public void PostMessage_From_Multiple_Threads()
        {
            int callCount = 0;
            var lockObj = new object();

            _dispatcher.SubscribeMessage<string>("test.msg", data =>
            {
                lock (lockObj) callCount++;
            });

            const int threadCount = 10;
            const int postsPerThread = 100;
            var tasks = new Task[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < postsPerThread; i++)
                    {
                        _dispatcher.PostMessage("test.msg", "data");
                    }
                });
            }

            Task.WaitAll(tasks);

            _dispatcher.FlushMessages();

            Assert.AreEqual(threadCount * postsPerThread, callCount);
        }

        [Test]
        public void Concurrent_Post_And_Flush()
        {
            int callCount = 0;
            var lockObj = new object();
            var running = true;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                lock (lockObj) callCount++;
            });

            // 后台线程持续 Post
            var postTask = Task.Run(() =>
            {
                while (running)
                {
                    _dispatcher.Post(new TestEvent());
                    Thread.Sleep(1);
                }
            });

            // 主线程多次 Flush
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(10);
                _dispatcher.Flush();
            }

            running = false;
            postTask.Wait();

            // 最后一次 Flush
            _dispatcher.Flush();

            Assert.Greater(callCount, 0);
        }

        [Test]
        public void Post_With_Different_Event_Types_From_Threads()
        {
            int countA = 0, countB = 0, countTest = 0;
            var lockObj = new object();

            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) =>
            {
                lock (lockObj) countA++;
            });
            _dispatcher.Subscribe<TestEventB>((ref TestEventB e) =>
            {
                lock (lockObj) countB++;
            });
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                lock (lockObj) countTest++;
            });

            var tasks = new Task[3];

            tasks[0] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    _dispatcher.Post(new TestEventA());
            });

            tasks[1] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    _dispatcher.Post(new TestEventB());
            });

            tasks[2] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    _dispatcher.Post(new TestEvent());
            });

            Task.WaitAll(tasks);
            _dispatcher.Flush();

            Assert.AreEqual(100, countA);
            Assert.AreEqual(100, countB);
            Assert.AreEqual(100, countTest);
        }
    }
}
