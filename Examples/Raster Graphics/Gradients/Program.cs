using System.Drawing;
using RetroSharp;

// This applications shows how custom coloring of graphical primitives can be done.

namespace Gradients
{
    [RasterGraphics(800, 600, KnownColor.PaleTurquoise)]
    [ScreenBorder(30, 20)]
    [AspectRatio(4,3)]
    class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            FillRectangle(10, 10, 200, 200, LinearGradient(10, 10, Color.Yellow, 200, 200, Color.Green));
            FillRectangle(210, 10, 400, 200, LinearGradient(250, 10, Color.Yellow, 360, 200, Color.Yellow, new ColorStop(0.5, Color.Green), new ColorStop(1.5, Color.Green)));

            FillRectangle(10, 210, 200, 400, RadialGradient(145, 265, Color.Yellow, 100, Color.Green));
            FillRectangle(210, 210, 400, 400, RadialGradient(345, 265, Color.Yellow, 100, Color.Yellow, new ColorStop(50, Color.Green), new ColorStop(150, Color.Green)));

            FillEllipse(105, 505, 95, 95, LinearGradient(10, 410, Color.Yellow, 400, 600, Color.Green));
            FillEllipse(305, 505, 95, 95, RadialGradient(345, 465, Color.Yellow, 100, Color.Green));

            Bitmap Star = GetResourceBitmap("Star.png");
            FillRectangle(410, 10, 600, 200, TextureFill(Star, -30, -80));

            FillRectangle(410, 210, 600, 400, TextureFill(Star, -30, -280));
            FillRectangle(450, 250, 560, 360, Blend(Color.Orange, 0.6));

            FillEllipse(505, 505, 95, 95, TextureFill(Star, -30, -50));

            FillRectangle(610, 10, 800, 200, Color.White);
            FillRectangle(620, 20, 790, 190, Xor(Color.White));
            FillRectangle(630, 30, 780, 180, Xor(Color.White));
            FillRectangle(640, 40, 770, 170, Xor(Color.White));
            FillRectangle(650, 50, 760, 160, Xor(Color.White));
            FillRectangle(660, 60, 750, 150, Xor(Color.White));

            FillRoundedRectangle(610, 210, 800, 400, 20, 20, Color.White);
            FillRoundedRectangle(620, 220, 790, 390, 20, 20, Xor(Color.White));
            FillRoundedRectangle(630, 230, 780, 380, 20, 20, Xor(Color.White));
            FillRoundedRectangle(640, 240, 770, 370, 20, 20, Xor(Color.White));
            FillRoundedRectangle(650, 250, 760, 360, 20, 20, Xor(Color.White));
            FillRoundedRectangle(660, 260, 750, 350, 20, 20, Xor(Color.White));

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