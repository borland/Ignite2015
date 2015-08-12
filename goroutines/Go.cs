// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

    public class SelectCase<T> : ISelectCase
    {
        public Channel<T> Channel;
        public Action<T> Action;

        Task Task;
        T Result = default(T);

        public Task SelectAsync(Func<bool> sync)
        {
            if (!Channel.IsOpen)
                return Task.FromResult(false);

            var r = new SelectCaseReceiver<T>(sync);
            return Task = Channel.ReceiveInto(r).ContinueWith(t => Result = t.Result, TaskContinuationOptions.ExecuteSynchronously);
        }

        public void ApplyResultIfIsMyTask(Task task)
        {
            if (task == Task)
                Action(Result);
        }
    }

    public class MultiSelectCase<T> : ISelectCase
    {
        public IEnumerable<Channel<T>> Channels;
        public Action<T> Action;

        Task Task;
        T Result = default(T);

        public Task SelectAsync(Func<bool> sync)
        {
            var r = new SelectCaseReceiver<T>(sync);
            return Task = Task.WhenAny(
                Channels.Select(c => c.ReceiveInto(r).ContinueWith(t => Result = t.Result, TaskContinuationOptions.ExecuteSynchronously)));
        }

        public void ApplyResultIfIsMyTask(Task task)
        {
            if (task == Task)
                Action(Result);
        }
    }

    public static MultiSelectCase<T> Case<T>(IEnumerable<Channel<T>> channels, Action<T> action)
        => new MultiSelectCase<T> { Channels = channels, Action = action };

    public static SelectCase<T> Case<T>(Channel<T> channel, Action<T> action)
        => new SelectCase<T> { Channel = channel, Action = action };
}

public interface IReceiver<T>
{
    bool TryReceive(T value);
    Task<T> ReceivedValue { get; }
}

public class Receiver<T> : IReceiver<T>
{
    protected readonly TaskCompletionSource<T> m_tcs = new TaskCompletionSource<T>();

    public virtual bool TryReceive(T value)
    {
        m_tcs.SetResult(value);
        return true;
    }

    public Task<T> ReceivedValue => m_tcs.Task;
}

public class SelectCaseReceiver<T> : Receiver<T>
{
    readonly Func<bool> m_sync;

    public SelectCaseReceiver(Func<bool> sync)
    { m_sync = sync; }

    public override bool TryReceive(T value)
    {
        if (m_sync()) { // we won the race
            m_tcs.SetResult(value);
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
    
    /// <summary>Dumps the queue and completes all the promised tasks pushing the given value</summary>
    /// <returns>The queue before we dumped it</returns>
    public T[] CompleteAll(T with = default(T))
    {
        T[] queue;
        TaskCompletionSource<T>[] promises;
        lock (m_queue)
        {
            queue = m_queue.ToArray();
            m_queue.Clear();

            promises = m_promises.ToArray();
            m_promises.Clear();
        }

        foreach (var p in promises)
            p.SetResult(with);

        return queue;
    }
}

public class Channel<T>
{
    volatile bool m_isOpen = true;

    // thread-safety provided by awaitableQueue
    readonly AwaitableQueue<IReceiver<T>> m_receivers = new AwaitableQueue<IReceiver<T>>();

    public virtual async Task Send(T value)
    {
        if (!m_isOpen) // in go, a send to a closed channel panics
            throw new InvalidOperationException("channel closed");

        while (true) {
            var receiver = await m_receivers.Dequeue().ConfigureAwait(false);
            if (receiver.TryReceive(value))
                break; // else we lost the race with another sender, wait for the next one
        }
    }

    public Task<T> Receive() => ReceiveInto(new Receiver<T>());

    public virtual Task<T> ReceiveInto(IReceiver<T> receiver)
    {
        // in go, a receive from a closed channel returns the zero value immediately
        if (!m_isOpen) {
            receiver.TryReceive(default(T));
            return Task.FromResult(default(T));
        }

        m_receivers.Enqueue(receiver);
        // now we have to wait for someone to call TryReceive on the receiver and for it to succeed
        // we can always block forever, it's not like Select where we have to retry
        return receiver.ReceivedValue;
    }

    class ChannelClosedReceiver : IReceiver<T>
    {
        public Task<T> ReceivedValue => Task.FromResult(default(T));
        public bool TryReceive(T value) => true;
    }

    public bool Close() { // nuke all waiting tasks
        m_isOpen = false;
        var queued = m_receivers.CompleteAll(with: new ChannelClosedReceiver());
        foreach (var x in queued)
            x.TryReceive(default(T));

        return false;
    }

    public bool IsOpen => m_isOpen;

    // TODO unit tests for closing of channels

    // TODO we should be able to adapt a channel into an enumerable of sorts once we've implemented close
}

public static class ChannelExtensions
{
    /// <summary>We can't adapt normal foreach because we want it to be async</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channel"></param>
    /// <param name="action"></param>
    public static async Task ForEach<T>(this Channel<T> channel, Action<T> action)
    {
        while(channel.IsOpen) {
            var item = await channel.Receive();
            action(item);
        }
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

    public Task<bool> Wait() => m_tcs.Task;
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
        T valueToSend;
        lock(m_buffer) {
            m_buffer.Enqueue(value);
            if (m_buffer.Count <= m_bufferSize)
                return Task.FromResult(true);

            valueToSend = m_buffer.Dequeue();
        }

        return base.Send(valueToSend);
    }

    public override Task<T> ReceiveInto(IReceiver<T> receiver)
    {
        lock (m_buffer) {
            if(m_buffer.Count > 0) {
                var value = m_buffer.Peek();
                if (receiver.TryReceive(value))
                    m_buffer.Dequeue();
                return Task.FromResult(value);
            }
        }
        // else the buffer is empty, act like a normal queue
        return base.ReceiveInto(receiver);
    }
}