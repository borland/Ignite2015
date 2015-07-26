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
        //using (var tx = database.Transaction()) {
        //    tx.Execute("update people set name = 'Orion' where id = 5");
        //    tx.Commit();
        //}

        //var tx = database.Transaction();
        //tx.Execute("update people set name = 'Orion' where id = 5");
        //tx.Commit();

        IDatabase database = Connect("connectionString");
        database.Transaction(tx => {
            tx.Execute("update people set name = 'Orion' where id = 5");
            tx.Commit();
        });

        Server.Configure(config => {
            config.LogLevel = LogLevel.Debug;
            config.EagerLoad = true;
            config.Middleware.Use<ExceptionNotifier>();
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
