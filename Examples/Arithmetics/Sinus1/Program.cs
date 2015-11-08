using System;
using System.Drawing;
using RetroSharp;

namespace Sinus1
{
	[CharacterSet("Consolas", 256)]
	[Characters(80, 43, KnownColor.White)]
	[ScreenBorder(30, 20)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			Initialize();

			double a, v;

			Console.Out.WriteLine("Sinus:");
			Console.Out.WriteLine();

			for (a=0;a<System.Math.PI * 2; a+=0.1)
			{
				v = System.Math.Sin(a);
				a += 0.1;

				v *= 35;
				v += 40.5;

				Console.Out.Write(new string(' ', (int)v));
				Console.Out.WriteLine("*");
			}

			Console.Out.WriteLine();
			Console.Out.WriteLine("Press ENTER to continue.");

			Console.In.ReadLine();

			Terminate();
		}
	}
}