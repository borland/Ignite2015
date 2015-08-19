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
using static System.Console;

class basic
{
    static async Task MultiplyAsync(int a, int b, Channel<int> result, Channel<string> messages)
    {
        await result.Send(a * b);
        await messages.Send("ok");
        Console.WriteLine("mutliply done");
    }

    public static async Task Run()
    {
        var result = new Channel<int>();
        var messages = new Channel<string>();

        Go.Run(MultiplyAsync, 10, 20, result, messages);

        WriteLine($"result was {await result.Receive()}");
        WriteLine($"message was {await messages.Receive()}");
    }
}