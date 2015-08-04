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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace optionals_enums
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            ReadFile("c:\\temp\\test1.txt").Unwrap(
                c => Console.WriteLine($"Word count is {WordCount(c)}"),
                () => Console.WriteLine("warning: no file"));
        }

        static Either<int, Exception> Divide(int a, int b)
        {
            if (b == 0)
                return new ArgumentException("can't divide by zero");

            return a / b;
        }

        static int WordCount(string x) =>  x.Split(' ').Length;

        static Optional<string> ReadFile(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                return File.ReadAllText(path);
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}