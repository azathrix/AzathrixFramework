using System.Diagnostics;
using System.Collections.Generic;
using Azathrix.Framework.Events.Core;
using Azathrix.Framework.Events.Results;
using NUnit.Framework;
using Debug = UnityEngine.Debug;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// 性能测试
    /// </summary>
    public class PerformanceTests
    {
        public struct MakeDamageEvent
        {
            public int Damage;
            public int TargetId;
        }

        public class DamageData
        {
            public int Damage;
            public int TargetId;
        }

        [Test]
        public void Post_Fast_Performance()
        {
            RunPostTest(threadSafe: false, "Fast");
        }

        [Test]
        public void Post_ThreadSafe_Performance()
        {
            RunPostTest(threadSafe: true, "ThreadSafe");
        }

        [Test]
        public void Dispatch_Performance()
        {
            var dispatcher = new Framework.Events.Core.EventDispatcher();
            var subscriptions = new SubscriptionCollector();

            const int subscriberCount = 1000;
            const int dispatchCount = 10000;

            for (int i = 0; i < subscriberCount; i++)
            {
                dispatcher.Subscribe<MakeDamageEvent>((ref MakeDamageEvent evt) =>
                {
                    var _ = evt.Damage + evt.TargetId;
                }).AddTo(subscriptions);
            }

            // 预热
            for (int i = 0; i < 100; i++)
            {
                dispatcher.Dispatch(new MakeDamageEvent { Damage = i, TargetId = i });
            }

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            long gcBefore = System.GC.GetTotalMemory(false);
            int gen0Before = System.GC.CollectionCount(0);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < dispatchCount; i++)
            {
                dispatcher.Dispatch(new MakeDamageEvent { Damage = i, TargetId = i % 100 });
            }

            sw.Stop();

            long gcAfter = System.GC.GetTotalMemory(false);
            int gen0After = System.GC.CollectionCount(0);

            Debug.Log($"=== Dispatch Test: {subscriberCount} subscribers, {dispatchCount} events ===");
            Debug.Log($"Handler Calls: {subscriberCount * dispatchCount:N0}");
            Debug.Log($"Time: {sw.ElapsedMilliseconds}ms");
            Debug.Log($"GC: {(gcAfter - gcBefore) / 1024f:F2} KB | Gen0: {gen0After - gen0Before}");

            subscriptions.Dispose();
            dispatcher.Dispose();

            Assert.Pass();
        }

        [Test]
        public void Message_Dispatch_Performance()
        {
            var dispatcher = new Framework.Events.Core.EventDispatcher();
            var subscriptions = new List<MessageSubscriptionResult>();

            const int subscriberCount = 1000;
            const int dispatchCount = 10000;
            const string messageId = "damage.event";

            for (int i = 0; i < subscriberCount; i++)
            {
                subscriptions.Add(dispatcher.SubscribeMessage<DamageData>(messageId, data =>
                {
                    var _ = data.Damage + data.TargetId;
                }));
            }

            var testData = new DamageData { Damage = 10, TargetId = 1 };

            // 预热
            for (int i = 0; i < 100; i++)
            {
                dispatcher.DispatchMessage(messageId, testData);
            }

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            long gcBefore = System.GC.GetTotalMemory(false);
            int gen0Before = System.GC.CollectionCount(0);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < dispatchCount; i++)
            {
                dispatcher.DispatchMessage(messageId, testData);
            }

            sw.Stop();

            long gcAfter = System.GC.GetTotalMemory(false);
            int gen0After = System.GC.CollectionCount(0);

            Debug.Log($"=== Message Dispatch Test: {subscriberCount} subscribers, {dispatchCount} messages ===");
            Debug.Log($"Handler Calls: {subscriberCount * dispatchCount:N0}");
            Debug.Log($"Time: {sw.ElapsedMilliseconds}ms");
            Debug.Log($"GC: {(gcAfter - gcBefore) / 1024f:F2} KB | Gen0: {gen0After - gen0Before}");

            foreach (var sub in subscriptions) sub.Dispose();
            dispatcher.Dispose();

            Assert.Pass();
        }

        [Test]
        public void PostMessage_ThreadSafe_Performance()
        {
            RunPostMessageTest(threadSafe: true, "ThreadSafe");
        }

        [Test]
        public void PostMessage_Fast_Performance()
        {
            RunPostMessageTest(threadSafe: false, "Fast");
        }

        private void RunPostTest(bool threadSafe, string mode)
        {
            var dispatcher = new Framework.Events.Core.EventDispatcher();
            dispatcher.PostThreadSafe = threadSafe;
            var subscriptions = new SubscriptionCollector();

            const int subscriberCount = 1000;
            const int dispatchCount = 10000;
            const int iterations = 3;

            for (int i = 0; i < subscriberCount; i++)
            {
                dispatcher.Subscribe<MakeDamageEvent>((ref MakeDamageEvent evt) =>
                {
                    var _ = evt.Damage + evt.TargetId;
                }).AddTo(subscriptions);
            }

            Debug.Log($"=== Post Test ({mode}): {subscriberCount} subscribers, {dispatchCount} events ===");

            for (int iter = 1; iter <= iterations; iter++)
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();

                long gcBefore = System.GC.GetTotalMemory(false);
                int gen0Before = System.GC.CollectionCount(0);

                var sw = Stopwatch.StartNew();

                for (int i = 0; i < dispatchCount; i++)
                {
                    dispatcher.Post(new MakeDamageEvent { Damage = i, TargetId = i % 100 });
                }

                sw.Stop();
                long postTime = sw.ElapsedMilliseconds;

                sw.Restart();
                dispatcher.Flush();
                sw.Stop();
                long flushTime = sw.ElapsedMilliseconds;

                long gcAfter = System.GC.GetTotalMemory(false);
                int gen0After = System.GC.CollectionCount(0);

                Debug.Log($"[Run {iter}] Post: {postTime}ms | Flush: {flushTime}ms | Total: {postTime + flushTime}ms | GC: {(gcAfter - gcBefore) / 1024f:F2} KB");
            }

            subscriptions.Dispose();
            dispatcher.Dispose();

            Assert.Pass();
        }

        private void RunPostMessageTest(bool threadSafe, string mode)
        {
            var dispatcher = new Framework.Events.Core.EventDispatcher();
            dispatcher.MsgThreadSafe = threadSafe;
            var subscriptions = new List<MessageSubscriptionResult>();

            const int subscriberCount = 1000;
            const int dispatchCount = 10000;
            const int iterations = 3;
            const string messageId = "damage.event";

            for (int i = 0; i < subscriberCount; i++)
            {
                subscriptions.Add(dispatcher.SubscribeMessage<DamageData>(messageId, data =>
                {
                    var _ = data.Damage + data.TargetId;
                }));
            }

            var testData = new DamageData { Damage = 10, TargetId = 1 };

            Debug.Log($"=== PostMessage Test ({mode}): {subscriberCount} subscribers, {dispatchCount} messages ===");

            for (int iter = 1; iter <= iterations; iter++)
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();

                long gcBefore = System.GC.GetTotalMemory(false);
                int gen0Before = System.GC.CollectionCount(0);

                var sw = Stopwatch.StartNew();

                for (int i = 0; i < dispatchCount; i++)
                {
                    dispatcher.PostMessage(messageId, testData);
                }

                sw.Stop();
                long postTime = sw.ElapsedMilliseconds;

                sw.Restart();
                dispatcher.FlushMessages();
                sw.Stop();
                long flushTime = sw.ElapsedMilliseconds;

                long gcAfter = System.GC.GetTotalMemory(false);
                int gen0After = System.GC.CollectionCount(0);

                Debug.Log($"[Run {iter}] Post: {postTime}ms | Flush: {flushTime}ms | Total: {postTime + flushTime}ms | GC: {(gcAfter - gcBefore) / 1024f:F2} KB");
            }

            foreach (var sub in subscriptions) sub.Dispose();
            dispatcher.Dispose();

            Assert.Pass();
        }
    }
}
