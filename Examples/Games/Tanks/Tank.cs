using System;
using System.Collections.Generic;
using System.Text;
using RetroSharp;

namespace Tanks
{
	public abstract class Tank
	{
		protected Sprite sprite;
		protected double x;
		protected double y;
		protected double v;
		protected double a;

		public Tank(Sprite Sprite)
		{
			this.sprite = Sprite;
			this.x = this.sprite.X;
			this.y = this.sprite.Y;
			this.v = 0;
			this.a = 0;
		}

		public Sprite Sprite
		{
			get { return this.sprite; }
		}

		public double X
		{
			get { return this.x; }
		}

		public double Y
		{
			get { return this.y; }
		}

		public double V
		{
			get { return this.v; }
		}

		public double A
		{
			get { return this.a; }
		}

		public virtual void Move(double ElapsedSeconds)
		{
			this.v *= 0.95;
			if (this.v > 0)
				this.v -= this.v * this.v * ElapsedSeconds;
			else 
				this.v += this.v * this.v * ElapsedSeconds;
				
			if (this.a != 0)
				this.v += this.a;

			double phi = this.sprite.Angle * System.Math.PI / 180;
			double s = System.Math.Sin(phi);
			double c = System.Math.Cos(phi);

			ElapsedSeconds *= 60;
			this.x -= this.v * c * ElapsedSeconds;
			this.y -= this.v * s * ElapsedSeconds;

			this.sprite.SetPosition((int)(this.x + 0.5), (int)(this.y + 0.5));
		}
	}
}
