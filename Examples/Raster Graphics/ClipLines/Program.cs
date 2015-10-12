using System;
using System.Drawing;
using RetroSharp;

// This example uses the results from MoireLines to show how to clip lines against the borders of a bounding box.

namespace ClipLines
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
                DrawLine(i, 0, RasterWidth - 1, (3 * i) / 4, Color.DarkBlue);
                DrawLine(RasterWidth - i - 1, RasterHeight - 1, 0, RasterHeight - (3 * i) / 4 - 1, Color.DarkBlue);
            }

            double Rad;

            for (i = 0; i < 360; i++)
            {
                Rad = i * Math.PI / 180;
                DrawLine(320, 240, 320 + (int)Math.Round(150 * Math.Sin(Rad)), 240 + (int)Math.Round(150 * Math.Cos(Rad)), Color.DarkGreen);
            }

            int x1, y1, x2, y2;
            int Left = 50;
            int Top = 50;
            int Right = 590;
            int Bottom = 430;

            DrawRectangle(Left, Top, Right, Bottom, Color.Red);

            for (i = 0; i < RasterWidth; i += 3)
            {
                x1 = i;
                y1 = 0;
                x2 = RasterWidth - 1;
                y2 = (3 * i) / 4;

                if (ClipLine(ref x1, ref y1, ref x2, ref y2, Left, Top, Right, Bottom))
                    DrawLine(x1, y1, x2, y2, Color.Blue);

                x1 = RasterWidth - i - 1;
                y1 = RasterHeight - 1;
                x2 = 0;
                y2 = RasterHeight - (3 * i) / 4 - 1;

                if (ClipLine(ref x1, ref y1, ref x2, ref y2, Left, Top, Right, Bottom))
                    DrawLine(x1, y1, x2, y2, Color.Blue);
            }
            
            Left = 200;
            Top = 180;
            Right = 440;
            Bottom = 300;

            DrawRectangle(Left, Top, Right, Bottom, Color.Red);

            for (i = 0; i < 360; i++)
            {
                Rad = i * Math.PI / 180;

                x1 = 320;
                y1 = 240;
                x2 = 320 + (int)Math.Round(150 * Math.Sin(Rad));
                y2 = 240 + (int)Math.Round(150 * Math.Cos(Rad));

                if (ClipLine(ref x1, ref y1, ref x2, ref y2, Left, Top, Right, Bottom))
                    DrawLine(x1, y1, x2, y2, Color.Green);
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