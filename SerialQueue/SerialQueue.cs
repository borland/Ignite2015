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

    // lock-order: We must always lock m_asyncActions BEFORE we lock
    // m_executionLock. Once we hold m_executionLock we can release m_asyncActionsLock
    readonly List<Action> m_asyncActions = new List<Action>();
    volatile bool m_actionsAreProcessing = false; // acquire m_asyncActions lock before making decisions
    readonly bool m_trackQueueForVerify = false;
    readonly HashSet<Timer> m_timers = new HashSet<Timer>();

    public SerialQueue(bool trackQueueForVerify = false)
    { 
        m_trackQueueForVerify = trackQueueForVerify; 
    }

    public void InternalVerifyQueue(IDispatchQueue otherQueue)
    {
        if (m_trackQueueForVerify && this != otherQueue)
            throw new InvalidOperationException("On the wrong queue");
    }

    public event EventHandler<UnhandledExceptionEventArgs> UnhandledException;

    public IDisposable DispatchAfter(TimeSpan dueTime, Action action)
    {
        IDisposable token = null;
        Timer timer = null;
        timer = new Timer(_ => {
            Interlocked.Exchange(ref token, DispatchAsync(action));
            lock (m_timers)
                m_timers.Remove(timer);

        }, null, dueTime, Timeout.InfiniteTimeSpan);

        lock (m_timers)
            m_timers.Add(timer);

        token = new AnonymousDisposable(() => {
            lock (m_timers)
                m_timers.Remove(timer);
        });

        return new AnonymousDisposable(() => {
            var tkn = Interlocked.Exchange(ref token, null);
            if(tkn != null)
                tkn.Dispose();
        });
    }

    public IDisposable DispatchAsync(Action action)
    {
        lock (m_asyncActions)
            m_asyncActions.Add(action);

        ThreadPool.QueueUserWorkItem(s => ProcessAsyncActions());

        return new AnonymousDisposable(() => {
            lock (m_asyncActions)
                m_asyncActions.Remove(action);
        });
    }

    void ProcessAsyncActions()
    {
        bool actionsLockTaken = false;
        IDispatchQueue previousQueue = null;
        try
        {
            Monitor.Enter(m_asyncActions, ref actionsLockTaken);
            Debug.Assert(actionsLockTaken);

            if (m_actionsAreProcessing)
                return; // another thread is already processing, this one can't do anything helpful here
            m_actionsAreProcessing = true;

            // even though we don't hold m_asyncActionsLock when asyncActionsAreProcessing is set to false
            // that should be OK as the only "contention" happens up here while we do hold it
            if (m_trackQueueForVerify)
                previousQueue = DispatchQueue.SetCurrentQueue(this);

            while (m_asyncActions.Count > 0)
            {
                // get the head of the queue, then release the lock
                var action = m_asyncActions[0];
                m_asyncActions.RemoveAt(0);
                Monitor.Exit(m_asyncActions);
                actionsLockTaken = false;

                // process the action
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    var handler = UnhandledException;
                    if (handler != null)
                        handler(this, new UnhandledExceptionEventArgs(exception));
                }
            
                // now re-acquire the lock for the next thing
                Debug.Assert(!actionsLockTaken);
                Monitor.Enter(m_asyncActions, ref actionsLockTaken);
                Debug.Assert(actionsLockTaken);
            }
        }
        finally
        {
            if (m_trackQueueForVerify)
                DispatchQueue.SetCurrentQueue(previousQueue);

            m_actionsAreProcessing = false;
            if (actionsLockTaken)
                Monitor.Exit(m_asyncActions);
        }
    }

    public void DispatchSync(Action action)
    {
        bool actionsLockTaken = false;
        try
        {
            Monitor.Enter(m_asyncActions, ref actionsLockTaken);
            Debug.Assert(actionsLockTaken);

            if (m_asyncActions.Count == 0 && !m_actionsAreProcessing)
            {
                IDispatchQueue previousQueue = null;
                if (m_trackQueueForVerify)
                    previousQueue = DispatchQueue.SetCurrentQueue(this);

                m_actionsAreProcessing = true; // TODO when can we reset this? Argh that's what the execution lock was for

                Monitor.Exit(m_asyncActions);
                actionsLockTaken = false;

                // process the action
                try
                {
                    action(); // DO NOT CATCH EXCEPTIONS. We're excuting synchronously so just let it throw
                }
                finally
                {
                    if (m_trackQueueForVerify)
                        DispatchQueue.SetCurrentQueue(previousQueue);
                }
            }
            else
            { // the queue is busy but we still hold the queue lock
                var queueAcquired = new ManualResetEvent(false);
                var releaseQueue = new ManualResetEvent(false);
                DispatchAsync(() => {
                    queueAcquired.Set();
                    releaseQueue.WaitOne();
                });
                Monitor.Exit(m_asyncActions);
                actionsLockTaken = false;

                queueAcquired.WaitOne();
                try
                {
                    action(); // DO NOT CATCH EXCEPTIONS. We're excuting synchronously so just let it throw
                }
                finally
                {
                    releaseQueue.Set();
                }
            }
        }
        finally
        { // should never get here but inc ase we have some unepxectd throw
            if (actionsLockTaken)
                Monitor.Exit(m_asyncActions);
        }
    }

    // dump the queue (which should cause any workers to stop after their current action)
    public void Dispose()
    {
        lock (m_asyncActions)
            m_asyncActions.Clear();
    }
}
