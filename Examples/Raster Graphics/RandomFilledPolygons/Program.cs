using System;
using System.Drawing;
using RetroSharp;

namespace RandomFilledPolygons
{
	[RasterGraphics(320, 200)]
	[ScreenBorder(30, 20, System.Drawing.KnownColor.DimGray)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			Initialize();

			bool Done = false;
			int i, c, x, y, r, g, b;
			Point[] Nodes;

			OnKeyDown += (sender, e) => Done = true;

			while (!Done)
			{
				c = Random(3, 10);

				Nodes = new Point[c];
				for (i = 0; i < c; i++)
				{
					x = Random(RasterWidth);
					y = Random(RasterHeight);
					Nodes[i] = new Point(x, y);
				}

				r = Random(256);
				g = Random(256);
				b = Random(256);

				FillPolygon(Nodes, Color.FromArgb(r, g, b));
			}

			Terminate();
		}
	}
}