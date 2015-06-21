using NUnit.Framework;
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
	public void SendBlocking_1item()
	{
		var c = new Channel<int>(bufferSize: 0);

		var hits = new List<int>();
		Task.Run(() => {
			hits.Add(c.Receive().Result);
		});

		CollectionAssert.IsEmpty(hits);
		c.Send(1).Wait();
		CollectionAssert.AreEqual(new[]{ 1 }, hits);
	}

	[Test]
	public void SendBlocking_Sequence()
	{
		var c = new Channel<int>(bufferSize: 0);

		var hits = new List<int>();
		Task.Run(() => {
			var result = c.Receive().Result;
			hits.Add(result);
		});

		CollectionAssert.IsEmpty(hits);
		c.Send(1).Wait();
		c.Send(2).Wait();
		c.Send(3).Wait();
		c.Send(4).Wait();
		c.Send(5).Wait();
		CollectionAssert.AreEqual(new[]{ 1, 2, 3, 4, 5 }, hits);
	}
}


