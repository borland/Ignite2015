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
using System.Linq;

static class MainClass
{
    class Person { public string Name => "Orion"; }

    static string ReadString() => "A String";

    static void DeleteFile(string s) { }

    public static void NotMain(string[] args)
    {
        ReadString().AndAnd(s => DeleteFile(s));
    }
}

static class MainClass_Tap
{
    static String ToString<T>(T item) => item.ToString();

    static List<double> GetItems => new List<double> { 1, 9, 2, 55, 3, 12 };

    static List<double> GetSortedItems => GetItems.Tap(x => x.Sort());

    static void NotMain(string[] args)
    {
        Console.WriteLine(string.Join(", ", GetSortedItems.Select(ToString)));
    }
}

static class MainClass_ScopeRestriction
{
    enum LogLevel { Debug }
    class ExceptionNotifier { }

    static void Main(string[] args)
    {
        IDatabase database = Connect("connectionString");
        database.Transaction(tx => {
            tx.Execute("update people set name = 'Orion' where id = 5");
            tx.Commit();
        });
    }

    static IDatabase Connect(string connectionString) => null;

    public interface IDatabase
    {
        ITransaction Transaction();
        void Transaction(Action<ITransaction> action);

        int Execute(string sql);
    }

    public interface ITransaction : IDisposable
    {
        int Execute(string sql);

        void Commit();
        void Abort();
    }
}


static class AndAndExtension
{
    public static void AndAnd<T>(this T x, Action<T> andand) where T : class
    {
        if (x != null)
            andand(x);
    }

    public static TResult AndAnd<T, TResult>(this T x, Func<T, TResult> andand) where T : class
    {
        if (x != null)
            return andand(x);
        return default(TResult);
    }
}

public static class TapExtension
{
    public static T Tap<T>(this T obj, Action<T> action)
    {
        action(obj);
        return obj;
    }
}
