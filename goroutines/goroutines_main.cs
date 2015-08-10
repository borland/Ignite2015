// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.Threading.Tasks;

class MainClass
{
    public static void Main(string[] args)
    {
        //basic.Run();
        //crc_basic.Run();
        //crc_concurrent.Run().Wait();

        FanOutIn().Wait();
    }

    public static async Task FanOutIn()
    {
        var numbers = new Channel<int>();
        var letters = new Channel<char>();

        Go.Run(async () => {
            for (int i = 0; i < 10; i++)
                await numbers.Send(i);

            Console.WriteLine("numbers all sent");
        });

        Go.Run(async () => {
            for (int i = 0; i < 10; i++)
            {
                var num = (char)(i % 52);
                if (num < 26)
                    await letters.Send((char)(num + 97));
                else
                    await letters.Send((char)(num + 65 - 26));
            }

            Console.WriteLine("letters all sent");
        });

        while(true)
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
