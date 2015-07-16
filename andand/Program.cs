using System;

namespace andand
{
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

	static class MainClass
	{
        class Person { public string Name => "Orion"; }

        static string ReadString() => null;//"A String";

        static void DeleteFile(string s) { }

		public static void Main(string[] args)
		{
            ReadString().AndAnd(s => DeleteFile(s));
        }
    }
}
