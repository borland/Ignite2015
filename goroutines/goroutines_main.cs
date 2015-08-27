// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.Threading.Tasks;

class MainClass
{
    // the implementation of all the things is in Go.cs
    public static void Main(string[] args)
    {
        basic.Run().Wait();
        //crc_basic.Run();
        //crc_concurrent.Run().Wait();

        //FanOutIn().Wait();
    }

    public static async Task FanOutIn()
    {
        var numbers = new Channel<int>();
        var letters = new Channel<char>();

        Go.Run(async () => {
            for (int i = 0; i < 10; i++)
                await numbers.Send(i);

            Console.WriteLine("numbers all sent");
            numbers.Close();
        });

        Go.Run(async () => {
            for (int i = 0; i < 10; i++)
            {
                await letters.Send((char)(i + 97));
            }

            Console.WriteLine("letters all sent");
            letters.Close();
        });

        while(numbers.IsOpen || letters.IsOpen)
        {
            await Go.Select(
                Go.Case(numbers, num => {
                    Console.WriteLine($"Got {num}");
                }),
                Go.Case(letters, ch => {
                    Console.WriteLine($"Got {ch}");
                }));
        }
    }
}
