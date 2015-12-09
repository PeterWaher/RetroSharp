using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public class BouncingBall : Shot
	{
		public BouncingBall(int X, int Y, int VX, int VY, int FramesPerPixel, int StepsPerFrame, Color Color, int Power)
			: base(X, Y, VX, VY, FramesPerPixel, StepsPerFrame, Color, Power, false)
		{
		}

		public override bool MoveStep()
		{
			if (this.dead)
				return true;

			if (--this.framesLeft <= 0)
			{
				this.framesLeft = this.framesPerPixel;
				this.BeforeMove();

				this.x += this.vx;
				if (this.x < 0)
				{
					this.x = -this.x;
					this.vx = -this.vx;
				}
				else if (this.x >= this.w)
				{
					this.x = this.w - (this.x + 1 - this.w);
					this.vx = -this.vx;
				}

				this.y += this.vy;
				if (this.y < 8)
				{
					this.y = 16 - this.y;
					this.vy = -this.vy;
				}
				else if (this.y >= this.h)
				{
					this.y = this.h - (this.y + 1 - this.h);
					this.vy = -this.vy;
				}

				this.AfterMove();
			}

			return this.dead;
		}

	}
}
