// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication.Models
{
    public class Person
    {
        public string Name { get; set; }
        public Nullable<int> Age { get; set; }
        public Optional<string> Address { get; set; }

        static Person[] s_people = new[]
        {
            new Person { Name = "Orion", Age = 33, Address = "Hamilton" },
            new Person { Name = "Anon" },
            new Person { Name = "Bill Gates", Age = 100, Address = "A mansion" },
        };

        public static Person[] LoadAll() => s_people;
    }
}