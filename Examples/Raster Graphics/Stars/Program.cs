using System;
using System.Drawing;
using System.Threading;
using RetroSharp;

namespace Stars
{
	[RasterGraphics(800, 450)]
	[ScreenBorder(30, 20, KnownColor.Gray)]
	[AspectRatio(16, 9)]
	class Program : RetroApplication
	{
		private const int NrStars = 1000;
		private static Random rnd = new Random();
		private static int d2;
		private static int xc;
		private static int yc;
		private static double angle = 0;
		private static double vangle = 0;
		private static double m11, m12, m21, m22;

		public static void Main(string[] args)
		{
			ManualResetEvent Terminated = new ManualResetEvent(false);
			int i;

			Initialize();

			ForegroundColor = Color.Gray;
			Console.Out.WriteLine("Press LEFT or RIGHT to rotate.");
			Console.Out.WriteLine("Press ESC to quit.");

			OnKeyDown += (sender, e) =>
			{
				if (e.Key == Key.Escape || (e.Key == Key.C && e.Control))
					Terminated.Set();
			};

			xc = RasterWidth / 2;
			yc = RasterHeight / 2;
			d2 = (xc + 1) * (xc + 1) + (yc + 1) * (yc + 1);

			Star[] Stars = new Star[NrStars];
			for (i = 0; i < NrStars; i++)
				Stars[i] = new Star();

			OnUpdateModel += (sender, e) =>
			{
				Star Star;

				if (IsPressed(KeyCode.Left))
					vangle -= 0.001;

				else if (IsPressed(KeyCode.Right))
					vangle += 0.001;

				angle += vangle;

				m11 = Math.Cos(angle);
				m12 = -Math.Sin(angle);
				m21 = Math.Sin(angle);
				m22 = Math.Cos(angle);

				for (i = 0; i < NrStars; i++)
				{
					Star = Stars[i];
					if (!Star.Move())
						Stars[i] = new Star();
				}
			};

			while (!Terminated.WaitOne(1000))
				;

			Terminate();
		}

		public class Star
		{
			public int ix, iy;
			public double x, y;
			public double vx, vy;
			public double ax, ay;
			public int intensity;

			public Star()
			{
				double a = rnd.NextDouble() * 2 * Math.PI;
				double d = rnd.NextDouble() * xc;
				double c = Math.Cos(a);
				double s = Math.Sin(a);

				this.ix = -1;
				this.iy = -1;

				this.x = d * c;
				this.y = d * s;

				d = rnd.NextDouble();
				this.vx = c * 0.1;
				this.vy = s * 0.1;

				this.ax = c * 0.05;
				this.ay = s * 0.05;

				this.intensity = 0;
			}

			public bool Move()
			{
				Raster[this.ix, this.iy] = Color.Black;

				this.vx += this.ax;
				this.x += this.vx;

				this.vy += this.ay;
				this.y += this.vy;

				this.ix = xc + (int)(m11 * this.x + m12 * this.y + 0.5);
				this.iy = yc + (int)(m21 * this.x + m22 * this.y + 0.5);

				if (this.intensity < 255)
				{
					this.intensity += 5;
					if (this.intensity > 255)
						this.intensity = 255;
				}

				Raster[this.ix, this.iy] = Color.FromArgb(this.intensity, this.intensity, this.intensity);

				return this.x * this.x + this.y * this.y <= d2;
			}
		}
	}
}