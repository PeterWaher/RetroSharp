using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public class Present : AnimatedObject
	{
		private static Color color1 = Color.FromArgb(254, 255, 0, 0);
		private static Color color2 = Color.FromArgb(254, 255, 255, 0);
		private int x1, y1, x2, y2;
		private int p;

		public Present(int X1, int Y1, int X2, int Y2)
			: base(1)
		{
			this.x1 = X1;
			this.y1 = Y1;
			this.x2 = X2;
			this.y2 = Y2;

			this.p = 0;
			RetroApplication.FillRectangle(this.x1, this.y1, this.x2, this.y2, color1);
		}

		public static bool CanPlace(int X1, int Y1, int X2, int Y2)
		{
			int Black=Color.Black.ToArgb();
			int x, y;

			for (y = Y1; y <= Y2; y++)
			{
				for (x = X1; x <= X2; x++)
				{
					if (RetroApplication.Raster[x, y].ToArgb() != Black)
						return false;
				}
			}

			return true;
		}

		public override bool Move()
		{
			if (this.dead)
				return true;

			double p2;

			this.p++;
			if (this.p >= 60)
				this.p -= 60;

			if (this.p >= 30)
				p2 = 60 - this.p;
			else
				p2 = this.p;

			p2 /= 30;

			Color cl = RetroApplication.Blend(color1, color2, p2);
			Color cl2;
			int x, y;
			bool Found = false;

			for (y = this.y1; y <= this.y2; y++)
			{
				for (x = this.x1; x <= this.x2; x++)
				{
					cl2 = RetroApplication.Raster[x, y];
					if (cl2.A == 254)
					{
						Found = true;
						RetroApplication.Raster[x, y] = cl;
					}
				}
			}

			if (!Found)
				this.Die();

			return !Found;
		}

	}
}
