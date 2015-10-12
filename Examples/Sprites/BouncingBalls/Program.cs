using System.Threading;
using RetroSharp;

// This is a template for retro applications using a character-based screen by default.

namespace BouncingBalls
{
	[CharacterSet("Consolas", 256)]
	[Characters(80, 30, System.Drawing.KnownColor.LightGray, System.Drawing.KnownColor.Black)]
	[ScreenBorder(30, 20)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			ManualResetEvent Terminated = new ManualResetEvent(false);

			Initialize();

			WriteLine("Press ESC to close the application when running.");

			OnKeyDown += (sender, e) =>
			{
				if (e.Key == Key.Escape || (e.Key == Key.C && e.Control))
					Terminated.Set();
			};

			// Sound borrowed from: http://www.freesound.org/people/davidou/sounds/88451/
			int BoingSound = UploadAudioSample(GetResourceWavAudio("88451__davidou__boing.wav"));

			int BallTexture = AddSpriteTexture(GetResourceBitmap("Ball.png"), new System.Drawing.Point(25, 25), System.Drawing.Color.Black, true);
			System.Random Rnd = new System.Random();
			Ball[] Balls = new Ball[20];
			int i;

			for (i = 0; i < Balls.Length; i++)
			{
				Balls[i] = new Ball(
					CreateSprite(Rnd.Next(50, RasterWidth - 50), Rnd.Next(50, RasterHeight - 50), BallTexture), 
					Rnd.Next(500000) - 250000, BoingSound);
			}

			OnUpdateModel += (sender, e) =>
			{
				foreach (Ball Ball in Balls)
					Ball.Move();
			};

			while (!Terminated.WaitOne(1000))
				;

			Terminate();
		}
	}
}