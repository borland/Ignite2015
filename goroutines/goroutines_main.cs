// Copyright(c) 2015 Orion Edwards
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Threading.Tasks;

class MainClass
{
    public static void Main(string[] args)
    {
        //basic.Run();
        //crc_basic.Run();
        //crc_concurrent.Run();

        FanOutIn().Wait();
    }

    public static async Task FanOutIn()
    {
        var numbers = new Channel<int>();
        var letters = new Channel<char>();

        Go.Run(async () => {
            for (int i = 0; i < 1; i++)
                await numbers.Send(i);

            Console.WriteLine("numbers all done");
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

            Console.WriteLine("letters all done");
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
