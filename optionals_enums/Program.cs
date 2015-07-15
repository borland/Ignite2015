using System;
using System.Diagnostics;
using System.IO;

namespace optionals_enums
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var x = Divide(6, 0);
            x.Switch(
                a => Console.WriteLine($"Result was {a}"),
                b => Console.WriteLine(b.Message));

            GetFileContents("c:\\temp\\test1.txt").Unwrap(
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

        static Optional<string> GetFileContents(string path)
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