// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Text;

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
        var mre = new ManualResetEvent(false);
        Task.Run(() => {
            for (int i = 0; i < 5; i++) {
                hits.Add(c.Receive().Result);
            }
            mre.Set();
        });

        Assert.AreEqual(0, hits.Count);
        c.Send(1).Wait();
        c.Send(2).Wait();
        c.Send(3).Wait();
        c.Send(4).Wait();
        c.Send(5).Wait();
        mre.WaitOne();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, hits);
    }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    // we don't need to await in this case because we're using AutoResetEvent for synchronization
    [TestMethod]
    public void SendAndReceiveBlocking_ManyReceivers()
    {
        var c = new Channel<int>();

        var hits = new List<int>();
        var evt = new AutoResetEvent(false);
        for (int i = 0; i < 5; i++) {
            Task.Run(() => {
                hits.Add(c.Receive().Result);
                evt.Set();
            });
        }

        Assert.AreEqual(0, hits.Count);
        c.Send(1);
        evt.WaitOne();
        CollectionAssert.AreEqual(new[] { 1 }, hits);

        c.Send(2);
        evt.WaitOne();
        CollectionAssert.AreEqual(new[] { 1, 2 }, hits);

        c.Send(3);
        evt.WaitOne();
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, hits);

        c.Send(4);
        evt.WaitOne();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, hits);

        c.Send(5);
        evt.WaitOne();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, hits);
    }

    // we don't need to await in this case because we're using AutoResetEvent for synchronization
    [TestMethod]
    public void SendAndReceiveBlocking_ManyReceivers_ArrivingOneAtATime()
    {
        var c = new Channel<int>();

        var hits = new List<int>();
        var evt = new AutoResetEvent(false);
        Action addReceiver = () => {
            Task.Run(() => {
                hits.Add(c.Receive().Result);
                evt.Set();
            });
        };
        Assert.AreEqual(0, hits.Count);
        addReceiver();
        c.Send(1);
        evt.WaitOne();
        CollectionAssert.AreEqual(new[] { 1 }, hits);

        addReceiver();
        c.Send(2);
        evt.WaitOne();
        CollectionAssert.AreEqual(new[] { 1, 2 }, hits);

        addReceiver();
        c.Send(3);
        evt.WaitOne();
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, hits);

        addReceiver();
        c.Send(4);
        evt.WaitOne();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, hits);

        addReceiver();
        c.Send(5);
        evt.WaitOne();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, hits);
    }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

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
    [TestMethod]
    public async Task MultiChannels_SelectPullsFromEachChannel()
    {
        var c1 = new Channel<int>();
        var c2 = new Channel<int>();

        var sb = new StringBuilder();

        var t = Task.Run(async () => {
            await c1.Send(1);
            await c2.Send(2);
        });

        // it frees up after the first time it's hit
        await Go.Select(
            Go.Case(c1, i => sb.Append($"c1={i};")),
            Go.Case(c2, i => sb.Append($"c2={i};")));
        Assert.AreEqual("c1=1;", sb.ToString());

        await Go.Select(
            Go.Case(c1, i => sb.Append($"c1={i};")),
            Go.Case(c2, i => sb.Append($"c2={i};")));
        Assert.AreEqual("c1=1;c2=2;", sb.ToString());
    }

    [TestMethod]
    public async Task MultiChannels_SelectDoesntLoseUnselectedValues()
    {
        var c1 = new Channel<int>();
        var c2 = new Channel<int>();

        var sb = new StringBuilder();

        var t1 = c1.Send(1); // don't wait for the sends to complete, they won't until later
        var t2 = c2.Send(2);
       
        // it frees up after the first time it's hit
        await Go.Select(
            Go.Case(c1, i => sb.Append($"c1={i};")),
            Go.Case(c2, i => sb.Append($"c2={i};")));
        Assert.AreEqual("c1=1;", sb.ToString());

        await Go.Select(
            Go.Case(c1, i => sb.Append($"c1={i};")),
            Go.Case(c2, i => sb.Append($"c2={i};")));
        Assert.AreEqual("c1=1;c2=2;", sb.ToString());
    }

    [TestMethod]
    public async Task MultiChannels_SelectCanShareLambdas()
    {
        var c1 = new Channel<int>();
        var c2 = new Channel<int>();
        var c3 = new Channel<string>();

        var sb = new StringBuilder();

        var t1 = c1.Send(1); // don't wait for the sends to complete, they won't until later
        var t2 = c2.Send(2);
        var t3 = c3.Send("3");

        // it frees up after the first time it's hit
        await Go.Select(
            Go.Case(new[] { c1, c2 } , i => sb.Append($"cx={i};")),
            Go.Case(c3, i => sb.Append($"c3={i};")));
        Assert.AreEqual("cx=1;", sb.ToString());

        await Go.Select(
            Go.Case(new[] { c1, c2 }, i => sb.Append($"cx={i};")),
            Go.Case(c3, i => sb.Append($"c3={i};")));
        Assert.AreEqual("cx=1;cx=2;", sb.ToString());

        await Go.Select(
            Go.Case(new[] { c1, c2 }, i => sb.Append($"cx={i};")),
            Go.Case(c3, i => sb.Append($"c3={i};")));
        Assert.AreEqual("cx=1;cx=2;c3=3;", sb.ToString());

    }
}

[TestClass]
public class TestChannel_Closing
{
    [TestMethod]
    public void ReceiveOnClosedChannelReturnsDefault()
    {
        var ci = new Channel<int>();
        ci.Close();
        Assert.AreEqual(0, ci.Receive().Result);

        var cs = new Channel<string>();
        cs.Close();
        Assert.AreEqual(null, cs.Receive().Result);
    }

    [TestMethod]
    public void SendOnClosedChannelThrows()
    {
        var ci = new Channel<int>();
        ci.Close();
        var ex = ExAssert.Catch(new Action(() => ci.Send(1).Wait()));
        Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidOperationException));

        ex = null;

        var cs = new Channel<string>();
        cs.Close();
        ex = ExAssert.Catch(new Action(() => cs.Send("a").Wait()));
        Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidOperationException));
    }

    [TestMethod]
    public void ReceiveOnClosedChannelCompletesExistingBlockedThingsFirst()
    {
        var ci = new Channel<int>();
        var t1 = ci.Send(1); // cheating here by not awaiting
        var t2 = ci.Send(2); // cheating here by not awaiting
        ci.Close();
        Assert.AreEqual(1, ci.Receive().Result);
        Assert.AreEqual(2, ci.Receive().Result);
        Assert.AreEqual(0, ci.Receive().Result);
    }

    [TestMethod]
    public void ReceiveOnClosedChannelEmptiesBufferFirst()
    {
        var ci = new BufferedChannel<int>(2);
        ci.Send(1).Wait();
        ci.Send(2).Wait();
        ci.Close();
        Assert.AreEqual(1, ci.Receive().Result);
        Assert.AreEqual(2, ci.Receive().Result);
        Assert.AreEqual(0, ci.Receive().Result);
    }

    [TestMethod]
    public void ClosingAReceivingChannelCompletesItImmediatelyWithDefault()
    {
        // this tests all things in m_queue not in m_promises; we need some other thing for that
        var ci = new Channel<int>();
        Go.Run(async () => {
            await Task.Delay(25); // let the Receive begin
            ci.Close();
        });
        var i = ci.Receive().Result;
        
        Assert.AreEqual(0, i);
    }

    [TestMethod]
    public void ClosingASelectedChannelCompletesItImmediatelyWithNoCallback()
    {
        var ci = new Channel<int>();
        var hits = new List<int>();
        Go.Run(async () => {
            await Task.Delay(25); // let the Receive begin
            ci.Close();
        });
        Go.Select(
            Go.Case(ci, hits.Add)).Wait(); // if the channel doesn't complete our unit test will hang and we'll find out the hard way

        Assert.AreEqual(0, hits.Count);
    }

    [TestMethod]
    public void SelectOnClosedChannelReturnsImmediately()
    {
        var ci = new Channel<int>(); ci.Close();
        var hits = new List<int>();
        Go.Select(
            Go.Case(ci, hits.Add)).Wait(); // if the channel doesn't complete our unit test will hang and we'll find out the hard way

        Assert.AreEqual(0, hits.Count);
    }

    [TestMethod]
    public async Task SelectIgnoresClosedChannels()
    {
        var ci = new Channel<int>();
        var t1 = ci.Send(1);

        var cs = new Channel<string>();
        cs.Close();

        var hits = new List<object>();
        await Go.Select(
            Go.Case(ci, i => hits.Add(i)),
            Go.Case(cs, s => hits.Add(s)));

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual(1, hits[0]);
    }
}

public static class ExAssert
{
    public static Exception Catch(Action throws)
    {
        try
        {
            throws();
        }
        catch(Exception e)
        {
            return e;
        }
        return null;
    }
}
