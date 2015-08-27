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
using System.Xml.Linq;
using Dispatch;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        const int NumQueues = 2000;

        Console.WriteLine($"About to create {NumQueues} queues"); Console.ReadLine();
        var vmem = Process.GetCurrentProcess().VirtualMemorySize64;
        var amem = GC.GetTotalMemory(forceFullCollection: true);

        // create all the queues
        var operations = new OperationState[NumQueues];
        for (int i = 0; i < NumQueues; i++) {
            operations[i] = new OperationState { Queue = new SerialQueue() };
        }

        var vmem2 = Process.GetCurrentProcess().VirtualMemorySize64;
        var vdiff = Math.Round((vmem2 - vmem) / (1024.0), 2);
        var amem2 = GC.GetTotalMemory(forceFullCollection: true);
        var adiff = Math.Round((amem2 - amem) / (1024.0), 2);

        // put actual memory in as well as virtual
        Console.WriteLine($"Memory Used: {adiff} KB, {vdiff} KB of virtual"); Console.ReadLine();
        var sw = Stopwatch.StartNew();

        // post all the events
        foreach (var op in operations) {
            op.Queue.DispatchAsync(() => {
                //for (int i = 0; i < 3; i++)
                op.Document = ParseXml();
            });
        }
        foreach (var op in operations) {
            op.Queue.DispatchAsync(() => {
                //for (int i = 0; i < 3; i++)
                op.CD = GetCDByArtist(op.Document, "Bee Gees");
            });
        }
        foreach (var op in operations) {
            op.Queue.DispatchSync(() => {
                //for (int i = 0; i < 3; i++)
                op.Price = GetPrice(op.CD);
            });
        }
        foreach (var op in operations) {
            if (op.Price != 10.90)
                Console.WriteLine($"queue broken!. price was {op.Price} instead of 10.90");
        }
        sw.Stop();

        Console.WriteLine($"Processed in {sw.ElapsedMilliseconds}ms; about to dispose"); Console.ReadLine();

        // dispose all the queues
        foreach (IDisposable q in operations.Select(op => op.Queue))
            q.Dispose();
    }

    // "busy work" for our queues to do
    static XDocument ParseXml() => XDocument.Load("cdcatalog.xml");

    static XElement GetCDByArtist(XDocument document, string artistName)
        => document.Root.Elements("CD").FirstOrDefault(e => e.Element("ARTIST").Value == artistName);

    static double GetPrice(XElement cdElement)
        => double.Parse(cdElement.Element("PRICE").Value);

    class OperationState
    {
        public IDispatchQueue Queue;

        public XDocument Document;
        public XElement CD;
        public double Price;
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

    public void VerifyQueue()
    {
        if (Thread.CurrentThread != m_thread)
            throw new InvalidOperationException("wrong thread");
    }

    public IDisposable DispatchAsync(Action action)
    {
        lock (m_asyncActions) {
            m_asyncActions.Add(action);
            Monitor.Pulse(m_asyncActions);
        }

        return new AnonymousDisposable(() => {
            lock (m_asyncActions)
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

    public IDisposable DispatchAfter(TimeSpan ts, Action action)
    { throw new NotImplementedException(); }

    void ThreadProc()
    {
        while (!m_isDisposed) {
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
