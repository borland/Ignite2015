// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static NativeMethods;

static class NativeMethods
{
    [DllImport("SafeHandleCpp.dll")]
    public static extern MyHandle OpenResource(int id, [MarshalAs(UnmanagedType.LPStr)]string name);

    [DllImport("SafeHandleCpp.dll")]
    public static extern void CloseResource(IntPtr resource);

    [DllImport("SafeHandleCpp.dll")]
    public static extern void UseResource(MyHandle resource);
}

class Program
{
    static void Main(string[] args)
    {
        var res = new Resource(5, "orion");
        res.Use();
    }
}

class Resource : IDisposable
{
    readonly MyHandle ptr;

    public Resource(int id, string name)
    {
        ptr = OpenResource(id, name);
    }

    public void Use() => UseResource(ptr);

    public void Dispose() => ptr.Dispose();
}

class MyHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private MyHandle() : base(true)
    {
    }
    
    protected override bool ReleaseHandle()
    {
        CloseResource(handle);
        return true;
    }
}
