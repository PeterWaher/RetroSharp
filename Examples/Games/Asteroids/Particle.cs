using System;
using System.Drawing;
using RetroSharp;

namespace Asteroids
{
	public class Particle : MovingObject
	{
		public Color Color;
		private double Lifetime;
		private double TotalTime;
		private double p = 1;

		public Particle(double X, double Y, double VelocityX, double VelocityY, Color Color, double Lifetime)
			: base(X, Y, VelocityX, VelocityY)
		{
			this.Color = Color;
			this.Lifetime = this.TotalTime = Lifetime;
		}

		public override void Move(double ElapsedSeconds)
		{
			base.Move(ElapsedSeconds);

			this.Lifetime -= ElapsedSeconds;
			this.p = this.Lifetime / this.TotalTime;
		}

		public override bool Draw(Color Color)
		{
			Color cl = RetroApplication.Blend(Color.Black, Color, p);

			int x = (int)(this.X + 0.5);
			int y = (int)(this.Y + 0.5);

			RetroApplication.Raster[x - 1, y] = cl;
			RetroApplication.Raster[x, y] = cl;
			RetroApplication.Raster[x + 1, y] = cl;
			RetroApplication.Raster[x, y - 1] = cl;
			RetroApplication.Raster[x, y + 1] = cl;

			return this.Lifetime >= 0 && x >= -1 && x <= RetroApplication.RasterWidth + 1 && y >= -1 && y <= RetroApplication.RasterHeight + 1;
		}
	}
}
