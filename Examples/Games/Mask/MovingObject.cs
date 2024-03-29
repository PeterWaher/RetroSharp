﻿using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public abstract class MovingObject : AnimatedObject
	{
		protected int x;
		protected int y;
		protected int vx;
		protected int vy;
		protected int w;
		protected int h;
		protected bool wrap;

		public MovingObject(int X, int Y, int VX, int VY, int FramesPerPixel, int StepsPerFrame, bool Wrap)
			: base(FramesPerPixel, StepsPerFrame)
		{
			this.x = X;
			this.y = Y;
			this.vx = VX;
			this.vy = VY;
			this.wrap = Wrap;

			this.w = RetroApplication.RasterWidth;
			this.h = RetroApplication.RasterHeight;
		}

		public int X { get { return this.x; } }
		public int Y { get { return this.y; } }
		public int VX { get { return this.vx; } }
		public int VY{ get { return this.vy; } }

		public void SetPosition(int X, int Y)
		{
			this.x = X;
			this.y = Y;
		}

		public void SetPosition(int X, int Y, int VX, int VY)
		{
			this.x = X;
			this.y = Y;
			this.vx = VX;
			this.vy = VY;
		}

		public override bool MoveStep()
		{
			if (this.dead)
				return true;

			if (--this.framesLeft <= 0)
			{
				this.framesLeft = this.framesPerPixel;
				this.BeforeMove();

				if (this.wrap)
				{
					if (this.vx != 0)
					{
						this.x += this.vx;

						if (this.x < 0)
							this.x += this.w;
						else if (this.x >= this.w)
							this.x -= this.w;
					}

					if (this.vy != 0)
					{
						this.y += this.vy;

						if (this.y < 8)
							this.y += (this.h - 8);
						else if (this.y >= this.h)
							this.y -= (this.h - 8);
					}
				}
				else
				{
					int i;

					i = this.x + this.vx;
					if (i < 0 || i >= this.w)
						this.Die();
					else
						this.x = i;

					i = this.y + this.vy;
					if (i < 8 || i >= this.h)
						this.Die();
					else
						this.y = i;
				}

				this.AfterMove();
			}

			return this.dead;
		}

		public abstract void BeforeMove();
		public abstract void AfterMove();

	}
}
