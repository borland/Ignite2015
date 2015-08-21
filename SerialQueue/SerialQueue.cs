// Copyright (c) 2015 Orion Edwards
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

namespace Dispatch
{
    public interface IDispatchQueue : IDisposable
    {
        IDisposable DispatchAsync(Action action);
        IDisposable DispatchAfter(TimeSpan dueTime, Action action);
        void DispatchSync(Action action);
    }

    public interface IDispatchQueueInternal
    {
        void InternalVerifyQueue(IDispatchQueue otherQueue);
    }

    /// <summary>A serial queue needs a threadpool to run tasks on. You can provide your own implementation if you want to have a custom threadpool with it's own limits (e.g. no more than X concurrent threads)</summary>
    public interface IThreadPool
    {
        void QueueWorkItem(Action action);
        IDisposable Schedule(TimeSpan dueTime, Action action);
    }

    public class ClrThreadPool : IThreadPool
    {
        static ClrThreadPool s_default = new ClrThreadPool();
        public static ClrThreadPool Default { get { return s_default; } }

        public void QueueWorkItem(Action action)
        { ThreadPool.QueueUserWorkItem(_ => action()); }

        public IDisposable Schedule(TimeSpan dueTime, Action action)
        { return new Timer(_ => action(), null, dueTime, TimeSpan.FromMilliseconds(-1)); }
    }

    public static class DispatchQueue
    {
        [ThreadStatic]
        static IDispatchQueue s_currentQueue;

        // this doesn't work for nested DispatchSync calls. Perhaps that's why apple don't allow it in GCD themselves?
        public static void VerifyQueue(this IDispatchQueue queue)
        {
            var iq = queue as IDispatchQueueInternal;
            if (iq != null)
                iq.InternalVerifyQueue(s_currentQueue);
        }

        public static IDispatchQueue Current { get { return s_currentQueue; } }

        public static IDispatchQueue SetCurrentQueue(IDispatchQueue queue)
        {
            var previousQueue = s_currentQueue;
            s_currentQueue = queue;
            return previousQueue;
        }
    }

    public sealed class AnonymousDisposable : IDisposable
    {
        Action m_action;

        public AnonymousDisposable(Action action)
        {
            Debug.Assert(action != null, "action must not be null");
            m_action = action;
        }

        public void Dispose()
        {
            var handler = Interlocked.Exchange(ref m_action, null);  // only call once
            if (handler != null)
                handler();
        }
    }

    public class SerialQueue : IDispatchQueue, IDispatchQueueInternal
    {
        public class UnhandledExceptionEventArgs : EventArgs
        {
            readonly Exception m_exception;
            public UnhandledExceptionEventArgs(Exception exception)
            { m_exception = exception; }

            public Exception Exception { get { return m_exception; } }
        }

        readonly IThreadPool m_threadPool;
        readonly bool m_trackQueueForVerify = false;

        // lock-order: We must never hold both these locks concurrently
        readonly object m_schedulerLock = new object(); // acquire this before adding any async/timer actions
        readonly object m_executionLock = new object(); // acquire this before doing dispatchSync

        readonly List<Action> m_asyncActions = new List<Action>(); // aqcuire m_schedulerLock 
        readonly HashSet<IDisposable> m_timers = new HashSet<IDisposable>(); // acquire m_schedulerLock
        volatile bool m_asyncActionsAreProcessing = false; // acquire m_schedulerLock
        bool m_isDisposed = false; // acquire m_schedulerLock

        public SerialQueue(IThreadPool threadpool, bool trackQueueForVerify = false)
        {
            if (threadpool == null)
                throw new ArgumentNullException("threadpool");
            
            m_threadPool = threadpool;
            m_trackQueueForVerify = trackQueueForVerify;
        }

        public SerialQueue(bool trackQueueForVerify = false) : this(ClrThreadPool.Default, trackQueueForVerify)
        { }

        public void InternalVerifyQueue(IDispatchQueue otherQueue)
        {
            if (m_trackQueueForVerify && this != otherQueue)
                throw new InvalidOperationException("On the wrong queue");
        }

        public event EventHandler<UnhandledExceptionEventArgs> UnhandledException;

        public IDisposable DispatchAfter(TimeSpan dueTime, Action action)
        {
            IDisposable cancel = null;
            IDisposable timer = null;

            lock (m_schedulerLock)
            {
                if (m_isDisposed)
                    throw new ObjectDisposedException("SerialQueue", "Cannot call DispatchAfter on a disposed queue");

                timer = m_threadPool.Schedule(dueTime, () => {
                    lock(m_schedulerLock)
                    {
                        m_timers.Remove(timer);
                        if (cancel == null || m_isDisposed) // we've been canceled OR the queue has been disposed
                            return;

                        // we must call DispatchAsync while still holding m_schedulerLock to prevent a window where we get disposed right here
                        cancel = DispatchAsync(action); 
                    }

                });
                m_timers.Add(timer);
            }

            cancel = new AnonymousDisposable(() => {
                lock (m_schedulerLock)
                    m_timers.Remove(timer);

                timer.Dispose();
            });

            return new AnonymousDisposable(() => {
                lock (m_schedulerLock) {
                    if (cancel != null) {
                        cancel.Dispose(); // this will either cancel the timer or cancel the DispatchAsync depending on which stage it's in
                        cancel = null;
                    }
                }
            });
        }

        public IDisposable DispatchAsync(Action action)
        {
            lock (m_schedulerLock)
            {
                if (m_isDisposed)
                    throw new ObjectDisposedException("SerialQueue", "Cannot call DispatchSync on a disposed queue");

                m_asyncActions.Add(action);
                if (!m_asyncActionsAreProcessing)
                {
                    m_asyncActionsAreProcessing = true;
                    m_threadPool.QueueWorkItem(ProcessAsyncActions);
                }
            }

            return new AnonymousDisposable(() => {
                lock (m_schedulerLock)
                    m_asyncActions.Remove(action);
            });
        }

        void ProcessAsyncActions()
        {
            bool schedulerLockTaken = false;
            try
            {
                Monitor.Enter(m_schedulerLock, ref schedulerLockTaken);
                Debug.Assert(schedulerLockTaken);

                if (m_isDisposed)
                    return; // the actions will have been dumped, there's no point doing anything

                // even though we don't hold m_schedulerLock when asyncActionsAreProcessing is set to false
                // that should be OK as the only "contention" happens up here while we do hold it
                if (m_trackQueueForVerify)
                    DispatchQueue.SetCurrentQueue(this);

                while (m_asyncActions.Count > 0)
                {
                    // get the head of the queue, then release the lock
                    var action = m_asyncActions[0];
                    m_asyncActions.RemoveAt(0);
                    Monitor.Exit(m_schedulerLock);
                    schedulerLockTaken = false;

                    // process the action
                    try
                    {
                        lock(m_executionLock) // we must lock here or a DispatchSync could run concurrently with the last thing in the queue
                            action();
                    }
                    catch (Exception exception)
                    {
                        var handler = UnhandledException;
                        if (handler != null)
                            handler(this, new UnhandledExceptionEventArgs(exception));
                    }

                    // now re-acquire the lock for the next thing
                    Debug.Assert(!schedulerLockTaken);
                    Monitor.Enter(m_schedulerLock, ref schedulerLockTaken);
                    Debug.Assert(schedulerLockTaken);
                }
            }
            finally
            {
                if (m_trackQueueForVerify)
                    DispatchQueue.SetCurrentQueue(null);

                m_asyncActionsAreProcessing = false;
                if (schedulerLockTaken)
                    Monitor.Exit(m_schedulerLock);
            }
        }

        public void DispatchSync(Action action)
        {
            bool schedulerLockTaken = false;
            try
            {
                Monitor.Enter(m_schedulerLock, ref schedulerLockTaken);
                Debug.Assert(schedulerLockTaken);

                if (m_isDisposed)
                    throw new ObjectDisposedException("SerialQueue", "Cannot call DispatchSync on a disposed queue");

                if (!m_asyncActionsAreProcessing) // if there is any async stuff happening we must wait for it
                {
                    IDispatchQueue previousQueue = null;
                    if (m_trackQueueForVerify)
                        previousQueue = DispatchQueue.SetCurrentQueue(this);
                    
                    Monitor.Exit(m_schedulerLock);
                    schedulerLockTaken = false;

                    // process the action
                    try
                    {
                        lock (m_executionLock)
                            action(); // DO NOT CATCH EXCEPTIONS. We're excuting synchronously so just let it throw
                    }
                    finally
                    {
                        if (m_trackQueueForVerify)
                            DispatchQueue.SetCurrentQueue(previousQueue);
                    }
                }
                else
                { // the queue is busy, we need to acquire the execution lock
                    var asyncReady = new ManualResetEvent(false);
                    var syncDone = new ManualResetEvent(false);
                    DispatchAsync(() => {
                        asyncReady.Set();
                        syncDone.WaitOne();
                    });
                    Monitor.Exit(m_schedulerLock);
                    schedulerLockTaken = false;

                    try
                    {
                        asyncReady.WaitOne();
                        action(); // DO NOT CATCH EXCEPTIONS. We're excuting synchronously so just let it throw
                    }
                    finally
                    {
                        syncDone.Set(); // tell the dispatchAsync it can release the lock
                    }
                }
            }
            finally
            { // should never get here but inc ase we have some unepxectd throw
                if (schedulerLockTaken)
                    Monitor.Exit(m_schedulerLock);
            }
        }

        // dump the queue (which should cause any workers to stop after their current action)
        public void Dispose()
        {
            IDisposable[] timers;
            lock (m_schedulerLock)
            {
                if (m_isDisposed)
                    return; // double-dispose

                m_isDisposed = true;
                m_asyncActions.Clear();

                timers = m_timers.ToArray();
                m_timers.Clear();
            }
            foreach (var t in timers)
                t.Dispose();
        }
    }

}