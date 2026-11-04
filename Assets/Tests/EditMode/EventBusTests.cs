using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;

namespace EscapeFire.Tests
{
    public class EventBusTests
    {
        private struct PingEvent
        {
            public int Value;
            public PingEvent(int value) { Value = value; }
        }

        [SetUp]
        public void SetUp() => EventBus<PingEvent>.Clear();

        [TearDown]
        public void TearDown() => EventBus<PingEvent>.Clear();

        [Test]
        public void Publish_DeliversPayloadToSubscriber()
        {
            int received = 0;
            EventBus<PingEvent>.Subscribe(e => received = e.Value);

            EventBus<PingEvent>.Publish(new PingEvent(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            void Handler(PingEvent e) => calls++;
            EventBus<PingEvent>.Subscribe(Handler);
            EventBus<PingEvent>.Unsubscribe(Handler);

            EventBus<PingEvent>.Publish(new PingEvent(1));

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Subscribe_SameHandlerTwice_DeliversOnce()
        {
            int calls = 0;
            void Handler(PingEvent e) => calls++;
            EventBus<PingEvent>.Subscribe(Handler);
            EventBus<PingEvent>.Subscribe(Handler);

            EventBus<PingEvent>.Publish(new PingEvent(1));

            Assert.AreEqual(1, calls);
            Assert.AreEqual(1, EventBus<PingEvent>.HandlerCount);
        }

        [Test]
        public void Publish_HandlerThrows_OtherHandlersStillReceive()
        {
            int calls = 0;
            EventBus<PingEvent>.Subscribe(e => throw new System.InvalidOperationException("boom"));
            EventBus<PingEvent>.Subscribe(e => calls++);
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            EventBus<PingEvent>.Publish(new PingEvent(1));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Publish_HandlerUnsubscribesItself_NoErrorAndOthersReceive()
        {
            int otherCalls = 0;
            System.Action<PingEvent> selfRemoving = null;
            selfRemoving = e => EventBus<PingEvent>.Unsubscribe(selfRemoving);
            EventBus<PingEvent>.Subscribe(e => otherCalls++);
            EventBus<PingEvent>.Subscribe(selfRemoving);

            EventBus<PingEvent>.Publish(new PingEvent(1));

            Assert.AreEqual(1, otherCalls);
            Assert.AreEqual(1, EventBus<PingEvent>.HandlerCount);
        }

        [Test]
        public void ResetAll_ClearsEveryTouchedBus()
        {
            EventBus<PingEvent>.Subscribe(e => { });

            EventBusRegistry.ResetAll();

            Assert.AreEqual(0, EventBus<PingEvent>.HandlerCount);
        }
    }
}
