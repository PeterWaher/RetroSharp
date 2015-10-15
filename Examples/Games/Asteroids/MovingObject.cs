using System;
using System.Drawing;
using RetroSharp;

namespace Asteroids
{
	public abstract class MovingObject
	{
		public double X;
		public double Y;
		public double PrevX;
		public double PrevY;
		public double VelocityX;
		public double VelocityY;

		public MovingObject(double X, double Y, double VelocityX, double VelocityY)
		{
			this.X = this.PrevX = X;
			this.Y = this.PrevY = Y;
			this.VelocityX = VelocityX;
			this.VelocityY = VelocityY;
		}

		public virtual void Move(double ElapsedSeconds)
		{
			this.X += this.VelocityX * ElapsedSeconds;
			this.Y += this.VelocityY * ElapsedSeconds;

			if (this.X < -30)
				this.X += RetroApplication.RasterWidth + 60;

			if (this.X > RetroApplication.RasterWidth + 30)
				this.X -= RetroApplication.RasterWidth + 60;

			if (this.Y < -30)
				this.Y += RetroApplication.RasterHeight + 60;

			if (this.Y > RetroApplication.RasterHeight + 30)
				this.Y -= RetroApplication.RasterHeight + 60;
		}

		public abstract bool Draw(Color Color);
	}
}
