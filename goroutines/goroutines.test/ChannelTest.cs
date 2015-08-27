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
    public void SendAndReceiveBlocking_1item()
    {
        var c = new Channel<int>();

        var hits = new List<int>();
        var t = Task.Run(async () => {
            var x = await c.Receive();
            hits.Add(x);
        });

        Assert.AreEqual(0, hits.Count);
        c.Send(1).Wait();
        t.Wait();
        CollectionAssert.AreEqual(new[] { 1 }, hits);
    }

    [TestMethod]
    public void SendAndReceiveBlocking_Sequence()
    {
        var c = new Channel<int>();

        var hits = new List<int>();

        var t = Task.Run(() => {
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
        t.Wait();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, hits);
    }

    [TestMethod]
    public void SendAndReceiveBlocking_ManyReceivers()
    {
        var c = new Channel<int>();

        var hits = new List<int>();
        var tasks = new List<Task>(5);
        for (int i = 0; i < 5; i++) {
            tasks.Add(Task.Run(() => {
                hits.Add(c.Receive().Result);
            }));
        }

        Assert.AreEqual(0, hits.Count);
        var _ = c.Send(1);
        // the first task to call Receive will complete first, but we can't control
        // the task launch order so we don't know which task that might be
        tasks.Remove(Task.WhenAny(tasks).Result);
        CollectionAssert.AreEqual(new[] { 1 }, hits);

        _ = c.Send(2);
        tasks.Remove(Task.WhenAny(tasks).Result);
        CollectionAssert.AreEqual(new[] { 1, 2 }, hits);

        _ = c.Send(3);
        tasks.Remove(Task.WhenAny(tasks).Result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, hits);

        _ = c.Send(4);
        tasks.Remove(Task.WhenAny(tasks).Result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, hits);

        _ = c.Send(5);
        tasks.Remove(Task.WhenAny(tasks).Result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, hits);

        Assert.AreEqual(0, tasks.Count);
    }

    // we don't need to await in this case because we're using AutoResetEvent for synchronization
    [TestMethod]
    public void SendAndReceiveBlocking_ManyReceivers_ArrivingOneAtATime()
    {
        var c = new Channel<int>();

        var hits = new List<int>();
        Func<Task> addReceiver = () => Task.Run(() => hits.Add(c.Receive().Result));
                
        Assert.AreEqual(0, hits.Count);
        var t = addReceiver();
        var _ = c.Send(1);
        t.Wait();
        CollectionAssert.AreEqual(new[] { 1 }, hits);

        t = addReceiver();
        _ = c.Send(2);
        t.Wait();
        CollectionAssert.AreEqual(new[] { 1, 2 }, hits);

        t = addReceiver();
        _ = c.Send(3);
        t.Wait();
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, hits);

        t = addReceiver();
        _ = c.Send(4);
        t.Wait();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, hits);

        t = addReceiver();
        _ = c.Send(5);
        t.Wait();
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
        Assert.AreEqual(new ReceivedValue<int>(1, true), ci.ReceiveEx().Result);
        Assert.AreEqual(new ReceivedValue<int>(2, true), ci.ReceiveEx().Result);
        Assert.AreEqual(new ReceivedValue<int>(0, false), ci.ReceiveEx().Result);
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

        Assert.AreEqual(new ReceivedValue<int>(0, false), ci.ReceiveEx().Result);
    }

    [TestMethod]
    public void ClosingASelectedChannelCompletesItImmediatelySignallingChannelClosed()
    {
        var ci = new Channel<int>();
        var hits = new List<Tuple<int, bool>>();
        Go.Run(async () => {
            await Task.Delay(25); // let the Receive begin
            ci.Close();
        });
        Go.Select(
            Go.Case(ci, (v, ok) => hits.Add(Tuple.Create(v, ok)))).Wait(); // if the channel doesn't complete our unit test will hang and we'll find out the hard way

        CollectionAssert.AreEqual(new[] { Tuple.Create(0, false) }, hits);
    }

    [TestMethod]
    public void SelectOnClosedChannelReturnsImmediatelyWithoutSignalling()
    {
        var ci = new Channel<int>(); ci.Close();
        var hits = new List<int>();
        Go.Select(
            Go.Case(ci, hits.Add)).Wait(); // if the channel doesn't complete our unit test will hang and we'll find out the hard way

        Assert.AreEqual(0, hits.Count);
    }

    [TestMethod]
    public async Task SelectIgnoresNullChannels()
    {
        var ci = new Channel<int>();
        var t1 = ci.Send(1);
        
        var hits = new List<object>();
        await Go.Select(
            Go.Case(ci, i => hits.Add(i)),
            Go.Case((Channel<string>)null, s => hits.Add(s)));

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual(1, hits[0]);
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
