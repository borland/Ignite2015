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
    public static extern IntPtr OpenResource(int id, [MarshalAs(UnmanagedType.LPStr)]string name);

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
        ptr = new MyHandle(OpenResource(id, name));
    }

    public void Use() => UseResource(ptr);

    public void Dispose() => ptr.Dispose();
}

class MyHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public MyHandle(IntPtr ptr) : base(true)
    {
        handle = ptr;
    }

    protected override bool ReleaseHandle()
    {
        CloseResource(handle);
        return true;
    }
}
