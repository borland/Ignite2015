﻿using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

[TestFixture]
public class Channel
{
	[Test]
	public void SendAndReceive_Buffer_1()
	{
		var c = new Channel<int>(bufferSize: 1);
		c.Send(1).Wait();
		Assert.AreEqual(1, c.Receive().Result);
	}

	[Test]
	public void SendAndReceive_Buffer_5()
	{
		var c = new Channel<int>(bufferSize: 5);
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

	[Test]
	public void SendAndReceiveBlocking_1item()
	{
		var c = new Channel<int>(bufferSize: 0);

		var hits = new List<int>();
		Task.Run(() => {
			var x = c.Receive().Result;
			hits.Add(x);
		});

		CollectionAssert.IsEmpty(hits);
		c.Send(1).Wait();
		CollectionAssert.AreEqual(new[]{ 1 }, hits);
	}


	[Test]
	public void SendAndReceiveBlocking_Sequence()
	{
		var c = new Channel<int>(bufferSize: 0);

		var hits = new List<int>();
		Task.Run(() => {
			for(int i = 0; i< 5; i++) {
				hits.Add(c.Receive().Result);
			}
		});

		CollectionAssert.IsEmpty(hits);
		c.Send(1).Wait();
		c.Send(2).Wait();
		c.Send(3).Wait();
		c.Send(4).Wait();
		c.Send(5).Wait();
		CollectionAssert.AreEqual(new[]{ 1, 2, 3, 4, 5 }, hits);
	}

	[Test]
	public void SendAndReceiveBlocking_ManyReceivers()
	{
		var c = new Channel<int>(bufferSize: 0);

		var hits = new List<int>();
		for(int i = 0; i< 5; i++) {
			Task.Run(() => {
				hits.Add(c.Receive().Result);
			});
		}

		CollectionAssert.IsEmpty(hits);
		c.Send(1).Wait();
		c.Send(2).Wait();
		c.Send(3).Wait();
		c.Send(4).Wait();
		c.Send(5).Wait();
		CollectionAssert.AreEqual(new[]{ 1, 2, 3, 4, 5 }, hits);
	}

	[Test]
	public void SendAndReceiveBlocking_ManySenders()
	{
		var c = new Channel<int>(bufferSize: 0);

		for(int i = 0; i< 5; i++) {
			var monoDoesntHaveNewLoopScope = i;
			Task.Run(() => {
				c.Send(monoDoesntHaveNewLoopScope + 1).Wait();
			});
		}

		var hits = new List<int>();
		for(int i = 0; i< 5; i++) {
			hits.Add(c.Receive().Result);
		}

		hits.Sort(); // 5 tasks run in random order
		CollectionAssert.AreEqual(new[]{ 1, 2, 3, 4, 5 }, hits);
	}
}

