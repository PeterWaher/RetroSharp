using System;
using System.Drawing;
using RetroSharp;

// This example draws lines onto a raster graphics display to demonstrate Moiré patterns.
// It also introduces the concept of Aspect Ratio of the screen.

namespace MoireLines
{
    [RasterGraphics(640, 480)]
    [ScreenBorder(30, 20, System.Drawing.KnownColor.DimGray)]
    [AspectRatio(4,3)]
    class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            int i;

            for (i = 0; i < RasterWidth; i += 3)
            {
                DrawLine(i, 0, RasterWidth - 1, (3 * i) / 4, Color.Blue);
                DrawLine(RasterWidth - i - 1, RasterHeight - 1, 0, RasterHeight - (3 * i) / 4 - 1, Color.Blue);
            }

            double Rad;

            for (i = 0; i < 360; i++)
            {
                Rad = i * Math.PI / 180;
                DrawLine(320, 240, 320 + (int)Math.Round(150 * Math.Sin(Rad)), 240 + (int)Math.Round(150 * Math.Cos(Rad)), Color.Green);
            }

            bool Done = false;

            OnKeyDown += (sender, e) => Done = true;

            while (!Done)
            {
                Sleep(10);
            }

            Terminate();
        }


    }
}