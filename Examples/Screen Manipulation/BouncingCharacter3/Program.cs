using System.Drawing;
using RetroSharp;

// This example builds on BouncingCharacter2, and introduces user interaction, functions,
// events and lamda expressions.

namespace BouncingCharacter3
{
    [CharacterSet("Consolas", 256)]
    [Characters(80, 30)]
    [ScreenBorder(30, 20)]
    class Program : RetroApplication
    {
        static void Main(string[] args)
        {
            Initialize();

            WriteLine("Press ESC to close the application when running.");
            WriteLine("Press SPACE to redraw obstacles.");
            WriteLine();
            WriteLine("Now, press ENTER to start.");
            ReadLine();

            int BallX = 0;
            int BallY = 0;
            int DirX = 1;
            int DirY = 1;
            int NewX;
            int NewY;
            bool HitHorizontal;
            bool HitVertical;
            bool Done = false;

            Screen[BallX, BallY] = 'O';
            Foreground[BallX, BallY] = Color.White;

            Clear();
            DrawBars();

			OnKeyDown += (sender, e) =>
			{
				if (e.Key == Key.Escape || (e.Key == Key.C && e.Control))
					Done = true;
				else if (e.Key == Key.Space)
				{
					Clear();
					DrawBars();
				}
			};

            while (!Done)
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

                if (!HitHorizontal && !HitVertical)
                    CheckHits(NewX, NewY, ref HitHorizontal, ref HitVertical);

                if (HitVertical && HitHorizontal)
                {
                    // Hits both a vertical and horizontal wall. Reverse direction will always be possible.
                    DirX = -DirX;
                    DirY = -DirY;
                }
                else if (HitHorizontal)
                {
                    // Hits a horizontal wall. Flip Y direction and test if the new direction is possible.

                    DirY = -DirY;
                    NewY = BallY + DirY;
                    HitHorizontal = false;

                    if (NewY < 0 || NewY >= ConsoleHeight)
                        HitHorizontal = true;   // Top and bottom borders act as horizontal walls.
                    else
                        CheckHits(NewX, NewY, ref HitHorizontal, ref HitVertical);

                    if (HitHorizontal || HitVertical)   // New direction not possible. Reverse direction will always be possible.
                        DirX = -DirX;
                }
                else if (HitVertical)
                {
                    // Hits a vertical wall. Flip X direction and test if the new direction is possible.

                    DirX = -DirX;
                    NewX = BallX + DirX;
                    HitVertical = false;

                    if (NewX < 0 || NewX >= ConsoleWidth)
                        HitVertical = true;     // Left and right borders act as vertical walls.
                    else
                        CheckHits(NewX, NewY, ref HitHorizontal, ref HitVertical);

                    if (HitHorizontal || HitVertical)   // New direction not possible. Reverse direction will always be possible.
                        DirY = -DirY;
                }

                Screen[BallX, BallY] = ' ';
                BallX += DirX;
                BallY += DirY;
                Screen[BallX, BallY] = 'O';
                Foreground[BallX, BallY] = Color.White;
            }

            Terminate();
        }

        static void CheckHits(int NewX, int NewY, ref bool HitHorizontal, ref bool HitVertical)
        {
            char ch = Screen[NewX, NewY];

            switch (ch)
            {
                case '=':
                    HitHorizontal = true;
                    break;

                case '|':
                    HitVertical = true;
                    break;

                case '+':
                    HitHorizontal = true;
                    HitVertical = true;
                    break;
            }
        }

        static void DrawBars()
        {
            int a, b, c, i;

            // Draw some horizontal bars

            for (i = 0; i < 3; i++)
            {
                a = Random(10, 70);
                b = Random(10, 70);

                if (b < a)
                {
                    c = a;
                    a = b;
                    b = c;
                }

                c = Random(7, 25);

                while (a <= b)
                {
                    Screen[a, c] = '=';
                    Foreground[a, c] = Color.Yellow;
                    a++;
                }
            }

            // Draw some vertical bars

            for (i = 0; i < 3; i++)
            {
                a = Random(7, 25);
                b = Random(7, 25);

                if (b < a)
                {
                    c = a;
                    a = b;
                    b = c;
                }

                c = Random(10, 70);

                while (a <= b)
                {
                    if (Screen[c, a] == '=')
                        Screen[c, a] = '+';
                    else
                        Screen[c, a] = '|';

                    Foreground[c, a] = Color.Yellow;
                    a++;
                }
            }
        }

    }
}