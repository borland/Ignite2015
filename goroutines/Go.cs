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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class Go
{
    public static void Run(Action action) => Task.Run(action);
    public static void Run<T1>(Action<T1> action, T1 arg1) => Task.Run(() => action(arg1));
    public static void Run<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2) => Task.Run(() => action(arg1, arg2));
    public static void Run<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) => Task.Run(() => action(arg1, arg2, arg3));
    public static void Run<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Task.Run(() => action(arg1, arg2, arg3, arg4));

    public static void Run<TResult>(Func<TResult> action) => Task.Run(action);
    public static void Run<T1, TResult>(Func<T1, TResult> action, T1 arg1) => Task.Run(() => action(arg1));
    public static void Run<T1, T2, TResult>(Func<T1, T2, TResult> action, T1 arg1, T2 arg2) => Task.Run(() => action(arg1, arg2));
    public static void Run<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3) => Task.Run(() => action(arg1, arg2, arg3));
    public static void Run<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Task.Run(() => action(arg1, arg2, arg3, arg4));

    // await on this function
    public static async Task Select(params ISelectCase[] cases)
    {
        int done = 0;
        Func<bool> sync = () => Interlocked.Exchange(ref done, 1) == 0;

        // hrm need some sort of global lock over all 3 channels
        // so that we can cancel the receive on c1 when c2 arrives with a value
        // but do so in a thread-safe way
        var tasks = cases.Select(c => c.SelectAsync(sync)).ToArray();
        var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);

        foreach (var c in cases)
            c.ApplyResultIfIsMyTask(completedTask);
    }

    public interface ISelectCase
    {
        Task SelectAsync(Func<bool> lockAcquirer);
        void ApplyResultIfIsMyTask(Task task);
    }

    class SelectCase<T> : ISelectCase
    {
        public Channel<T> Channel;
        public Action<T, bool> Action;

        Task Task;
        ReceivedValue<T> ReceivedValue = default(ReceivedValue<T>);

        public Task SelectAsync(Func<bool> sync)
        {
            if (Channel == null || !Channel.IsOpen)
                return Task.FromResult(false);

            var r = new SelectCaseReceiver<T>(sync);
            return Task = Channel.ReceiveInto(r).ContinueWith(t => ReceivedValue = t.Result, TaskContinuationOptions.ExecuteSynchronously);
        }

        public void ApplyResultIfIsMyTask(Task task)
        {
            if (task == Task)
                Action(ReceivedValue.Value, ReceivedValue.IsValid);
        }
    }

    class MultiSelectCase<T> : ISelectCase
    {
        public IEnumerable<Channel<T>> Channels;
        public Action<T, bool> Action;

        Task Task;
        ReceivedValue<T> ReceivedValue = default(ReceivedValue<T>);

        public Task SelectAsync(Func<bool> sync)
        {
            var r = new SelectCaseReceiver<T>(sync);
            return Task = Task.WhenAny(
                Channels.Select(c => c.ReceiveInto(r).ContinueWith(t => ReceivedValue = t.Result, TaskContinuationOptions.ExecuteSynchronously)));
        }

        public void ApplyResultIfIsMyTask(Task task)
        {
            if (task == Task)
                Action(ReceivedValue.Value, ReceivedValue.IsValid);
        }
    }

    public static ISelectCase Case<T>(IEnumerable<Channel<T>> channels, Action<T> action)
        => new MultiSelectCase<T> { Channels = channels, Action = (x, _) => action(x) };

    public static ISelectCase Case<T>(IEnumerable<Channel<T>> channels, Action<T, bool> action)
        => new MultiSelectCase<T> { Channels = channels, Action = action };

    public static ISelectCase Case<T>(Channel<T> channel, Action<T> action)
        => new SelectCase<T> { Channel = channel, Action = (x, _) => action(x) };

    public static ISelectCase Case<T>(Channel<T> channel, Action<T, bool> action)
        => new SelectCase<T> { Channel = channel, Action = action };
}

public struct ReceivedValue<T>
{
    /// <summary>Constructs a new received value</summary>
    /// <param name="value">The value</param>
    /// <param name="isValid">Whether the value is valid (i.e it didn't come as the result of closing a channel)</param>
    public ReceivedValue(T value, bool isValid = true)
    {
        IsValid = isValid;
        Value = value;
    }

    /// <summary>Value that was received</summary>
    public T Value { get; private set; }

    /// <summary>True if the value is valid. 
    /// You will receive an invalid value if you call Receive() on a channel and that channel gets closed while you are waiting</summary>
    public bool IsValid { get; private set; }
}

public interface IReceiver<T>
{
    bool TryReceive(T value, bool isValid);
    Task<ReceivedValue<T>> ReceivedValue { get; }
}

public class Receiver<T> : IReceiver<T>
{
    protected readonly TaskCompletionSource<ReceivedValue<T>> m_tcs = new TaskCompletionSource<ReceivedValue<T>>();

    public virtual bool TryReceive(T value, bool isValid)
    {
        m_tcs.SetResult(new ReceivedValue<T>(value, isValid));
        return true;
    }

    public Task<ReceivedValue<T>> ReceivedValue => m_tcs.Task;
}

public class SelectCaseReceiver<T> : Receiver<T>
{
    readonly Func<bool> m_sync;

    public SelectCaseReceiver(Func<bool> sync)
    { m_sync = sync; }

    public override bool TryReceive(T value, bool isValid)
    {
        if (m_sync()) { // we won the race
            m_tcs.SetResult(new ReceivedValue<T>(value, isValid));
            return true;
        }
        return false; // we lost the race
    }
}

// wrapper over a normal queue that has awaitable Dequeue
public class AwaitableQueue<T>
{
    readonly Queue<T> m_queue = new Queue<T>();

    // this is the "negative" queue for when people Dequeue more things than we actually have
    readonly Queue<TaskCompletionSource<T>> m_promises = new Queue<TaskCompletionSource<T>>(); // acquire m_queue to access

    public int Count
    {
        get
        {
            lock (m_queue)
                return m_queue.Count;
        }
    }

    public int PromisedCount
    {
        get
        {
            lock (m_queue)
                return m_promises.Count;
        }
    }

    public void Enqueue(T value)
    {
        TaskCompletionSource<T> tcs = null;
        lock (m_queue) {
            if (m_promises.Count > 0)
                tcs = m_promises.Dequeue();
            else
                m_queue.Enqueue(value);
        }
        if (tcs != null)
            tcs.SetResult(value);
    }

    /// <summary>Dequeues a value. If there is no value, returns a task
    /// which will complete next time someone enqueues a new value</summary>
    public Task<T> Dequeue()
    {
        lock (m_queue) {
            if (m_queue.Count > 0) {
                return Task.FromResult(m_queue.Dequeue());
            }
            else {
                var tcs = new TaskCompletionSource<T>();
                m_promises.Enqueue(tcs);
                return tcs.Task;
            }
        }
    }

    /// <summary>Tries an immediate dequeue. Will not create any new Tasks</summary>
    /// <param name="result">Value (if there was one)</param>
    /// <returns>Whether or not there was a value in the queue</returns>
    public bool TryDequeue(out T result)
    {
        lock (m_queue) {
            if (m_queue.Count > 0) {
                result = m_queue.Dequeue();
                return true;
            }
            else {
                result = default(T);
                return false;
            }
        }
    }

    /// <summary>Dumps the queue. Does not touch promised tasks</summary>
    /// <returns>The queue before we dumped it</returns>
    public T[] ClearQueue()
    {
        T[] queue;
        lock (m_queue) {
            queue = m_queue.ToArray();
            m_queue.Clear();
        }

        return queue;
    }
}

class S
{
    public static int s_token = 0; // statics in generic classes are per-type-instantiated
}

public class Channel<T> : IDisposable
{
    readonly int m_tkn;

    public Channel()
    {
        m_tkn = Interlocked.Increment(ref S.s_token);
    }

    volatile bool m_isOpen = true;

    // thread-safety provided by awaitableQueue
    protected readonly AwaitableQueue<IReceiver<T>> m_receivers = new AwaitableQueue<IReceiver<T>>();

    int SToken = 0;

    public virtual async Task Send(T value)
    {
        if (!m_isOpen) // in go, a send to a closed channel panics
            throw new InvalidOperationException("channel closed");

        var tkn = Interlocked.Increment(ref SToken);

        while (true) {
            var receiver = await m_receivers.Dequeue().ConfigureAwait(false);
            if (receiver.TryReceive(value, true)) {
                return;
            }
            // else we lost the race with another sender, wait for the next one
        }
    }

    public Task<T> Receive() => ReceiveInto(new Receiver<T>())
        .ContinueWith(
            t => t.Result.Value,
            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);

    public Task<ReceivedValue<T>> ReceiveEx() => ReceiveInto(new Receiver<T>());

    int RToken = 0;

    public virtual Task<ReceivedValue<T>> ReceiveInto(IReceiver<T> receiver)
    {
        // in go, a receive from a closed channel returns the zero value immediately
        // unless there are "unfinished" senders
        if (!m_isOpen && m_receivers.PromisedCount == 0) {
            receiver.TryReceive(default(T), false);
            return Task.FromResult(default(ReceivedValue<T>));
        }

        var tkn = Interlocked.Increment(ref RToken);

        m_receivers.Enqueue(receiver);
        // now we have to wait for someone to call TryReceive on the receiver and for it to succeed
        // we can always block forever, it's not like Select where we have to retry
        return receiver.ReceivedValue;
    }

    public bool Close()
    {
        // nuke all waiting tasks
        m_isOpen = false;
        var queued = m_receivers.ClearQueue();
        foreach (var x in queued)
            x.TryReceive(default(T), false);

        return false;
    }

    public bool IsOpen => m_isOpen;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            Close();
    }

    public void Dispose() => Dispose(true);
}

public static class ChannelExtensions
{
    /// <summary>We can't adapt normal foreach because we want it to be async</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channel"></param>
    /// <param name="action"></param>
    public static async Task ForEach<T>(this Channel<T> channel, Action<T> action)
    {
        while (true) {
            var r = await channel.ReceiveEx(); // can't use ConfigureAwait here as otherwise we might call action() in the wrong thread
            if (!r.IsValid)
                return;
            action(r.Value);
        }
    }

    public static Channel<TResult> Zip<TA, TB, TResult>(this Channel<TA> a, Channel<TB> b, Func<TA, TB, TResult> zipper)
    {
        var outgoing = new Channel<TResult>();
        Task.Run(async () => {
            try {
                while (true) {
                    var ra = await a.ReceiveEx().ConfigureAwait(false);
                    if (!ra.IsValid)
                        break;

                    var rb = await b.ReceiveEx().ConfigureAwait(false);
                    if (!rb.IsValid)
                        break;

                    await outgoing.Send(zipper(ra.Value, rb.Value));
                }
            }
            finally {
                outgoing.Close();
            }
        });
        return outgoing;
    }

    public static async Task<int> Sum<T>(this Channel<T> channel, Func<T, int> selector)
    {
        int sum = 0;
        await ForEach(channel, x => sum += selector(x));
        return sum;
    }

    public static async Task<ulong> Sum<T>(this Channel<T> channel, Func<T, ulong> selector)
    {
        ulong sum = 0;
        await ForEach(channel, x => sum += selector(x));
        return sum;
    }

    public static Channel<T> Where<T>(this Channel<T> channel, Func<T, bool> filter)
    {
        var outgoing = new Channel<T>();
        var _ = ForEach(channel, async value => {
            if (filter(value))
                await outgoing.Send(value);
        }).ContinueWith(t => outgoing.Close(), TaskContinuationOptions.ExecuteSynchronously);
        return outgoing;
    }

    public static Channel<TResult> Select<T, TResult>(this Channel<T> channel, Func<T, TResult> selector)
    {
        var outgoing = new Channel<TResult>();
        var _ = ForEach(channel, async value => {
            await outgoing.Send(selector(value));
        }).ContinueWith(t => outgoing.Close(), TaskContinuationOptions.ExecuteSynchronously);
        return outgoing;
    }

    public static Channel<TResult> Select<T, TResult>(this Channel<T> channel, Func<T, Task<TResult>> selector)
    {
        var outgoing = new Channel<TResult>();
        var _ = ForEach(channel, async value => {
            await outgoing.Send(await selector(value));
        }).ContinueWith(t => outgoing.Close(), TaskContinuationOptions.ExecuteSynchronously);
        return outgoing;
    }
}

public class WaitGroup
{
    readonly TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();
    int m_count = 0;

    public int Add(int n)
    {
        var count = Interlocked.Add(ref m_count, n);
        if (count <= 0)
            throw new InvalidOperationException("WaitGroup count went below zero"); // go panics when you ADD to a waitgroup that has gone below zero. Weird
        return count;
    }

    public int Done()
    {
        var count = Interlocked.Decrement(ref m_count);
        if (count == 0)
            m_tcs.TrySetResult(true);
        return count;
    }

    public Task<bool> Wait() => m_count == 0 ? Task.FromResult(true) : m_tcs.Task;
}

public class BufferedChannel<T> : Channel<T>
{
    readonly Queue<T> m_buffer;
    readonly int m_bufferSize;
    public BufferedChannel(int bufferSize)
    {
        if (bufferSize <= 0)
            throw new ArgumentException("buffer size must be > 0", nameof(bufferSize));

        m_bufferSize = bufferSize;
        m_buffer = new Queue<T>(bufferSize);
    }

    public override Task Send(T value)
    {
        // first we need to check if there are any outstanding receivers
        IReceiver<T> receiver;
        if (m_receivers.TryDequeue(out receiver) && receiver.TryReceive(value, true)) { // there was one and we managed to send to it
            return Task.FromResult(true); // so we're done
        }

        // else put the value in the queue
        T valueToSend;
        lock (m_buffer) {
            m_buffer.Enqueue(value);
            if (m_buffer.Count <= m_bufferSize)
                return Task.FromResult(true); // if the queue is not full, we're done

            // else we fallback to blocking send
            valueToSend = m_buffer.Dequeue();
        }

        return base.Send(valueToSend);
    }

    public override Task<ReceivedValue<T>> ReceiveInto(IReceiver<T> receiver)
    {
        lock (m_buffer) {
            if (m_buffer.Count > 0) {
                var value = m_buffer.Peek();
                if (receiver.TryReceive(value, true)) // values dequeued from a buffer are always valid
                    m_buffer.Dequeue();
                return Task.FromResult(new ReceivedValue<T>(value, true));
            }
        }
        // else the buffer is empty, act like a normal queue
        return base.ReceiveInto(receiver);
    }
}