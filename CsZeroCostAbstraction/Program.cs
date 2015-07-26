using System;
using System.Diagnostics;
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

    static void Main(string[] args)
    {
        Metres tenKms = 10 * 1000;
        Seconds oneHour = 60 * 60;

        var speed = tenKms / oneHour;
        WriteLine($"{speed.Value} m/s");

        var baseline = GC.GetTotalMemory(forceFullCollection:true);
        var darray = new double[1000000];
        var after = GC.GetTotalMemory(forceFullCollection: true);
        WriteLine("array of a million doubles approx {0} bytes\n", after - baseline);

        baseline = GC.GetTotalMemory(forceFullCollection: true);
        var marray = new Metres[1000000];
        after = GC.GetTotalMemory(forceFullCollection: true);
        WriteLine("array of a million Metres is {0} bytes\n", after - baseline);

        baseline = GC.GetTotalMemory(forceFullCollection: true);
        var larray = new int[1000000];
        after = GC.GetTotalMemory(forceFullCollection: true);
        WriteLine("array of a million ints is {0} bytes\n", after - baseline);

        GC.KeepAlive(darray);
        GC.KeepAlive(marray);
        GC.KeepAlive(larray);

        ReadLine();

        //Delay(km1);
    }

}