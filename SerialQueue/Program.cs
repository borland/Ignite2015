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

class Program
{
    static void Main(string[] args)
    {
        const int NumQueues = 1;

        Console.WriteLine($"About to create {NumQueues} queues"); Console.ReadLine();
        var memory = GC.GetTotalMemory(forceFullCollection: true);

        // create all the queues
        var queues = new SerialQueue[NumQueues];
        for(int i = 0; i < NumQueues; i++) {
            queues[i] = new SerialQueue();
        }

        var memory2 = GC.GetTotalMemory(forceFullCollection: true);
        var diff = (memory2 - memory) / 1024;

        Console.WriteLine($"Queues consume {diff}KB; About to process events"); Console.ReadLine();
        var sw = Stopwatch.StartNew();

        // post all the events
        var counters = new int[NumQueues];
        for (int i = 0; i < NumQueues; i++) {
            var loopi = i;
            queues[i].DispatchAsync(() => {
                counters[loopi]++;
            });
        }
        for (int i = 0; i < NumQueues; i++) {
            var loopi = i;
            queues[i].DispatchAsync(() => {
                counters[loopi]++;
            });
        }
        for (int i = 0; i < NumQueues; i++) {
            var loopi = i;
            queues[i].DispatchSync(() => {
                counters[loopi]++;
            });
        }
        for(int i = 0; i < NumQueues; i++) {
            if (counters[i] != 3) {
                Console.WriteLine($"queue {i} broken!. hitcount was {counters[i]} instead of 3");
            }
        }
        sw.Stop();

        Console.WriteLine($"Processed in {sw.ElapsedMilliseconds}ms; about to dispose"); Console.ReadLine();

        // dispose all the queues
        foreach (var q in queues)
            q.Dispose();
    }
}

// manages a "queue" by posting events to a worker thread
sealed class ThreadQueue : IDispatchQueue, IDisposable
{
    readonly Thread m_thread;
    volatile bool m_isDisposed = false;
    readonly List<Action> m_asyncActions = new List<Action>();

    public ThreadQueue()
    {
        m_thread = new Thread(ThreadProc);
        m_thread.IsBackground = true;
        m_thread.Start();
    }

    public IDisposable DispatchAsync(Action action)
    {
        lock (m_asyncActions) {
            m_asyncActions.Add(action);
            Monitor.Pulse(m_asyncActions);
        }

        return new AnonymousDisposable(() => {
            lock(m_asyncActions)
                m_asyncActions.Remove(action);
        });
    }

    public void DispatchSync(Action action)
    {
        var mre = new ManualResetEvent(false);
        DispatchAsync(() => {
            action();
            mre.Set();
        });
        mre.WaitOne();
    }

    public void Dispose()
    {
        m_isDisposed = true;
        lock (m_asyncActions) {
            m_asyncActions.Clear();
            Monitor.Pulse(m_asyncActions);
        }
    }

    void ThreadProc()
    {
        while(!m_isDisposed) {
            Action action = null;
            lock (m_asyncActions) {
                if (m_asyncActions.Count == 0)
                    Monitor.Wait(m_asyncActions);

                if (m_isDisposed || m_asyncActions.Count == 0) // either disposed or spurious wakeup
                    continue;
                
                action = m_asyncActions[0];
                m_asyncActions.RemoveAt(0);
            }
            Debug.Assert(action != null);
            action();
        }
    }
}
