using System;
using System.Threading;
using System.Drawing;
using RetroSharp;

namespace MousePointer
{
	[RasterGraphics(320, 200)]
	//[RasterGraphics(1280, 720)]
	[ScreenBorder(30, 20)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			ManualResetEvent Terminated = new ManualResetEvent(false);

			Initialize();

			Console.Out.WriteLine("Move the mouse to move the pointer on the screen.");
			Console.Out.WriteLine("Press left mouse button while moving to draw.");
			Console.Out.WriteLine("Press the ESC key to close the application.");

			OnKeyDown += (sender, e) =>
			{
				if (e.Key == Key.Escape || (e.Key == Key.C && e.Control))
					Terminated.Set();
			};

			FillRectangle(0, 0, ScreenWidth, ScreenHeight, C64Colors.Blue);

			int PointerTexture = AddSpriteTexture(GetResourceBitmap("Pointer.png"), System.Drawing.Color.FromArgb(0, 0, 255), true);
			Point P = GetMousePointer();
			Point LastP = P;
			Sprite Pointer = CreateSprite(P.X, P.Y, PointerTexture);
			bool Draw = false;

			OnMouseMove += (sender, e) =>
			{
				P = e.Position;
				Pointer.SetPosition(P);

				int DX = P.X - RasterWidth / 2;
				int DY = P.Y - RasterHeight / 2;

				Pointer.Angle = 90 + 22.5 + System.Math.Atan2(DY, DX) * 180 / System.Math.PI;

				if (Draw)
					DrawLine(LastP.X, LastP.Y, P.X, P.Y, Color.White);

				LastP = P;
			};

			OnMouseDown += (sender, e) =>
			{
				Draw = e.LeftButton;
			};

			OnMouseUp += (sender, e) =>
			{
				Draw = e.LeftButton;
			};

			while (!Terminated.WaitOne(1000))
				;

			Terminate();
		}
	}
}