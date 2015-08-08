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
        var cts = new CancellationTokenSource();

        // hrm need some sort of global lock over all 3 channels
        // so that we can cancel the receive on c1 when c2 arrives with a value
        // but do so in a thread-safe way
        var tasks = cases.Select(c => c.SelectAsync(cts.Token)).ToArray();
        var completedTask = await Task.WhenAny(tasks);
        cts.Cancel();

        foreach (var c in cases)
            c.ApplyResultIfIsMyTask(completedTask);

        return;
    }

    public interface ISelectCase
    {
        Task SelectAsync(CancellationToken cancellationToken);
        void ApplyResultIfIsMyTask(Task task);
    }

    public class SelectCase<T> : ISelectCase
    {
        public Channel<T> Channel;
        public Action<T> Action;

        Task Task;
        T Result = default(T);

        public Task SelectAsync(CancellationToken cancellationToken)
        {
            return Task = Channel.Receive().ContinueWith(a => {
                Result = a.Result;
            });
        }

        public void ApplyResultIfIsMyTask(Task task)
        {
            if (task == Task)
                Action(Result);
        }
    }

    public static SelectCase<T> Case<T>(Channel<T> channel, Action<T> action)
        => new SelectCase<T> { Channel = channel, Action = action };

}

public class Receiver<T>
{
    readonly TaskCompletionSource<T> m_tcs = new TaskCompletionSource<T>();

    public bool TryReceive(T value)
    {
        m_tcs.SetResult(value);
        return true;
    }

    public Task<T> ReceivedValue => m_tcs.Task;
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
        lock (m_queue) {
            if (m_promises.Count > 0)
                m_promises.Dequeue().SetResult(value);
            else
                m_queue.Enqueue(value);
        }
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
}

public class Channel<T>
{
    // thread-safety provided by awaitableQueue
    AwaitableQueue<Receiver<T>> m_receivers = new AwaitableQueue<Receiver<T>>();

    public virtual async Task Send(T value)
    {
        while (true) {
            var receiver = await m_receivers.Dequeue();
            if (receiver.TryReceive(value))
                break; // else we lost the race with another sender, wait for the next one
        }
    }

    public Task<T> Receive() => ReceiveInto(new Receiver<T>());

    public virtual Task<T> ReceiveInto(Receiver<T> receiver)
    {
        m_receivers.Enqueue(receiver);
        // now we have to wait for someone to call TryReceive on the receiver and for it to succeed
        // we can always block forever, it's not like Select where we have to retry
        return receiver.ReceivedValue;
    }
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
        throw new NotImplementedException("not yet");
        // first we need to check if there are any outstanding receivers!
        T valueToSend;
        lock(m_buffer) {
            m_buffer.Enqueue(value);
            if (m_buffer.Count <= m_bufferSize)
                return Task.FromResult(true);

            valueToSend = m_buffer.Dequeue();
        }

        return base.Send(valueToSend);
    }

    public override Task<T> ReceiveInto(Receiver<T> receiver)
    {
        throw new NotImplementedException("not yet");
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


public class OldChannel<T>
{
    readonly int m_bufferCapacity;
    readonly Queue<T> m_bufferQueue;
    readonly Queue<TaskCompletionSource<T>> m_receiverQueue = new Queue<TaskCompletionSource<T>>();
    readonly Queue<Tuple<T, TaskCompletionSource<bool>>> m_senderQueue = new Queue<Tuple<T, TaskCompletionSource<bool>>>();

    public OldChannel(int bufferSize = 0)
    {
        m_bufferCapacity = bufferSize;
        m_bufferQueue = new Queue<T>(bufferSize);
    }

    /* ——— send with buffer space
    send()
     -> push a value into the buffer
     -> return a task which completes immediately

    receive() then does
     -> pop a value off the buffer
     -> complete it

    ——— send with blocked receiver:
    send()
     -> pop a value off the receiver queue
     -> complete it
     -> return a task which completes immediately

    ——— send with no receiver:
    send()
     -> add a pair<value, task> to the send queue
     -> return the task which will complete later when someone receives

    receive() then does
     -> pop a task off the send queue
     -> push the held value from before into it */

    /// <summary>
    /// Send the specified value.
    /// Await on this function.
    /// </summary>
    /// <param name="value">Value.</param>
    public Task Send(T value)
    {
        lock (this) {
            if (m_bufferQueue.Count < m_bufferCapacity) {
                m_bufferQueue.Enqueue(value);
                return Task.FromResult(true);
            }
            else if (m_receiverQueue.Count > 0) {
                var tcs = m_receiverQueue.Dequeue();
                Monitor.Exit(this);
                try {
                    tcs.SetResult(value);
                }
                finally {
                    Monitor.Enter(this);
                }
                return Task.FromResult(true);
            }
            else { // we must block
                var tcs = new TaskCompletionSource<bool>();
                m_senderQueue.Enqueue(Tuple.Create(value, tcs));
                return tcs.Task;
            }
        }
    }

    /* ——— receive with buffered data:
    recieve()
     -> pop a value off the buffer
     -> return a task<T> which publishes that value immediately

    ——— receive with waiting sender:
    receive()
     -> pop a pair<task,T> off the sender queue
     -> complete the task (unblocking the sender)
     -> return a task which completes immediately with the value

    ——— receive with no sender or buffer:
    receive()
     -> add a task<T> to the receive queue
     -> return the task which will complete later when someone sends

    send() then does
    -> pop a task off the receive queue
    -> complete it */

    /// <summary>
    /// Receive a value from the channel.
    /// Await on this function
    /// </summary>
    public Task<T> Receive()
    {
        lock (this) {
            if (m_bufferQueue.Count > 0) { // if we have a buffered value, return it
                return Task.FromResult(m_bufferQueue.Dequeue());
            }
            else if (m_senderQueue.Count > 0) { // we have a blocked sender
                var t = m_senderQueue.Dequeue();
                Monitor.Exit(this);
                try {
                    t.Item2.SetResult(false);
                }
                finally {
                    Monitor.Enter(this);
                }
                return Task.FromResult(t.Item1);
            }
            else { // we must block ourselves
                var tcs = new TaskCompletionSource<T>();
                m_receiverQueue.Enqueue(tcs);
                return tcs.Task;
            }
        }
    }

    /* try-receive:
     * for each channel:
     * - call tryReceive with a CTS
     *   when the channel gets a send(), it will check if the CTS is set
     *   if it is set, it will buffer the value and return false.
     *     - this will NOT unblock the sender
     *   if it is not set, it will set the CTS and return the value and unblock
     * 
     * what this means is that when channels get values they can't just
     * return a task. They have to bufferSentValue, then tryReturn with a CTS
     * - no CTS means tryReturn always succeeds
     */
}