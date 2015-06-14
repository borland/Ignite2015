using System;

namespace andand
{
	static class MainClass
	{
		public static void Main(string[] args)
		{
			var result = GetBase()?.GetNext()?.GetNext();
			Console.WriteLine("{0}", result);

			Repeat(3, () => Console.WriteLine("Hello"));
		}

		static void Repeat(int n, Action action)
		{
			for(int i = 0; i < n; i++)
				action();
		}

		static string GetBase() => null; //"Dog";

		static string GetNext(this string x) {
			var a = x.ToCharArray();
			a[0] = (char)(a[0] + 1);
			return new string(a);
		}
	}
}
