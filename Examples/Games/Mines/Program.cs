using System;
using System.Threading;
using System.Drawing;
using RetroSharp;

// This is a template for retro applications using a character-based screen by default.

namespace Mines
{
	[CharacterSet("Consolas", 256)]
	[Characters(50, 25, KnownColor.White, KnownColor.Black)]
	[ScreenBorder(30, 20, KnownColor.DarkGray)]
	[AspectRatio(16, 9)]
	class Program : RetroApplication
	{
		private static bool[,] Mines;
		private static Random rnd = new System.Random();
		private static int i, x, y;
		private static bool Dead = false;

		public static void Main(string[] args)
		{
			ManualResetEvent Terminated = new ManualResetEvent(false);

			Initialize();

			Console.Out.WriteLine("Welcome to mines.");
			Console.Out.WriteLine();
			Console.Out.WriteLine("Left-click on squares to clear them.");
			Console.Out.WriteLine("If they contains mines, you loose.");
			Console.Out.WriteLine();
			Console.Out.WriteLine("Right-click on squares to mark them");
			Console.Out.WriteLine("as suspected mine site.");
			Console.Out.WriteLine("You need to find all mines.");
			Console.Out.WriteLine();
			Console.Out.WriteLine("Press the ESC key to close the application.");
			Console.Out.WriteLine();
			Console.Out.WriteLine("Press ENTER to start.");

			OnKeyDown += (sender, e) =>
			{
				if (e.Key == Key.Escape || (e.Key == Key.C && e.Control))
					Terminated.Set();
			};

			Console.In.ReadLine();

			CustomizeCharacter('x',
				"xxxxxxxxx",
				"x       x",
				"x       x",
				"x       x",
				"x       x",
				"x       x",
				"x       x",
				"xxxxxxxxx");

			CustomizeCharacter('M',
				"xxxxxxxxx",
				"x       x",
				"x x   x x",
				"x xx xx x",
				"x x x x x",
				"x x   x x",
				"x       x",
				"xxxxxxxxx");

			for (y = 0; y < ScreenHeight; y++)
			{
				for (x = 0; x < ScreenWidth; x++)
				{
					Screen[x, y] = 'x';
					Foreground[x, y] = Color.LightGray;
					Background[x, y] = Color.Gray;
				}
			}

			Mines = new bool[ScreenWidth, ScreenHeight];

			for (i = 0; i < 200; i++)
			{
				do
				{
					x = rnd.Next(0, ConsoleWidth);
					y = rnd.Next(0, ConsoleHeight);
				}
				while (Mines[x, y]);

				Mines[x, y] = true;
			}

			int PointerTexture = AddSpriteTexture(GetResourceBitmap("Pointer.png"), System.Drawing.Color.FromArgb(0, 0, 255), true);
			Point P = GetMousePointer();
			Sprite Pointer = CreateSprite(P.X, P.Y, PointerTexture);

			OnMouseMove += (sender, e) =>
			{
				P = e.Position;
				Pointer.SetPosition(e.Position);

				int DX = P.X - RasterWidth / 2;
				int DY = P.Y - RasterHeight / 2;

				Pointer.Angle = 90 + 22.5 + System.Math.Atan2(DY, DX) * 180 / System.Math.PI;
			};

			OnMouseDown += (sender, e) =>
			{
				if (!Dead)
				{
					if (e.LeftButton)
					{
						x = (e.X * ConsoleWidth) / RasterWidth;
						y = (e.Y * ConsoleHeight) / RasterHeight;

						Reveal(x, y);
					}
					else if (e.RightButton)
					{
						x = (e.X * ConsoleWidth) / RasterWidth;
						y = (e.Y * ConsoleHeight) / RasterHeight;

						if (Screen[x, y] == 'x')
							Screen[x, y] = 'M';
					}

				}
			};

			do
			{
				x = rnd.Next(0, ConsoleWidth);
				y = rnd.Next(0, ConsoleHeight);
			}
			while (Mines[x, y] || NrMinesAround(x, y) > 0);

			Reveal(x, y);

			while (!Terminated.WaitOne(60000))
				;

			Terminate();
		}

		private static int NrMinesAround(int x, int y)
		{
			i = 0;

			for (int dy = -1; dy <= 1; dy++)
			{
				for (int dx = -1; dx <= 1; dx++)
				{
					if (x + dx >= 0 && x + dx < ConsoleWidth && y + dy >= 0 && y + dy < ConsoleHeight && Mines[x + dx, y + dy])
						i++;
				}
			}

			return i;
		}

		private static void Reveal(int x, int y)
		{
			if (Mines[x, y])
			{
				Screen[x, y] = '�';
				Background[x, y] = Color.LightGray;
				Foreground[x, y] = Color.Gray;
				Dead = true;
			}
			else
			{
				i = NrMinesAround(x, y);

				if (i == 0)
					Screen[x, y] = ' ';
				else
					Screen[x, y] = (char)('0' + i);

				Background[x, y] = Color.LightGray;

				switch (i)
				{
					case 0:
						for (int dy = -1; dy <= 1; dy++)
						{
							for (int dx = -1; dx <= 1; dx++)
							{
								if (x + dx >= 0 && x + dx < ConsoleWidth && y + dy >= 0 && y + dy < ConsoleHeight &&
									(dx != 0 || dy != 0) && Screen[x + dx, y + dy] == 'x')
								{
									Reveal(x + dx, y + dy);
								}
							}
						}
						break;

					case 1:
						Foreground[x, y] = Color.Blue;
						break;

					case 2:
						Foreground[x, y] = Color.Green;
						break;

					case 3:
						Foreground[x, y] = Color.Red;
						break;

					case 4:
						Foreground[x, y] = Color.Navy;
						break;

					case 5:
						Foreground[x, y] = Color.Brown;
						break;

					case 6:
						Foreground[x, y] = Color.Magenta;
						break;

					case 7:
						Foreground[x, y] = Color.DarkCyan;
						break;

					case 8:
						Foreground[x, y] = Color.Black;
						break;
				}
			}
		}
	}
}