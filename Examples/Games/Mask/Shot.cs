using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public class Shot : MovingObject
	{
		private Color color;
		private int power;

		public Shot(int X, int Y, int VX, int VY, int FramesPerPixel, int StepsPerFrame, Color Color, int Power, bool Wrap)
			: base(X, Y, VX, VY, FramesPerPixel, StepsPerFrame, Wrap)
		{
			this.color = Color;
			this.power = Power;

			RetroApplication.Raster[X, Y] = Color;
		}

		public int Power { get { return this.power; } }

		public override void BeforeMove()
		{
			RetroApplication.Raster[this.x, this.y] = Color.Black;
		}

		public override void AfterMove()
		{
			if (RetroApplication.Raster[this.x, this.y].ToArgb() != Color.Black.ToArgb())
				this.Die();
			else
				RetroApplication.Raster[this.x, this.y] = this.color;
		}

	}
}
