// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


class basic
{
    static async Task MultiplyAsync(int a, int b, Channel<int> result)
        => await result.Send(a * b);

    public static async void Run()
    {
        var result = new Channel<int>();
        Go.Run(MultiplyAsync, 10, 20, result);
        Thread.Sleep(1000);
        Console.WriteLine($"result was {await result.Receive()}");
    }
}

