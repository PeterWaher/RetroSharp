using System;
using System.Drawing;
using RetroSharp;

// This example draws random rectangles onto a raster bitmap display.

namespace RandomRectangles
{
    [RasterGraphics(320, 200)]
    [ScreenBorder(30, 20, System.Drawing.KnownColor.DimGray)]
    class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            bool Done = false;
            int x1, y1, x2, y2, r, g, b;

            OnKeyDown += (sender, e) => Done = true;

            while (!Done)
            {
                x1 = Random(RasterWidth);
                y1 = Random(RasterHeight);
                x2 = Random(RasterWidth);
                y2 = Random(RasterHeight);
                r = Random(256);
                g = Random(256);
                b = Random(256);

                DrawRectangle(x1, y1, x2, y2, Color.FromArgb(r, g, b));
            }

            Terminate();
        }

    }
}