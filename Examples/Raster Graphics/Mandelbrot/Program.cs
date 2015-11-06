using System;
using System.Threading;
using System.Drawing;
using RetroSharp;

// This is a template for retro applications using a raster graphics-based screen by default.

namespace Mandelbrot
{
	[RasterGraphics(1280, 720, KnownColor.Black)]
	[ScreenBorder(30, 20, KnownColor.Gray)]
	[AspectRatio(16, 9)]
	class Program : RetroApplication
	{
		//private const double LimitPercentChange = 0.0025;
		private const double LimitPercentChange = 0.00025;

		private static Random gen = new Random();
		private static ManualResetEvent Terminated = new ManualResetEvent(false);
		private static AutoResetEvent NewFractal = new AutoResetEvent(false);
		private static AutoResetEvent NewPalette = new AutoResetEvent(false);
		private static AutoResetEvent CalcSmooth = new AutoResetEvent(false);
		private static double[] ColorIndex;
		private static Sprite Pointer;
		private static bool Calculating = false;
		private static bool MandelbrotMode = true;
		private static int PointerTexture;
		private static int ZTexture;

		public static void Main(string[] args)
		{

			Initialize();

			Console.Out.WriteLine("Left-click on graph to zoom in.");
			Console.Out.WriteLine("Right-click on graph to toggle between Mandelbrot/Julia graphs.");
			Console.Out.WriteLine("Press the SPACE key to create a new palette.");
			Console.Out.WriteLine("Press the ENTER key to smooth the graph.");
			Console.Out.WriteLine("Press the cursor keys to scroll the graph up/down/left/right.");
			Console.Out.WriteLine("Press the ESC key to close the application.");
			Console.Out.WriteLine();
			Console.Out.WriteLine("Press ENTER to start.");
			Console.In.ReadLine();
			Clear();

			PointerTexture = AddSpriteTexture(GetResourceBitmap("Pointer.png"), System.Drawing.Color.FromArgb(0, 0, 255), true);
			ZTexture = AddSpriteTexture(GetResourceBitmap("Z.png"), System.Drawing.Color.FromArgb(0, 0, 255), true);
			Point P = GetMousePointer();
			Pointer = CreateSprite(P.X, P.Y, PointerTexture);

			double re0 = -0.5;
			double im0 = 0;
			double dr = 5;
			double cre0 = 0;
			double cim0 = 0;
			Color[] Palette = CreatePalette(1024, 16);

			OnKeyDown += (sender, e) =>
			{
				switch (e.Key)
				{
					case Key.Escape:
						Terminated.Set();
						break;

					case Key.C:
						if (e.Control)
							Terminated.Set();
						break;

					case Key.Space:
						NewPalette.Set();
						break;

					case Key.Enter:
						CalcSmooth.Set();
						break;

					case Key.Left:
						if (!Calculating)
						{
							re0 -= dr * 0.25;
							NewFractal.Set();
						}
						break;

					case Key.Right:
						if (!Calculating)
						{
							re0 += dr * 0.25;
							NewFractal.Set();
						}
						break;

					case Key.Up:
						if (!Calculating)
						{
							im0 -= dr * 0.25;
							NewFractal.Set();
						}
						break;

					case Key.Down:
						if (!Calculating)
						{
							im0 += dr * 0.25;
							NewFractal.Set();
						}
						break;
				}
			};

			OnMouseMove += (sender, e) =>
			{
				P = e.Position;
				Pointer.SetPosition(P);

				int DX = P.X - RasterWidth / 2;
				int DY = P.Y - RasterHeight / 2;

				Pointer.Angle = 90 + 22.5 + System.Math.Atan2(DY, DX) * 180 / System.Math.PI;
			};

			CalcMandelbrot(re0, im0, dr, Palette);

			OnMouseDown += (sender, e) =>
			{
				if (!Calculating)
				{
					if (e.LeftButton)
					{
						re0 = re0 + dr * (e.X - RasterWidth * 0.5) / RasterWidth;
						im0 = im0 + dr * (e.Y - RasterHeight * 0.5) / RasterWidth;	// Note: /RasterWidth, not /RasterHeight
						dr /= 4;
						NewFractal.Set();
					}
					else if (e.RightButton)
					{
						if (MandelbrotMode)
						{
							cre0 = re0 + dr * (e.X - RasterWidth * 0.5) / RasterWidth;
							cim0 = im0 + dr * (e.Y - RasterHeight * 0.5) / RasterWidth;	// Note: /RasterWidth, not /RasterHeight
							MandelbrotMode = false;
							re0 = 0;
							im0 = 0;
							dr = 5;
						}
						else
						{
							MandelbrotMode = true;
							re0 = -0.5;
							im0 = 0;
							dr = 5;
						}

						NewFractal.Set();
					}
				}
			};

			WaitHandle[] Handles = new WaitHandle[] { Terminated, NewFractal, NewPalette, CalcSmooth };
			bool Continue = true;

			while (Continue)
			{
				switch (WaitHandle.WaitAny(Handles, 60000))
				{
					case 0:
						Continue = false;
						break;

					case 1:
						if (MandelbrotMode)
							CalcMandelbrot(re0, im0, dr, Palette);
						else
							CalcJulia(re0, im0, cre0, cim0, dr, Palette);
						break;

					case 2:
						Palette = CreatePalette(1024, 16);
						if (MandelbrotMode)
							CalcMandelbrot(re0, im0, dr, Palette);
						else
							CalcJulia(re0, im0, cre0, cim0, dr, Palette);
						break;

					case 3:
						BeginCalculation();
						try
						{
							double[] Boundary = FindBoundaries(ColorIndex, RasterWidth, RasterHeight);
							Smooth(ColorIndex, Boundary, RasterWidth, RasterHeight, Palette.Length, Palette);
						}
						finally
						{
							EndCalculation();
						}
						break;
				}
			}

			Terminate();
		}

		private static void BeginCalculation()
		{
			Calculating = true;
			Pointer.SpriteTexture = ZTexture;
		}

		private static void EndCalculation()
		{
			Calculating = false;
			Pointer.SpriteTexture = PointerTexture;
		}

		public static void CalcMandelbrot(double rCenter, double iCenter, double rDelta, Color[] Palette)
		{
			int Width = RasterWidth;
			int Height = RasterHeight;
			double r0, i0, r1, i1;
			double dr, di;
			double r, i;
			double zr, zi, zrt, zr2, zi2;
			double aspect;
			int x, y;
			int n, N;
			int ci;

			BeginCalculation();
			try
			{
				N = Palette.Length;

				ColorIndex = new double[Width * Height];
				ci = 0;

				rDelta *= 0.5;
				r0 = rCenter - rDelta;
				r1 = rCenter + rDelta;

				aspect = ((double)Width) / Height;

				i0 = iCenter - rDelta / aspect;
				i1 = iCenter + rDelta / aspect;

				dr = (r1 - r0) / Width;
				di = (i1 - i0) / Height;

				for (y = 0, i = i0; y < Height; y++, i += di)
				{
					for (x = 0, r = r0; x < Width; x++, r += dr)
					{
						zr = r;
						zi = i;

						n = 0;
						zr2 = zr * zr;
						zi2 = zi * zi;

						while (zr2 + zi2 < 9 && n < N)
						{
							n++;
							zrt = zr2 - zi2 + r;
							zi = 2 * zr * zi + i;
							zr = zrt;

							zr2 = zr * zr;
							zi2 = zi * zi;
						}

						if (n >= N)
						{
							ColorIndex[ci++] = N;
							Raster[x, y] = Color.Black;
						}
						else
						{
							ColorIndex[ci++] = n;
							Raster[x, y] = Palette[n];
						}
					}
				}
			}
			finally
			{
				EndCalculation();
			}
		}

		public static void CalcJulia(double rCenter, double iCenter, double R0, double I0, double rDelta, Color[] Palette)
		{
			int Width = RasterWidth;
			int Height = RasterHeight;
			double r0, i0, r1, i1;
			double dr, di;
			double r, i;
			double zr, zi, zrt, zr2, zi2;
			double aspect;
			int x, y;
			int n, N;
			int ci;

			BeginCalculation();
			try
			{
				N = Palette.Length;

				ColorIndex = new double[Width * Height];
				ci = 0;

				rDelta *= 0.5;
				r0 = rCenter - rDelta;
				r1 = rCenter + rDelta;

				aspect = ((double)Width) / Height;

				i0 = iCenter - rDelta / aspect;
				i1 = iCenter + rDelta / aspect;

				dr = (r1 - r0) / Width;
				di = (i1 - i0) / Height;

				for (y = 0, i = i0; y < Height; y++, i += di)
				{
					for (x = 0, r = r0; x < Width; x++, r += dr)
					{
						zr = r;
						zi = i;

						n = 0;
						zr2 = zr * zr;
						zi2 = zi * zi;

						while (zr2 + zi2 < 9 && n < N)
						{
							n++;
							zrt = zr2 - zi2 + R0;
							zi = 2 * zr * zi + I0;
							zr = zrt;

							zr2 = zr * zr;
							zi2 = zi * zi;
						}

						if (n >= N)
						{
							ColorIndex[ci++] = N;
							Raster[x, y] = Color.Black;
						}
						else
						{
							ColorIndex[ci++] = n;
							Raster[x, y] = Palette[n];
						}
					}
				}
			}
			finally
			{
				EndCalculation();
			}
		}

		public static Color[] CreatePalette(int N, int BandSize)
		{
			return CreatePalette(N, BandSize, null);
		}

		public static Color[] CreatePalette(int N, int BandSize, int? Seed)
		{
			if (N <= 0)
				throw new ArgumentException("N in RandomLinearAnalogousHSV(N[,BandSize]) has to be positive.", "N");

			if (BandSize <= 0)
				throw new ArgumentException("BandSize in RandomLinearAnalogousHSV(N[,BandSize]) has to be positive.", "BandSize");

			Color[] Result = new Color[N];
			double H, S, V;
			int R1, G1, B1;
			int R2, G2, B2;
			int R, G, B;
			int i, j, c, d;
			int BandSize2 = BandSize / 2;
			Random Generator;

			if (Seed.HasValue)
				Generator = new Random(Seed.Value);
			else
				Generator = gen;

			lock (Generator)
			{
				H = Generator.NextDouble() * 360;
				S = Generator.NextDouble();
				V = Generator.NextDouble();

				HsvToRgb(H, S, V, out R2, out G2, out B2);

				i = 0;
				while (i < N)
				{
					R1 = R2;
					G1 = G2;
					B1 = B2;

					H += Generator.NextDouble() * 120 - 60;
					S = Generator.NextDouble();
					V = Generator.NextDouble();

					HsvToRgb(H, S, V, out R2, out G2, out B2);

					c = BandSize;
					j = N - i;
					if (c > j)
						c = j;

					d = N - i;
					if (d > c)
						d = c;

					for (j = 0; j < d; j++)
					{
						R = ((R2 * j) + (R1 * (BandSize - j)) + BandSize2) / BandSize;
						G = ((G2 * j) + (G1 * (BandSize - j)) + BandSize2) / BandSize;
						B = ((B2 * j) + (B1 * (BandSize - j)) + BandSize2) / BandSize;

						if (R > 255)
							R = 255;

						if (G > 255)
							G = 255;

						if (B > 255)
							B = 255;

						Result[i++] = Color.FromArgb(R, G, B);
					}
				}
			}

			return Result;
		}

		public static void HsvToRgb(double H, double S, double V, out int R, out int G, out int B)
		{
			// http://en.wikipedia.org/wiki/HSL_and_HSV#Conversion_from_HSV_to_RGB

			H = System.Math.IEEERemainder(H, 360);

			if (H < 0)
				H += 360;
			else if (H >= 360)
				H -= 360;

			if (S < 0)
				S = 0;
			else if (S > 1)
				S = 1;

			if (V < 0)
				V = 0;
			else if (V > 1)
				V = 1;

			H /= 60;
			int hi = (int)H;
			double f = H - hi;
			double p = V * (1 - S);
			double q = V * (1 - f * S);
			double t = V * (1 - (1 - f) * S);

			switch (hi)
			{
				case 0:
					R = (int)(V * 255 + 0.5);
					G = (int)(t * 255 + 0.5);
					B = (int)(p * 255 + 0.5);
					break;

				case 1:
					R = (int)(q * 255 + 0.5);
					G = (int)(V * 255 + 0.5);
					B = (int)(p * 255 + 0.5);
					break;

				case 2:
					R = (int)(p * 255 + 0.5);
					G = (int)(V * 255 + 0.5);
					B = (int)(t * 255 + 0.5);
					break;

				case 3:
					R = (int)(p * 255 + 0.5);
					G = (int)(q * 255 + 0.5);
					B = (int)(V * 255 + 0.5);
					break;

				case 4:
					R = (int)(t * 255 + 0.5);
					G = (int)(p * 255 + 0.5);
					B = (int)(V * 255 + 0.5);
					break;

				case 5:
					R = (int)(V * 255 + 0.5);
					G = (int)(p * 255 + 0.5);
					B = (int)(q * 255 + 0.5);
					break;

				default:
					R = G = B = 0;
					break;
			}
		}

		public static void Smooth(double[] ColorIndex, double[] Boundary, int Width, int Height, int N, Color[] Palette)
		{
			// Passing ColorIndex through the heat equation of 2 spatial dimensions, 
			// maintaining the boundary values fixed in each step.
			//
			// du       ( d2u   d2u )
			// -- = a * | --- + --- |
			// dt       ( dx2   dy2 )
			//
			// the following difference equations will be used to estimate the derivatives:
			//
			//         f(x+h)-2f(x)+f(x-h)
			// f"(x) = ------------------- + O(h^2)
			//                 h^2
			//
			// at the edges, we let f"(x)=0.

			int Size = Width * Height;
			double[] Delta = new double[Size];
			double uxx, uyy;
			double d;
			int Iterations = 0;
			int Index;
			int x, y;
			int DynamicPixels = Size;
			double Sum = Size;
			System.DateTime LastPreview = System.DateTime.Now;
			System.DateTime TP;

			for (Index = 0; Index < Size; Index++)
			{
				if (Boundary[Index] >= 0 || ColorIndex[Index] >= N)
					DynamicPixels--;
			}

			System.DateTime Start = System.DateTime.Now;
			System.TimeSpan Limit = new System.TimeSpan(1, 0, 0);

			while (100 * Sum / DynamicPixels > LimitPercentChange && Iterations < 50000 && (System.DateTime.Now - Start) < Limit)
			{
				Sum = 0;

				for (y = Index = 0; y < Height; y++)
				{
					for (x = 0; x < Width; x++)
					{
						d = Boundary[Index];
						if (d >= 0)
						{
							Delta[Index++] = 0;
							continue;
						}

						d = 2 * ColorIndex[Index];
						if (x == 0 || x == Width - 1)
							uxx = 0;
						else
							uxx = ColorIndex[Index - 1] - d + ColorIndex[Index + 1];

						if (y == 0 || y == Height - 1)
							uyy = 0;
						else
							uyy = ColorIndex[Index - Width] - d + ColorIndex[Index + Width];

						d = 0.2 * (uxx + uyy);
						Delta[Index++] = d;
						Sum += Math.Abs(d);
					}
				}

				for (Index = 0; Index < Size; Index++)
					ColorIndex[Index] += Delta[Index];

				Iterations++;

				TP = System.DateTime.Now;
				if ((TP - LastPreview).TotalSeconds > 5)
				{
					LastPreview = TP;
					DisplayColorIndices(ColorIndex, Width, Height, Palette);
				}
			}

			DisplayColorIndices(ColorIndex, Width, Height, Palette);
		}

		public static double[] FindBoundaries(double[] ColorIndex, int Width, int Height)
		{
			// Finding boundary values:

			double[] Boundary = (double[])ColorIndex.Clone();
			double d, d2;
			int Index;
			int x, y;

			Index = 0;

			d = ColorIndex[0];

			d2 = ColorIndex[1];
			if (d <= d2 && d > d2 - 2)
			{
				d2 = ColorIndex[Width];
				if (d <= d2 && d > d2 - 2)
					Boundary[0] = -1;
			}

			Index++;

			for (x = 2; x < Width; x++, Index++)
			{
				d = ColorIndex[Index];

				d2 = ColorIndex[Index + 1];
				if (d > d2 || d <= d2 - 2)
					continue;

				d2 = ColorIndex[Index - 1];
				if (d > d2 || d <= d2 - 2)
					continue;

				d2 = ColorIndex[Index + Width];
				if (d > d2 || d <= d2 - 2)
					continue;

				Boundary[Index] = -1;
			}

			d2 = ColorIndex[Index];
			if (d <= d2 && d > d2 - 2)
			{
				d2 = ColorIndex[Index - 1];
				if (d <= d2 && d > d2 - 2)
				{
					d2 = ColorIndex[Index + Width];
					if (d <= d2 && d > d2 - 2)
						Boundary[0] = -1;
				}
			}

			Index++;

			for (y = 2; y < Height; y++)
			{
				d = ColorIndex[Index];

				d2 = ColorIndex[Index + 1];
				if (d <= d2 && d > d2 - 2)
				{
					d2 = ColorIndex[Index - Width];
					if (d <= d2 && d > d2 - 2)
					{
						d2 = ColorIndex[Index + Width];
						if (d <= d2 && d > d2 - 2)
							Boundary[0] = -1;
					}
				}

				Index++;

				for (x = 2; x < Width; x++, Index++)
				{
					d = ColorIndex[Index];

					d2 = ColorIndex[Index + 1];
					if (d > d2 || d <= d2 - 2)
						continue;

					d2 = ColorIndex[Index - 1];
					if (d > d2 || d <= d2 - 2)
						continue;

					d2 = ColorIndex[Index + Width];
					if (d > d2 || d <= d2 - 2)
						continue;

					d2 = ColorIndex[Index - Width];
					if (d > d2 || d <= d2 - 2)
						continue;

					Boundary[Index] = -1;
				}

				d = ColorIndex[Index];

				d2 = ColorIndex[Index - 1];
				if (d <= d2 && d > d2 - 2)
				{
					d2 = ColorIndex[Index - Width];
					if (d <= d2 && d > d2 - 2)
					{
						d2 = ColorIndex[Index + Width];
						if (d <= d2 && d > d2 - 2)
							Boundary[0] = -1;
					}
				}

				Index++;
			}

			d = ColorIndex[Index];

			d2 = ColorIndex[Index + 1];
			if (d <= d2 && d > d2 - 2)
			{
				d2 = ColorIndex[Index - Width];
				if (d <= d2 && d > d2 - 2)
					Boundary[0] = -1;
			}

			Index++;

			for (x = 2; x < Width; x++, Index++)
			{
				d = ColorIndex[Index];

				d2 = ColorIndex[Index + 1];
				if (d > d2 || d <= d2 - 2)
					continue;

				d2 = ColorIndex[Index - 1];
				if (d > d2 || d <= d2 - 2)
					continue;

				d2 = ColorIndex[Index - Width];
				if (d > d2 || d <= d2 - 2)
					continue;

				Boundary[Index] = -1;
			}

			d = ColorIndex[Index];

			d2 = ColorIndex[Index - 1];
			if (d <= d2 && d > d2 - 2)
			{
				d2 = ColorIndex[Index - Width];
				if (d <= d2 && d > d2 - 2)
					Boundary[0] = -1;
			}

			return Boundary;
		}


		public static void DisplayColorIndices(double[] ColorIndex, int Width, int Height, Color[] Palette)
		{
			int N = Palette.Length;
			int Size = Width * Height;
			byte[] reds;
			byte[] greens;
			byte[] blues;
			double d;
			Color cl;
			int ci;
			int R, G, B;
			int Index;
			int x, y;

			reds = new byte[N];
			greens = new byte[N];
			blues = new byte[N];

			for (x = 0; x < N; x++)
			{
				cl = Palette[x];
				reds[x] = cl.R;
				greens[x] = cl.G;
				blues[x] = cl.B;
			}

			Index = 0;
			for (y = 0; y < Height; y++)
			{
				for (x = 0; x < Width; x++)
				{
					d = ColorIndex[Index++];

					ci = (int)d;
					if (ci < 0 || ci >= N)
						Raster[x, y] = Color.Black;
					else if (ci == N - 1)
						Raster[x, y] = Palette[ci];
					else
					{
						d -= ci;

						R = (int)(reds[ci + 1] * d + reds[ci] * (1 - d) + 0.5);
						G = (int)(greens[ci + 1] * d + greens[ci] * (1 - d) + 0.5);
						B = (int)(blues[ci + 1] * d + blues[ci] * (1 - d) + 0.5);

						Raster[x, y] = Color.FromArgb(R > 255 ? 255 : R, G > 255 ? 255 : G, B > 255 ? 255 : B);
					}

				}
			}
		}

	}
}