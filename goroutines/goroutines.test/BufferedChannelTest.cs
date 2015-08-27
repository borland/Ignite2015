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

    }
}
