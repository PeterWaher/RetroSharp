using System;
using System.Collections.Generic;
using System.Text;
using RetroSharp;

namespace BouncingBalls
{
	public class Ball
	{
		private Sprite sprite;
		private int x;
		private int y;
		private int vx;
		private int vy;
		private int minX = 25 << 16;
		private int maxX = (RetroApplication.RasterWidth - 25) << 16;
		private int maxY = (RetroApplication.RasterHeight - 25) << 16;
		private int boingSound;

		public Ball(Sprite Sprite, int VX, int BoingSound)
		{
			this.sprite = Sprite;
			this.x = this.sprite.X << 16;
			this.y = this.sprite.Y << 16;
			this.vx = VX;
			this.vy = 0;
			this.boingSound = BoingSound;
		}

		public void Move()
		{
			bool Boing = false;

			this.x += this.vx;
			this.y += this.vy;

			if (this.x < this.minX)
			{
				this.x = this.minX + (this.minX - this.x);
				this.vx = -this.vx;
				Boing = true;
			}
			else if (this.x > this.maxX)
			{
				this.x = this.maxX - (this.x - this.maxX);
				this.vx = -this.vx;
				Boing = true;
			}

			if (this.y > this.maxY)
			{
				this.y = this.maxY - (this.y - this.maxY);
				this.vy = -this.vy;
				Boing = true;
			}

			this.vy += 5000;

			this.sprite.SetPosition(this.x >> 16, this.y >> 16);

			if (Boing)
			{
				Console.Out.Write("Boing..");
				RetroApplication.PlayAudioSample(this.boingSound);
			}
		}
	}
}
