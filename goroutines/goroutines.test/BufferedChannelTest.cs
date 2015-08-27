using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace goroutines.test
{
    [TestClass]
    public class BufferedChannelTest
    {
        [TestMethod]
        public void SendAndReceive_Buffer_1()
        {
            var c = new BufferedChannel<int>(bufferSize: 1);
            c.Send(1).Wait();
            Assert.AreEqual(1, c.Receive().Result);
        }

        [TestMethod]
        public void SendAndReceive_Buffer_5()
        {
            var c = new BufferedChannel<int>(bufferSize: 5);
            c.Send(1).Wait();
            c.Send(2).Wait();
            c.Send(3).Wait();
            c.Send(4).Wait();
            c.Send(5).Wait();
            Assert.AreEqual(1, c.Receive().Result);
            Assert.AreEqual(2, c.Receive().Result);
            Assert.AreEqual(3, c.Receive().Result);
            Assert.AreEqual(4, c.Receive().Result);
            Assert.AreEqual(5, c.Receive().Result);
        }

        [TestMethod]
        public void ReceiveOnClosedChannelEmptiesBufferFirst()
        {
            var ci = new BufferedChannel<int>(2);
            ci.Send(1).Wait();
            ci.Send(2).Wait();
            ci.Close();
            Assert.AreEqual(new ReceivedValue<int>(1, true), ci.ReceiveEx().Result);
            Assert.AreEqual(new ReceivedValue<int>(2, true), ci.ReceiveEx().Result);
            Assert.AreEqual(new ReceivedValue<int>(0, false), ci.ReceiveEx().Result);
        }

        [TestMethod]
        public async Task ReceiveBeforeSend()
        {
            var c = new BufferedChannel<int>(5);
            var hits = new List<int>();
            var t = Task.Run(async () => hits.Add(await c.Receive()));

            await c.Send(1);
            await t; // wait for receive to complete

            CollectionAssert.AreEqual(new[] { 1 }, hits);
        }
        
        [TestMethod]
        public void ReceiveBeforeSendWithOverflow()
        {
            var c = new BufferedChannel<int>(2);

            var hits = new List<int>();
            var tasks = new List<Task>(5);
            for (int i = 0; i < 5; i++) {
                tasks.Add(Task.Run(() => hits.Add(c.Receive().Result)));
            }

            Assert.AreEqual(0, hits.Count);
            c.Send(1).Wait();
            // the first task to call Receive will complete first, but we can't control
            // the task launch order so we don't know which task that might be
            tasks.Remove(Task.WhenAny(tasks).Result);
            CollectionAssert.AreEqual(new[] { 1 }, hits);

            c.Send(2).Wait();
            tasks.Remove(Task.WhenAny(tasks).Result);
            CollectionAssert.AreEqual(new[] { 1, 2 }, hits);

            c.Send(3).Wait();
            tasks.Remove(Task.WhenAny(tasks).Result);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, hits);

            c.Send(4).Wait();
            tasks.Remove(Task.WhenAny(tasks).Result);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, hits);

            c.Send(5).Wait();
            tasks.Remove(Task.WhenAny(tasks).Result);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, hits);

            Assert.AreEqual(0, tasks.Count);
        }
    }
}
