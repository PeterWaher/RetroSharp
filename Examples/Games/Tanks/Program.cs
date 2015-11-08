using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using RetroSharp;

namespace Tanks
{
	[RasterGraphics(1280, 720)]
	[ScreenBorder(30, 20, KnownColor.Gray)]
	[AspectRatio(16,9)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			ManualResetEvent Terminated = new ManualResetEvent(false);

			Initialize();

			OnKeyDown += (sender, e) =>
			{
				if (e.Key == Key.Escape || (e.Key == Key.C && e.Control))
					Terminated.Set();
			};

			LinkedList<Particle> Particles = new LinkedList<Particle>();
			LinkedList<Particle> Shots = new LinkedList<Particle>();
			double ToRadians = Math.PI / 180;
			double d, s, c;
			int Tank1Texture = AddSpriteTexture(GetResourceBitmap("Graphics.jagtpanther.png"), new Point(38, 33), System.Drawing.Color.White, true);
			int Tank2Texture = AddSpriteTexture(GetResourceBitmap("Graphics.SU-85.png"), new Point(41, 32), System.Drawing.Color.White, true);
			Tank1 Tank1 = new Tank1(CreateSprite(100, 100, 180, Tank1Texture));
			Tank2 Tank2 = new Tank2(CreateSprite(1180, 620, 0, Tank2Texture));
			bool Left = false;
			bool Right = false;
			bool Forward = false;
			bool Backward = false;

			OnUpdateModel += (sender, e) =>
			{
				double ElapsedSeconds = e.Seconds;

				if (Left)
					Tank1.Left();

				if (Right)
					Tank1.Right();

				Tank1.Move(ElapsedSeconds);

				if (Forward)
				{
					d = (Random() * 45 - 15 + Tank1.Sprite.Angle) * ToRadians;    // Particle direction
					s = Math.Sin((Tank1.Sprite.Angle + 180) * ToRadians);
					c = Math.Cos((Tank1.Sprite.Angle + 180) * ToRadians);

					Particles.AddLast(new Particle(
						Tank1.Sprite.X - c * 26,  // X
						Tank1.Sprite.Y - s * 26,  // Y
						Math.Cos(d) * 30,                       // Velocity X
						Math.Sin(d) * 30,                       // Velocity Y
						Blend(Color.Yellow, Color.Orange, Random()),
						2));
				}

				LinkedListNode<Particle> ParticleNode = Particles.First;
				LinkedListNode<Particle> Next;
				Particle Particle;

				while (ParticleNode != null)
				{
					Particle = ParticleNode.Value;

					if (Particle.Draw(Color.Black))
					{
						Particle.Move(ElapsedSeconds);
						Particle.Draw(Particle.Color);
						ParticleNode = ParticleNode.Next;
					}
					else
					{
						Next = ParticleNode.Next;
						Particles.Remove(ParticleNode);
						ParticleNode = Next;
					}
				}
			};

			OnKeyDown += (sener, e) =>
			{
				switch (e.Key)
				{
 					case Key.Left:
						Left = true;
						break;

					case Key.Right:
						Right = true;
						break;

					case Key.Up:
						Forward = true;
						Tank1.Forward();
						break;

					case Key.Down:
						Backward = true;
						Tank1.Backward();
						break;

					case Key.Space:
						if (!e.IsRepeat)
							Tank1.Fire();
						break;
				}
			};

			OnKeyUp += (sender, e) =>
			{
				switch (e.Key)
				{
					case Key.Left:
						Left = false;
						break;

					case Key.Right:
						Right = false;
						break;

					case Key.Up:
						Forward = false;
						if (Backward)
							Tank1.Backward();
						else
							Tank1.NoForwardBackword();
						break;

					case Key.Down:
						Backward = false;
						if (Forward)
							Tank1.Forward();
						else
							Tank1.NoForwardBackword();
						break;
				}
			};

			while (!Terminated.WaitOne(1000))
				;

			Terminate();
		}
	}
}