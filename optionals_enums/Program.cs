// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
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