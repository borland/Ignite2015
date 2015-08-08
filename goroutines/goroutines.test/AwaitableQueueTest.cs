using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace goroutines
{
    [TestClass]
    public class TestAwaitableQueue
    {
        [TestMethod]
        public async Task TestPositiveQueueing()
        {
            var q = new AwaitableQueue<int>();
            q.Enqueue(1);
            q.Enqueue(2);
            q.Enqueue(3);

            Assert.AreEqual(3, q.Count);
            Assert.AreEqual(0, q.PromisedCount);

            Assert.AreEqual(1, await q.Dequeue());
            Assert.AreEqual(2, await q.Dequeue());
            Assert.AreEqual(3, await q.Dequeue());

            Assert.AreEqual(0, q.Count);
            Assert.AreEqual(0, q.PromisedCount);
        }

        [TestMethod]
        public void TestNegativeQueueing()
        {
            var q = new AwaitableQueue<int>();
            var hits = new List<int>();
            q.Dequeue().ContinueWith(t => {
                hits.Add(t.Result);
            }, TaskContinuationOptions.ExecuteSynchronously);
            q.Dequeue().ContinueWith(t => {
                hits.Add(t.Result);
            }, TaskContinuationOptions.ExecuteSynchronously);
            q.Dequeue().ContinueWith(t => {
                hits.Add(t.Result);
            }, TaskContinuationOptions.ExecuteSynchronously);

            Assert.AreEqual(0, q.Count);
            Assert.AreEqual(3, q.PromisedCount);

            q.Enqueue(1);
            q.Enqueue(2);
            q.Enqueue(3);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, hits);

            Assert.AreEqual(0, q.Count);
            Assert.AreEqual(0, q.PromisedCount);
        }
    }
}
