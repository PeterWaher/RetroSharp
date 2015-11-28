using System;
using System.Drawing;
using RetroSharp;

namespace Koch
{
	[RasterGraphics(1280, 720)]
	[Characters(80, 35, KnownColor.White)]
	[ScreenBorder(30, 20, KnownColor.Gray)]
	[AspectRatio(16, 9)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			Initialize();

			DrawKochFlakeLine(640, 110, 857, 485, Color.White);
			DrawKochFlakeLine(857, 485, 423, 485, Color.White);
			DrawKochFlakeLine(423, 485, 640, 110, Color.White);

			FillFlood(640, 360, Color.Gray);

			Console.Out.WriteLine();
			Console.Out.WriteLine("Press ENTER to continue.");
			Console.In.ReadLine();

			Terminate();
		}

		private static void DrawKochFlakeLine(double x1, double y1, double x2, double y2, Color Color)
		{
			if (Math.Abs(x1 - x2) + Math.Abs(y1 - y2) < 1)
			{
				int x = (int)Math.Round((x1 + x2) / 2);
				int y = (int)Math.Round((y1 + y2) / 2);

				Raster[x, y] = Color;
			}
			else
			{
				double x3 = (2 * x1 + x2) / 3;
				double y3 = (2 * y1 + y2) / 3;
				double x4 = (x1 + 2 * x2) / 3;
				double y4 = (y1 + 2 * y2) / 3;

				double xm = (x1 + x2) / 2;
				double ym = (y1 + y2) / 2;
				double d = 1 / Math.Sqrt(3);
				double x5 = xm + d * (ym - y1);
				double y5 = ym - d * (xm - x1);

				DrawKochFlakeLine(x1, y1, x3, y3, Color);
				DrawKochFlakeLine(x3, y3, x5, y5, Color);
				DrawKochFlakeLine(x5, y5, x4, y4, Color);
				DrawKochFlakeLine(x4, y4, x2, y2, Color);
			}
		}

	}
}