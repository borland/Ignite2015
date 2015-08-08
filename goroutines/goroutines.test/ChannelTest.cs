// Copyright(c) 2015 Orion Edwards
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

[TestClass]
public class TestChannel_Basic
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
    public void SendAndReceiveBlocking_1item()
    {
        var c = new Channel<int>();

        var hits = new List<int>();
        Task.Run(() => {
            var x = c.Receive().Result;
            hits.Add(x);
        });

        Assert.AreEqual(0, hits.Count);
        c.Send(1).Wait();
        CollectionAssert.AreEqual(new[] { 1 }, hits);
    }


    [TestMethod]
    public void SendAndReceiveBlocking_Sequence()
    {
        var c = new Channel<int>();

        var hits = new List<int>();
        Task.Run(() => {
            for (int i = 0; i < 5; i++) {
                hits.Add(c.Receive().Result);
            }
        });

        Assert.AreEqual(0, hits.Count);
        c.Send(1).Wait();
        c.Send(2).Wait();
        c.Send(3).Wait();
        c.Send(4).Wait();
        c.Send(5).Wait();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, hits);
    }

    [TestMethod]
    public void SendAndReceiveBlocking_ManyReceivers()
    {
        var c = new Channel<int>();

        var hits = new List<int>();
        for (int i = 0; i < 5; i++) {
            Task.Run(() => {
                hits.Add(c.Receive().Result);
            });
        }

        Assert.AreEqual(0, hits.Count);
        c.Send(1).Wait();
        c.Send(2).Wait();
        c.Send(3).Wait();
        c.Send(4).Wait();
        c.Send(5).Wait();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, hits);
    }

    [TestMethod]
    public void SendAndReceiveBlocking_ManySenders()
    {
        var c = new Channel<int>();

        for (int i = 0; i < 5; i++) {
            var monoDoesntHaveNewLoopScope = i;
            Task.Run(() => {
                c.Send(monoDoesntHaveNewLoopScope + 1).Wait();
            });
        }

        var hits = new List<int>();
        for (int i = 0; i < 5; i++) {
            hits.Add(c.Receive().Result);
        }

        hits.Sort(); // 5 tasks run in random order
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, hits);
    }
}

[TestClass]
public class TestChannel_Select
{
    //[TestMethod]
    public async Task MultiChannels_SelectPullsFromEachChannel()
    {
        var c1 = new Channel<int>();
        var c2 = new Channel<int>();

        var hits = new List<int>();

        //await Go.Select(
        //    Go.Case(c1, hits.Add),
        //    Go.Case(c2, hits.Add));

        Assert.Fail("oh no");
    }
}


