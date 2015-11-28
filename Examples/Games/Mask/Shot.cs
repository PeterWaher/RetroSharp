using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public class Shot : MovingObject
	{
		private Color color;
		private int power;

		public Shot(int X, int Y, int VX, int VY, int FramesPerPixel, int StepsPerFrame, Color Color, int Power)
			: base(X, Y, VX, VY, FramesPerPixel, StepsPerFrame, false)
		{
			this.color = Color;
			this.power = Power;

			RetroApplication.Raster[X, Y] = Color;
		}

		public int Power { get { return this.power; } }

		protected override void BeforeMove()
		{
			RetroApplication.Raster[this.x, this.y] = Color.Black;
		}

		protected override void AfterMove()
		{
			if (RetroApplication.Raster[this.x, this.y].ToArgb() != Color.Black.ToArgb())
				this.Die();
			else
				RetroApplication.Raster[this.x, this.y] = this.color;
		}

	}
}
