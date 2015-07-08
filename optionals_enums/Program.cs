using System;

namespace optionals_enums
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var x = new Either<string,int>("foo");

			Console.WriteLine("Hello {0}", x.A);

			Console.ReadLine();
		}
	}
}
