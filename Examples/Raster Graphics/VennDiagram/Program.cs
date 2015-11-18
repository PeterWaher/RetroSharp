using System;
using System.Drawing;
using RetroSharp;

namespace VennDiagram
{
	[RasterGraphics(1280, 720)]
	[ScreenBorder(30, 20, KnownColor.Gray)]
	[AspectRatio(16, 9)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			Initialize();

			DrawEllipse(640, 270, 200, 200, Color.White);
			DrawEllipse(540, 443, 200, 200, Color.White);
			DrawEllipse(740, 443, 200, 200, Color.White);

			FillFlood(640, 200, Color.FromArgb(255, 0, 0));
			FillFlood(500, 443, Color.FromArgb(0, 255, 0));
			FillFlood(800, 443, Color.FromArgb(0, 0, 255));

			FillFlood(500, 270, Color.FromArgb(255, 255, 0));
			FillFlood(800, 270, Color.FromArgb(255, 0, 255));
			FillFlood(640, 500, Color.FromArgb(0, 255, 255));

			FillFlood(640, 360, Color.White);

			Console.Out.WriteLine("Venn Diagram drawn using circles and flood fill operations.");
			Console.Out.WriteLine("Press ENTER to continue.");
			Console.In.ReadLine();

			Clear();

			FillEllipse(640, 270, 200, 200, Color.FromArgb(255, 0, 0));
			FillEllipse(540, 443, 200, 200, Add(Color.FromArgb(0, 255, 0)));
			FillEllipse(740, 443, 200, 200, Add(Color.FromArgb(0, 0, 255)));

			Console.Out.WriteLine("Venn Diagram drawn using procedural color algorithms.");
			Console.Out.WriteLine("Press ENTER to continue.");
			Console.In.ReadLine();

			Terminate();
		}
	}
}