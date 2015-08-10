// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.Diagnostics;
using System.Threading;

sealed class AnonymousDisposable : IDisposable
{
    Action m_action;

    public AnonymousDisposable(Action action)
    {
        Debug.Assert(action != null, "action must not be null");
        m_action = action;
    }

    public void Dispose() => Interlocked.Exchange(ref m_action, null)?.Invoke(); // only call once
}