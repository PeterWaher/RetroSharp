using System.Drawing;
using RetroSharp;

// This example shows how direct access to screen can be used to create simple animated scenes
// using character graphics.

namespace BouncingCharacter1
{
    [CharacterSet("Consolas", 256)]
    [Characters(80, 30)]
    [ScreenBorder(30, 20)]
    class Program : RetroApplication
    {
        static void Main(string[] args)
        {
            Initialize();

            int BallX = 0;
            int BallY = 0;
            int DirX = 1;
            int DirY = 1;
            int NewX;
            int NewY;
            bool HitHorizontal;
            bool HitVertical;

            Screen[BallX, BallY] = 'O';
            Foreground[BallX, BallY] = Color.White;

            while (TotalTime < 20)  // Display bouncing ball for 20 seconds.
            {
                Sleep(50);

                NewX = BallX + DirX;
                NewY = BallY + DirY;

                HitVertical = false;
                HitHorizontal = false;

                if (NewY < 0 || NewY >= ConsoleHeight)
                    HitHorizontal = true;   // Top and bottom borders act as horizontal walls.

                if (NewX < 0 || NewX >= ConsoleWidth)
                    HitVertical = true;     // Left and right borders act as vertical walls.

                if (HitHorizontal)
                    DirY = -DirY;

                if (HitVertical)
                    DirX = -DirX;

                Screen[BallX, BallY] = ' ';
                BallX += DirX;
                BallY += DirY;
                Screen[BallX, BallY] = 'O';
            }

            Terminate();
        }

    }
}