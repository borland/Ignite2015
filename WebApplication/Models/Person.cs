using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication.Models
{
    public class Person
    {
        public string Name { get; set; }
        public Optional<string> Address { get; set; }

        static Person[] s_people = new[]
        {
            new Person { Name = "Orion", Address = "Hamilton" },
            new Person { Name = "Anon" },
            new Person { Name = "Bill Gates", Address = "A mansion" },
        };

        public static Person[] LoadAll() => s_people;
    }
}