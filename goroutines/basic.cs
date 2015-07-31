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

