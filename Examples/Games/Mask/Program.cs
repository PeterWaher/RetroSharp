using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using RetroSharp;

// This is a template for retro applications using a raster graphics-based screen by default.

namespace Mask
{
	[RasterGraphics(320, 200)]
	[ScreenBorder(30, 20, KnownColor.DimGray)]
	[AspectRatio(4,3)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			Initialize();

			ManualResetEvent Done = new ManualResetEvent(false);
			LinkedList<Shot> Shots = new LinkedList<Shot>();
			LinkedList<Explosion> Explosions = new LinkedList<Explosion>();
			LinkedList<Present> Presents = new LinkedList<Present>();
			Player Player1 = new Player(20, 20, 1, 0, 3, Color.Green, Color.LightGreen);
			bool Player1Up = false;
			bool Player1Down = false;
			bool Player1Left = false;
			bool Player1Right = false;
			bool Player1Fire = false;
			int Player1Power = 15;

			OnKeyDown += (sender, e) =>
			{
				switch (e.Key)
				{
					case Key.Escape:
						Done.Set();
						break;

					case Key.C:
						if (e.Control)
							Done.Set();
						break;

					case Key.Up:
						if (!Player1.Dead)
							Player1Up = true;
						break;

					case Key.Down:
						if (!Player1.Dead)
							Player1Down = true;
						break;

					case Key.Left:
						if (!Player1.Dead)
							Player1Left = true;
						break;

					case Key.Right:
						if (!Player1.Dead)
							Player1Right = true;
						break;

					case Key.Space:
						if (!Player1.Dead)
							Player1Fire = true;
						break;
				}
			};

			OnUpdateModel += (sender, e) =>
			{
				if (Random() < 0.01)
				{
					int x1, y1;

					do
					{
						x1 = Random(30, 285);
						y1 = Random(30, 165);
					}
					while (!Present.CanPlace(x1, y1, x1 + 5, y1 + 5));

					Presents.AddLast(new Present(x1, y1, x1 + 5, y1 + 5));
				}

				LinkedListNode<Present> PresentObj, NextPresentObj;

				PresentObj = Presents.First;
				while (PresentObj != null)
				{
					NextPresentObj = PresentObj.Next;
					if (PresentObj.Value.Move())
						Presents.Remove(PresentObj);

					PresentObj = NextPresentObj;
				}

				if (Player1Up)
				{
					Player1.Up();
					Player1Up = false;
				}
				else if (Player1Down)
				{
					Player1.Down();
					Player1Down = false;
				}
				else if (Player1Left)
				{
					Player1.Left();
					Player1Left = false;
				}
				else if (Player1Right)
				{
					Player1.Right();
					Player1Right = false;
				}

				if (!Player1.Dead && Player1.Move())
					Explosions.AddLast(new Explosion(Player1.X, Player1.Y, 30, Color.White));

				if (Player1Fire)
				{
					Shots.AddLast(new Shot(Player1.X + Player1.VX, Player1.Y + Player1.VY, Player1.VX, Player1.VY, 1, Color.White, Player1Power));
					Player1Fire = false;
				}

				LinkedListNode<Shot> ShotObj, NextShotObj;

				ShotObj = Shots.First;
				while (ShotObj != null)
				{
					NextShotObj = ShotObj.Next;
					if (ShotObj.Value.Move())
					{
						Shots.Remove(ShotObj);
						Explosions.AddLast(new Explosion(ShotObj.Value.X, ShotObj.Value.Y, ShotObj.Value.Power, Color.White));
					}

					ShotObj = NextShotObj;
				}

				LinkedListNode<Explosion> ExplosionObj, NextExplosionObj;

				ExplosionObj = Explosions.First;
				while (ExplosionObj != null)
				{
					NextExplosionObj = ExplosionObj.Next;
					if (ExplosionObj.Value.Move())
						Explosions.Remove(ExplosionObj);

					ExplosionObj = NextExplosionObj;
				}

			};

			while (!Done.WaitOne(1000))
				;

			Terminate();
		}
	}
}