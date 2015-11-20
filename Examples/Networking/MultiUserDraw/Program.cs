using System;
using System.Drawing;
using System.Threading;
using RetroSharp;
using RetroSharp.Networking;
using RetroSharp.Networking.MQTT;

namespace MultiUserDraw
{
	[RasterGraphics(1280, 720)]
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
			Console.Out.WriteLine("You will be able to see what others draw as well.");

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
			Random Rnd = new System.Random();
			int R = Rnd.Next(128, 255);
			int G = Rnd.Next(128, 255);
			int B = Rnd.Next(128, 255);
			Color Color = Color.FromArgb(R, G, B);
			bool Draw = false;
			BinaryOutput Payload;

			using (MqttConnection MqttConnection = ConnectToMqttServer("iot.eclipse.org", false, string.Empty, string.Empty))
			{
				WriteLine("<" + MqttConnection.State.ToString() + ">", C64Colors.LightGreen);

				MqttConnection.TrustServer = true;

				MqttConnection.OnConnectionError += (sender, ex) =>
				{
					WriteLine("Unable to connect:", C64Colors.Red);
				};

				MqttConnection.OnError += (sender, ex) =>
				{
					WriteLine(ex.Message, C64Colors.Red);
				};

				MqttConnection.OnStateChanged += (sender, state) =>
				{
					WriteLine("<" + MqttConnection.State.ToString() + ">", C64Colors.LightGreen);

					if (state == MqttState.Connected)
						MqttConnection.SUBSCRIBE("RetroSharp/Examples/Networking/MultiUserDraw");
				};

				OnMouseMove += (sender, e) =>
				{
					P = e.Position;
					Pointer.SetPosition(P);

					int DX = P.X - RasterWidth / 2;
					int DY = P.Y - RasterHeight / 2;

					Pointer.Angle = 90 + 22.5 + System.Math.Atan2(DY, DX) * 180 / System.Math.PI;

					if (Draw)
					{
						Payload = new BinaryOutput();
						Payload.WriteString(MqttConnection.ClientId);
						Payload.WriteInt(LastP.X);
						Payload.WriteInt(LastP.Y);
						Payload.WriteInt(P.X);
						Payload.WriteInt(P.Y);
						Payload.WriteColor(Color);

						MqttConnection.PUBLISH("RetroSharp/Examples/Networking/MultiUserDraw", MqttQualityOfService.AtMostOne, false, Payload);

						DrawLine(LastP.X, LastP.Y, P.X, P.Y, Color);
					}

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

				MqttConnection.OnContentReceived += (sender, Content) =>
				{
					BinaryInput Input = Content.DataInput;
					string ClientId = Input.ReadString();
					if (ClientId != MqttConnection.ClientId)
					{
						int X1 = (int)Input.ReadInt();
						int Y1 = (int)Input.ReadInt();
						int X2 = (int)Input.ReadInt();
						int Y2 = (int)Input.ReadInt();
						Color cl = Input.ReadColor();

						DrawLine(X1, Y1, X2, Y2, cl);
					}
				};

				while (!Terminated.WaitOne(1000))
					;
			}

			Terminate();
		}
	}
}