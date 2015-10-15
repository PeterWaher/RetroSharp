using System;
using System.Drawing;
using RetroSharp;

namespace Asteroids
{
	public class RotatingObject : MovingObject
	{
		public double VelocityAngle;
		public double Angle;
		public Point[] Points;
		private int[] radiuses;
		private int[] angles;
		private int c;
		private int maxRadius;
		private bool recalc;

		public RotatingObject(int[] Radius, int[] Angles, double X, double Y, double VelocityX, double VelocityY, double Angle, double VelocityAngle)
			: base(X, Y, VelocityX, VelocityY)
		{
			this.radiuses = Radius;
			this.angles = Angles;

			c = Radius.Length;
			this.Points = new Point[c];
			this.recalc = true;

			this.Angle = Angle;
			this.VelocityAngle = VelocityAngle;

			this.maxRadius = 0;
			foreach (int R in Radius)
			{
				if (R > this.maxRadius)
					this.maxRadius = R;
			}
		}

		public override void Move(double ElapsedSeconds)
		{
			base.Move(ElapsedSeconds);

			this.Angle = Math.IEEERemainder(this.Angle + this.VelocityAngle * ElapsedSeconds, 360);
			this.recalc = true;
		}

		public void CalcPoints()
		{
			double ToRadians = Math.PI / 180.0;
			double r, a;
			int i;

			for (i = 0; i < c; i++)
			{
				r = radiuses[i];
				a = (angles[i] + Angle) * ToRadians;
				this.Points[i] = new Point((int)(r * Math.Cos(a) + X + 0.5), (int)(r * Math.Sin(a) + Y + 0.5));
			}

			this.recalc = false;
		}

		public override bool Draw(Color Color)
		{
			if (this.recalc)
				this.CalcPoints();

			RetroApplication.DrawPolygon(this.Points, Color);

			return true;
		}

		public bool Hits(double X, double Y)
		{
			X -= this.X;
			Y -= this.Y;

			if (Math.Abs(X) > this.maxRadius || Math.Abs(Y) > this.maxRadius)
				return false;

			return true;
		}
	}
}
