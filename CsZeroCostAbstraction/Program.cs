// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Console;

struct Seconds
{
    public readonly double Value;

    public Seconds(double x)
    { Value = x; }

    public static implicit operator Seconds(double x) => new Seconds(x);
}

struct MetresPerSecond
{
    public readonly double Value;
    public MetresPerSecond(double x)
    { Value = x; }

    public static implicit operator MetresPerSecond(double x) => new MetresPerSecond(x);
}

struct Metres
{
    public readonly double Value;
    public Metres(double x)
    { Value = x; }

    public static implicit operator Metres(double x) => new Metres(x);

    public static MetresPerSecond operator /(Metres m, Seconds s) => m.Value / s.Value;
}

class Program
{
    void Delay(Seconds howLong)
    {

    }

    void Travel(Metres howFar)
    {

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double DoMaths()
    {
        double tenKms = 10 * 1000;
        double oneHour = 60 * 60;

        return tenKms / oneHour;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static MetresPerSecond DoMathsWithStructs()
    {
        Metres tenKms = 10 * 1000;
        Seconds oneHour = 60 * 60;

        return tenKms / oneHour;
    }

    static void Main(string[] args)
    {
        // ----- Memory -----
        Console.WriteLine("Press enter to start memory stats");
        Console.ReadLine();

        var baseline = GC.GetTotalMemory(forceFullCollection: true);
        var darray = new double[1000000];
        var after = GC.GetTotalMemory(forceFullCollection: true);
        WriteLine("array of a million doubles is {0} bytes\n", after - baseline);

        baseline = GC.GetTotalMemory(forceFullCollection: true);
        var marray = new Metres[1000000];
        after = GC.GetTotalMemory(forceFullCollection: true);
        WriteLine("array of a million Metres is {0} bytes\n", after - baseline);
        
        GC.KeepAlive(darray);
        GC.KeepAlive(marray);

        // ----- Cpu -----
        Console.WriteLine("Press enter for maths");
        Console.ReadLine();

        for (int i = 0; i < 1000; i++) { // "warmup" JIT
            DoMaths();
            DoMathsWithStructs();
        }

        var sw = Stopwatch.StartNew();
        for(int i = 0; i < 50000000; i++)
            DoMaths();

        sw.Stop();
        Console.WriteLine($"raw maths took {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        for (int i = 0; i < 50000000; i++)
            DoMathsWithStructs();

        sw.Stop();
        Console.WriteLine($"struct maths took {sw.ElapsedMilliseconds}ms");
    }

}