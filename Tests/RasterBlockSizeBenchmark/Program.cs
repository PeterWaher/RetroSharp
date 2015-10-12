using System.Drawing;
using RetroSharp;

// This example shows how to draw some lines on a raster graphics display.

namespace RasterBlockSizeBenchmark
{
    [RasterGraphics(640, 480)]
    [ScreenBorder(30, 20, System.Drawing.KnownColor.DimGray)]
    class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            long Pixels = 0;
            bool Done = false;
            int x, y, r, g, b;
            double RenderingTime = 0;

            OnKeyDown += (sender, e) =>
            {
                RenderingTime = RetroApplication.FrameRenderingTime;
                Done = true;
            };

            while (!Done)
            {
                x = Random(RasterWidth);
                y = Random(RasterHeight);
                r = Random(256);
                g = Random(256);
                b = Random(256);

                Raster[x, y] = Color.FromArgb(r, g, b);

                Pixels++;
            }

            DisplayMode = DisplayMode.Characters;

            WriteLine("Average frame rendering time: " + RenderingTime.ToString()+" ms");
            ReadLine();

            Terminate();
        }
    }
}