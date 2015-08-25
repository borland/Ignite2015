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
            var file = "c:\\temp\\test1.txt";
            //ReadFile(file).

        }
        
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

        static int WordCount(string x) => x.Split(' ').Length;
    }
}