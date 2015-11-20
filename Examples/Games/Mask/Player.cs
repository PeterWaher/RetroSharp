using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public class Player : MovingObject
	{
		private Color color;
		private Color headColor;

		public Player(int X, int Y, int VX, int VY, int FramesPerPixel, Color Color, Color HeadColor)
			: base(X, Y, VX, VY, FramesPerPixel, true)
		{
			this.color = Color;
			this.headColor = HeadColor;

			RetroApplication.Raster[X, Y] = HeadColor;
		}

		protected override void BeforeMove()
		{
			RetroApplication.Raster[this.x, this.y] = this.color;
		}

		protected override void AfterMove()
		{
			if (RetroApplication.Raster[this.x, this.y].ToArgb() != Color.Black.ToArgb())
				this.Die();
			else
				RetroApplication.Raster[this.x, this.y] = this.headColor;
		}

		public void Up()
		{
			this.vx = 0;
			this.vy = -1;
		}

		public void Down()
		{
			this.vx = 0;
			this.vy = 1;
		}

		public void Left()
		{
			this.vx = -1;
			this.vy = 0;
		}

		public void Right()
		{
			this.vx = 1;
			this.vy = 0;
		}

	}
}
