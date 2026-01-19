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
            _dispatcher.PostThreadSafe = true;  // 线程安全测试需要启用
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

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;  // 无需lock，Flush在主线程顺序执行
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

            _dispatcher.Flush();  // 主线程顺序执行所有handler

            Assert.AreEqual(threadCount * postsPerThread, callCount);
            Assert.AreEqual(0, _dispatcher.PendingPostCount);
        }

        [Test]
        public void PostMessage_From_Multiple_Threads()
        {
            int callCount = 0;

            _dispatcher.SubscribeMessage<string>("test.msg", data =>
            {
                callCount++;  // 无需lock，FlushMessages在主线程顺序执行
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

            _dispatcher.FlushMessages();  // 主线程顺序执行所有handler

            Assert.AreEqual(threadCount * postsPerThread, callCount);
        }

        [Test]
        public void Concurrent_Post_And_Flush()
        {
            int callCount = 0;
            var running = true;

            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                callCount++;  // 无需lock，Flush在主线程执行
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

            _dispatcher.Subscribe<TestEventA>((ref TestEventA e) =>
            {
                countA++;  // 无需lock
            });
            _dispatcher.Subscribe<TestEventB>((ref TestEventB e) =>
            {
                countB++;
            });
            _dispatcher.Subscribe<TestEvent>((ref TestEvent e) =>
            {
                countTest++;
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
            _dispatcher.Flush();  // 主线程顺序执行

            Assert.AreEqual(100, countA);
            Assert.AreEqual(100, countB);
            Assert.AreEqual(100, countTest);
        }
    }
}
