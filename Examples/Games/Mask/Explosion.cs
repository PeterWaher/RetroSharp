using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public class Explosion : AnimatedObject
	{
		private Color color;
		private int xc;
		private int yc;
		private int expectedRadius;
		private int currentRadius;

		public Explosion(int X, int Y, int Radius, Color Color)
			: base(1)
		{
			this.color = Color;
			this.xc = X;
			this.yc = Y;
			this.expectedRadius = Radius;
			this.currentRadius = 0;

			RetroApplication.Raster[X, Y] = Color;
		}

		public override bool Move()
		{
			if (this.dead)
				return true;

			if (this.currentRadius >= this.expectedRadius)
			{
				this.dead = true;
				RetroApplication.FillEllipse(this.xc, this.yc, this.currentRadius, this.currentRadius, Color.Black);
			}
			else
			{
				this.currentRadius++;
				RetroApplication.FillEllipse(this.xc, this.yc, this.currentRadius, this.currentRadius,
					RetroApplication.Blend(this.color, Color.Black, ((double)this.currentRadius) / this.expectedRadius));
			}

			return this.dead;
		}

	}
}
