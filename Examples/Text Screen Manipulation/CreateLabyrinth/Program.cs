using System.Collections.Generic;
using System.Drawing;
using RetroSharp;

// This application creates a labyrinth on the screen.

namespace CreateLabyrinth
{
    [CharacterSet("Consolas", 256)]
    [Characters(80, 30)]
    [ScreenBorder(30, 20)]
    class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            // First part, generate labyrinth

            LinkedList<KeyValuePair<int, int>> Path = new LinkedList<KeyValuePair<int, int>>();
            int StartX, StartY;
            int x, y;
            int NrOptions;
            int Option;
            bool Up;
            bool Down;
            bool Left;
            bool Right;

            CustomizeCharacter('x',
                "xxxxxxxxx",
                "xxxxxxxxx",
                "xxxxxxxxx",
                "xxxxxxxxx",
                "xxxxxxxxx",
                "xxxxxxxxx",
                "xxxxxxxxx",
                "xxxxxxxxx",
                "xxxxxxxxx");

            for (y = 0; y < ConsoleHeight; y++)
            {
                for (x = 0; x < ConsoleWidth; x++)
                {
                    Screen[x, y] = 'x';
                }
            }

            x = StartX = Random(10, ConsoleWidth - 10);
            y = StartY = Random(10, ConsoleHeight - 10);
            Screen[x, y] = ' ';
            Path.AddLast(new KeyValuePair<int, int>(x, y));

            NrOptions = 4;
            Up = Down = Left = Right = true;

            while (NrOptions > 0)
            {
                Option = Random(NrOptions);

                if (Up)
                {
                    if (Option == 0)
                        y--;

                    Option--;
                }

                if (Down)
                {
                    if (Option == 0)
                        y++;

                    Option--;
                }

                if (Left)
                {
                    if (Option == 0)
                        x--;

                    Option--;
                }

                if (Right)
                {
                    if (Option == 0)
                        x++;

                    Option--;
                }

                Screen[x, y] = ' ';
                Path.AddLast(new KeyValuePair<int, int>(x, y));

                do
                {
                    Up = (y > 1 &&
                        Screen[x, y - 1] == 'x' && Screen[x - 1, y - 1] == 'x' && Screen[x + 1, y - 1] == 'x' &&
                        Screen[x, y - 2] == 'x' && Screen[x - 1, y - 2] == 'x' && Screen[x + 1, y - 2] == 'x');

                    Down = (y < ConsoleHeight - 2 &&
                        Screen[x, y + 1] == 'x' && Screen[x - 1, y + 1] == 'x' && Screen[x + 1, y + 1] == 'x' &&
                        Screen[x, y + 2] == 'x' && Screen[x - 1, y + 2] == 'x' && Screen[x + 1, y + 2] == 'x');

                    Left = (x > 1 &&
                        Screen[x - 1, y] == 'x' && Screen[x - 1, y - 1] == 'x' && Screen[x - 1, y + 1] == 'x' &&
                        Screen[x - 2, y] == 'x' && Screen[x - 2, y - 1] == 'x' && Screen[x - 2, y + 1] == 'x');

                    Right = (x < ConsoleWidth - 2 &&
                        Screen[x + 1, y] == 'x' && Screen[x + 1, y - 1] == 'x' && Screen[x + 1, y + 1] == 'x' &&
                        Screen[x + 2, y] == 'x' && Screen[x + 2, y - 1] == 'x' && Screen[x + 2, y + 1] == 'x');

                    NrOptions = 0;
                    if (Up) NrOptions++;
                    if (Down) NrOptions++;
                    if (Left) NrOptions++;
                    if (Right) NrOptions++;

                    if (NrOptions == 0)     // If no more options, retrace steps to see if other options are available.
                    {
                        Path.RemoveLast();
                        if (Path.Last is null)
                            break;  // Terminates the loop: while (NrOptions == 0).

                        x = Path.Last.Value.Key;
                        y = Path.Last.Value.Value;
                    }
                }
                while (NrOptions == 0);
            }

            // Second part, cusomize the display of the labyrinth

            int UpBit = 1;
            int DownBit = 2;
            int LeftBit = 4;
            int RightBit = 8;
            int Base = 'A';

            CustomizeCharacter(Base,
                "         ",
                "         ",
                "         ",
                "         ",
                "         ",
                "         ",
                "         ",
                "         ");

            CustomizeCharacter(Base + UpBit,
                "xxxxxxxxx",
                "         ",
                "         ",
                "         ",
                "         ",
                "         ",
                "         ",
                "         ");

            CustomizeCharacter(Base + DownBit,
                "         ",
                "         ",
                "         ",
                "         ",
                "         ",
                "         ",
                "         ",
                "xxxxxxxxx");

            CustomizeCharacter(Base + UpBit + DownBit,
                "xxxxxxxxx",
                "         ",
                "         ",
                "         ",
                "         ",
                "         ",
                "         ",
                "xxxxxxxxx");

            CustomizeCharacter(Base + LeftBit,
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ");

            CustomizeCharacter(Base + LeftBit + UpBit,
                "xxxxxxxxx",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ");

            CustomizeCharacter(Base + LeftBit + DownBit,
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "xxxxxxxxx");

            CustomizeCharacter(Base + LeftBit + UpBit + DownBit,
                "xxxxxxxxx",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "x        ",
                "xxxxxxxxx");

            CustomizeCharacter(Base + RightBit,
                "        x",
                "        x",
                "        x",
                "        x",
                "        x",
                "        x",
                "        x",
                "        x");

            CustomizeCharacter(Base + RightBit + UpBit,
                "xxxxxxxxx",
                "        x",
                "        x",
                "        x",
                "        x",
                "        x",
                "        x",
                "        x");

            CustomizeCharacter(Base + RightBit + DownBit,
                "        x",
                "        x",
                "        x",
                "        x",
                "        x",
                "        x",
                "        x",
                "xxxxxxxxx");

            CustomizeCharacter(Base + RightBit + UpBit + DownBit,
                "xxxxxxxxx",
                "        x",
                "        x",
                "        x",
                "        x",
                "        x",
                "        x",
                "xxxxxxxxx");

            CustomizeCharacter(Base + LeftBit + RightBit,
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x");

            CustomizeCharacter(Base + LeftBit + RightBit + UpBit,
                "xxxxxxxxx",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x");

            CustomizeCharacter(Base + LeftBit + RightBit + DownBit,
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "xxxxxxxxx");

            CustomizeCharacter(Base + LeftBit + RightBit + UpBit + DownBit,
                "xxxxxxxxx",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "x       x",
                "xxxxxxxxx");

            int Character;

            for (y = 0; y < ConsoleHeight; y++)
            {
                for (x = 0; x < ConsoleWidth; x++)
                {
                    if (Screen[x, y] == 'x')
                    {
                        Character = 0;

                        if (y == 0 || Screen[x, y - 1] == ' ')
                            Character |= UpBit;

                        if (y == ConsoleHeight - 1 || Screen[x, y + 1] == ' ')
                            Character |= DownBit;

                        if (x == 0 || Screen[x - 1, y] == ' ')
                            Character |= LeftBit;

                        if (x == ConsoleWidth - 1 || Screen[x + 1, y] == ' ')
                            Character |= RightBit;

                        Screen[x, y] = (char)(Base + Character);
                        Foreground[x, y] = Color.Brown;
                        Background[x, y] = Color.Orange;
                    }
                    else
                        Background[x, y] = Color.White;
                }
            }

            BorderColor = Color.Brown;

            // Third part, animate object transversing labyrinth

            x = StartX;
            y = StartY;
            bool Done = false;
            int PreviousDirection = -1;

            CustomizeCharacter('o',
                "   xxx   ",
                " xxxxxxx ",
                " xxxxxxx ",
                "xxxxxxxxx",
                "xxxxxxxxx",
                "xxxxxxxxx",
                " xxxxxxx ",
                " xxxxxxx ",
                "   xxx   ");

            Screen[x, y] = 'o';
            Foreground[x, y] = Color.Red;

            OnKeyDown += (sender, e) => Done = true;     // Any key will close the application

            while (!Done)
            {
                Sleep(40);

                NrOptions = 0;
                Up = Down = Left = Right = false;

                if (PreviousDirection != 1 && Screen[x, y - 1] == ' ')
                {
                    // Possible to go up if up is empty, and you didn't go down last step.
                    Up = true;
                    NrOptions++;
                }

                if (PreviousDirection != 0 && Screen[x, y + 1] == ' ')
                {
                    // Possible to go down if down is empty, and you didn't go up last step.
                    Down = true;
                    NrOptions++;
                }

                if (PreviousDirection != 3 && Screen[x - 1, y] == ' ')
                {
                    // Possible to go left if left is empty, and you didn't go right last step.
                    Left = true;
                    NrOptions++;
                }

                if (PreviousDirection != 2 && Screen[x + 1, y] == ' ')
                {
                    // Possible to go right if right is empty, and you didn't go left last step.
                    Right = true;
                    NrOptions++;
                }

                Screen[x, y] = ' ';

                if (NrOptions == 0)
                {
                    switch (PreviousDirection)
                    {
                        case 0: // Last up. Return down.
                            y++;
                            PreviousDirection = 1;
                            break;

                        case 1: // Last down. Return up.
                            y--;
                            PreviousDirection = 0;
                            break;

                        case 2: // Last left. Return right.
                            x++;
                            PreviousDirection = 3;
                            break;

                        case 3: // Last right. Return left.
                            x--;
                            PreviousDirection = 2;
                            break;
                    }
                }
                else
                {
                    Option = Random(NrOptions);

                    if (Up)
                    {
                        if (Option == 0)
                        {
                            PreviousDirection = 0;
                            y--;
                        }

                        Option--;
                    }

                    if (Down)
                    {
                        if (Option == 0)
                        {
                            PreviousDirection = 1;
                            y++;
                        }

                        Option--;
                    }

                    if (Left)
                    {
                        if (Option == 0)
                        {
                            PreviousDirection = 2;
                            x--;
                        }

                        Option--;
                    }

                    if (Right)
                    {
                        if (Option == 0)
                        {
                            PreviousDirection = 3;
                            x++;
                        }

                        Option--;
                    }
                }

                Screen[x, y] = 'o';
                Foreground[x, y] = Color.Red;
            }

            Terminate();
        }
    }
}