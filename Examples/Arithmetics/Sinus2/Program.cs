using System;
using System.Drawing;
using RetroSharp;

// This is a template for retro applications using a raster graphics-based screen by default.

namespace Sinus2
{
	[RasterGraphics(1280, 720)]
	[ScreenBorder(30, 20)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			Initialize();

			Console.Out.WriteLine("Sinus:");

			double x1, x2, y1, y2;

			DrawLine(60, 10, 60, 710, Color.White);
			DrawLine(60, 360, 1220, 360, Color.White);

			x1 = 60;
			y1 = 360;
			for (x2 = 61; x2 <= 1220; x2++)
			{
				y2 = System.Math.Sin((x2 - 60) * System.Math.PI * 4 / (1220 - 60)) * 350 + 360;

				DrawLine((int)(x2 + 0.5), (int)(y2 + 0.5), (int)(x1 + 0.5), (int)(y1 + 0.5), Color.Red);

				x1 = x2;
				y1 = y2;
			}

			Console.Out.WriteLine();
			Console.Out.WriteLine("Press ENTER to continue.");
			Console.In.ReadLine();

			Terminate();
		}
	}
}