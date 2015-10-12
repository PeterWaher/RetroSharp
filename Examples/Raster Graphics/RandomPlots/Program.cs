using System.Drawing;
using RetroSharp;

// This example plots random points onto a raster graphics display.

namespace RandomPlots
{
    [RasterGraphics(320, 200)]
    [ScreenBorder(30, 20, System.Drawing.KnownColor.DimGray)]
    class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            bool Done = false;
            int x, y, r, g, b;

            OnKeyDown += (sender, e) => Done = true;

            while (!Done)
            {
                x = Random(RasterWidth);
                y = Random(RasterHeight);
                r = Random(256);
                g = Random(256);
                b = Random(256);

                Raster[x, y] = Color.FromArgb(r, g, b);
            }

            Terminate();
        }
    }
}