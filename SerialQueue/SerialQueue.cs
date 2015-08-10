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

public interface IDispatchQueue
{
    IDisposable DispatchAsync(Action action);
    void DispatchSync(Action action);
}

public class SerialQueue : IDispatchQueue, IDisposable
{
    // lock-order: We must always lock m_asyncActions BEFORE we lock
    // m_executionLock. Once we hold m_executionLock we can release m_asyncActionsLock
    readonly List<Action> m_asyncActions = new List<Action>();
    volatile bool asyncActionsAreProcessing = false; // acquire m_asyncActions lock before making decisions
    readonly object m_executionLock = new object();

    public SerialQueue()
    { }

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
        try {
            Monitor.Enter(m_asyncActions, ref actionsLockTaken);
            Debug.Assert(actionsLockTaken);

            if (asyncActionsAreProcessing)
                return; // another thread is already processing, we don't want to waste this one sitting on m_executionLock
            asyncActionsAreProcessing = true;
            // even though we don't hold m_asyncActionsLock when asyncActionsAreProcessing is set to false
            // that should be OK as the only "contention" happens up here while we do hold it

            while (m_asyncActions.Count > 0) {
                lock (m_executionLock) {
                    // get the head of the queue, then release the lock
                    var action = m_asyncActions[0];
                    m_asyncActions.RemoveAt(0);
                    Monitor.Exit(m_asyncActions);
                    actionsLockTaken = false;

                    // process the action
                    action();
                }

                // now re-acquire the lock for the next thing
                Debug.Assert(!actionsLockTaken);
                Monitor.Enter(m_asyncActions, ref actionsLockTaken);
                Debug.Assert(actionsLockTaken);
            }
        }
        finally {
            asyncActionsAreProcessing = false;
            if (actionsLockTaken)
                Monitor.Exit(m_asyncActions);
        }
    }

    public void DispatchSync(Action action)
    {
        bool actionsLockTaken = false;
        try {
            Monitor.Enter(m_asyncActions, ref actionsLockTaken);
            Debug.Assert(actionsLockTaken);

            if (m_asyncActions.Count == 0) {
                lock (m_executionLock) {
                    Monitor.Exit(m_asyncActions);
                    actionsLockTaken = false;

                    action();
                }
            }
            else { // the queue is busy but we still hold the queue lock
                var mre = new ManualResetEvent(false);
                DispatchAsync(() => {
                    action();
                    mre.Set();
                });
                Monitor.Exit(m_asyncActions);
                actionsLockTaken = false;

                mre.WaitOne();
            }
        }
        finally { // should never get here but inc ase we have some unepxectd throw
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
