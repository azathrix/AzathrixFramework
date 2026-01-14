using System;
using Azathrix.Framework.Events.Serialization;
using NUnit.Framework;

namespace Azathrix.EventDispatcher.Tests.Tests.Editor
{
    /// <summary>
    /// 消息事件测试
    /// </summary>
    public class MessageTests
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

        public class PlayerData
        {
            public string Name;
            public int Score;
        }

        private sealed class LargeSerializer : IMessageSerializer
        {
            public int Serialize<T>(T data, Span<byte> buffer)
            {
                const int required = 8192;
                if (buffer.Length < required)
                    throw new InvalidOperationException("Buffer too small for serialized data.");
                buffer.Slice(0, required).Clear();
                return required;
            }

            public T Deserialize<T>(ReadOnlySpan<byte> data)
            {
                return default;
            }
        }

        [Test]
        public void SubscribeMessage_And_DispatchMessage_Works()
        {
            PlayerData received = null;

            _dispatcher.SubscribeMessage<PlayerData>("player.update", data =>
            {
                received = data;
            });

            _dispatcher.DispatchMessage("player.update", new PlayerData
            {
                Name = "Test",
                Score = 100
            });

            Assert.IsNotNull(received);
            Assert.AreEqual("Test", received.Name);
            Assert.AreEqual(100, received.Score);
        }

        [Test]
        public void Message_Unsubscribe_Works()
        {
            int callCount = 0;

            var sub = _dispatcher.SubscribeMessage<PlayerData>("player.update", data =>
            {
                callCount++;
            });

            _dispatcher.DispatchMessage("player.update", new PlayerData());
            Assert.AreEqual(1, callCount);

            sub.Unsubscribe();

            _dispatcher.DispatchMessage("player.update", new PlayerData());
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Message_Multiple_Subscribers()
        {
            int sum = 0;

            _dispatcher.SubscribeMessage<PlayerData>("player.update", data => sum += 1);
            _dispatcher.SubscribeMessage<PlayerData>("player.update", data => sum += 10);
            _dispatcher.SubscribeMessage<PlayerData>("player.update", data => sum += 100);

            _dispatcher.DispatchMessage("player.update", new PlayerData());

            Assert.AreEqual(111, sum);
        }

        [Test]
        public void Message_Different_Ids_Independent()
        {
            int countA = 0;
            int countB = 0;

            _dispatcher.SubscribeMessage<PlayerData>("channel.a", data => countA++);
            _dispatcher.SubscribeMessage<PlayerData>("channel.b", data => countB++);

            _dispatcher.DispatchMessage("channel.a", new PlayerData());

            Assert.AreEqual(1, countA);
            Assert.AreEqual(0, countB);
        }

        [Test]
        public void PostMessage_Works()
        {
            int callCount = 0;

            _dispatcher.SubscribeMessage<PlayerData>("player.update", data =>
            {
                callCount++;
            });

            _dispatcher.PostMessage("player.update", new PlayerData());

            Assert.AreEqual(0, callCount); // 还没执行

            _dispatcher.FlushMessages();

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Message_Priority_Works()
        {
            var order = new System.Collections.Generic.List<int>();

            _dispatcher.SubscribeMessage<PlayerData>("player.update", data => order.Add(1), 1);
            _dispatcher.SubscribeMessage<PlayerData>("player.update", data => order.Add(3), 3);
            _dispatcher.SubscribeMessage<PlayerData>("player.update", data => order.Add(2), 2);

            _dispatcher.DispatchMessage("player.update", new PlayerData());

            Assert.AreEqual(3, order[0]);
            Assert.AreEqual(2, order[1]);
            Assert.AreEqual(1, order[2]);
        }

        [Test]
        public void ClearPendingMessages_Works()
        {
            int callCount = 0;

            _dispatcher.SubscribeMessage<PlayerData>("player.update", data =>
            {
                callCount++;
            });

            _dispatcher.PostMessage("player.update", new PlayerData());
            _dispatcher.PostMessage("player.update", new PlayerData());

            _dispatcher.ClearPendingMessages();
            _dispatcher.FlushMessages();

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void ClearMessageSubscriptions_Works()
        {
            int callCount = 0;

            _dispatcher.SubscribeMessage<PlayerData>("player.update", data => callCount++);
            _dispatcher.SubscribeMessage<PlayerData>("player.update", data => callCount++);

            _dispatcher.DispatchMessage("player.update", new PlayerData());
            Assert.AreEqual(2, callCount);

            _dispatcher.ClearMessageSubscriptions("player.update");

            _dispatcher.DispatchMessage("player.update", new PlayerData());
            Assert.AreEqual(2, callCount); // 不应增加
        }

        [Test]
        public void ClearAllMessageSubscriptions_Works()
        {
            int countA = 0, countB = 0;

            _dispatcher.SubscribeMessage<PlayerData>("channel.a", data => countA++);
            _dispatcher.SubscribeMessage<PlayerData>("channel.b", data => countB++);

            _dispatcher.DispatchMessage("channel.a", new PlayerData());
            _dispatcher.DispatchMessage("channel.b", new PlayerData());
            Assert.AreEqual(1, countA);
            Assert.AreEqual(1, countB);

            _dispatcher.ClearAllMessageSubscriptions();

            _dispatcher.DispatchMessage("channel.a", new PlayerData());
            _dispatcher.DispatchMessage("channel.b", new PlayerData());
            Assert.AreEqual(1, countA);
            Assert.AreEqual(1, countB);
        }

        [Test]
        public void MessageSubscriptionResult_IsValid()
        {
            var sub = _dispatcher.SubscribeMessage<PlayerData>("test", data => { });
            Assert.IsTrue(sub.IsValid);
            Assert.AreNotEqual(0u, sub.Id);
            Assert.AreEqual("test", sub.MessageId);
        }

        [Test]
        public void Message_SameId_DifferentType_Throws()
        {
            _dispatcher.SubscribeMessage<int>("type.conflict", _ => { });

            Assert.Throws<InvalidOperationException>(() =>
            {
                _dispatcher.SubscribeMessage<string>("type.conflict", _ => { });
            });
        }

        [Test]
        public void DispatchMessageSerialized_BufferTooSmall_Throws()
        {
            _dispatcher.SetMessageSerializer(new LargeSerializer());
            _dispatcher.SubscribeMessage<TestEvent>("msg.large", _ => { });

            Assert.Throws<InvalidOperationException>(() =>
            {
                _dispatcher.DispatchMessageSerialized("msg.large", new TestEvent { Value = 1 });
            });
        }
    }
}
