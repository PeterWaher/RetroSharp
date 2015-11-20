using System;
using System.Drawing;
using System.Threading;
using RetroSharp;
using RetroSharp.Networking;
using RetroSharp.Networking.MQTT;

namespace MultiUserChat
{
	[CharacterSet("Consolas", 256)]
	[Characters(80, 30, KnownColor.White)]
	[ScreenBorder(30, 20)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			Initialize();

			WriteLine("What is your name?", C64Colors.LightBlue);
			string Name = Console.In.ReadLine();
			BinaryOutput Payload;

			WriteLine("Hello " + Name + ".", C64Colors.LightBlue);
			WriteLine("Strings entered below will be seen by everybody running the application.", C64Colors.LightBlue);
			WriteLine("Enter an empty string to close the application.", C64Colors.LightBlue);
			WriteLine(new string('-', ConsoleWidth), C64Colors.LightBlue);

			using (MqttConnection MqttConnection = ConnectToMqttServer("iot.eclipse.org", true, string.Empty, string.Empty))
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
					{
						MqttConnection.SUBSCRIBE("RetroSharp/Examples/Networking/MultiUserChat");

						Payload = new BinaryOutput();
						Payload.WriteString(MqttConnection.ClientId);
						Payload.WriteString(Name);
						Payload.WriteByte(0);

						MqttConnection.PUBLISH("RetroSharp/Examples/Networking/MultiUserChat", MqttQualityOfService.AtLeastOne, false, Payload);
					}
				};

				MqttConnection.OnContentReceived += (sender, Content) =>
				{
					string ClientId = Content.DataInput.ReadString();
					if (ClientId != sender.ClientId)
					{
						string Author = Content.DataInput.ReadString();
						byte Command = Content.DataInput.ReadByte();

						switch (Command)
						{
							case 0:
								WriteLine("<" + Author + " enters the room.>", C64Colors.LightGreen);
								break;

							case 1:
								string Text = Content.DataInput.ReadString();
								WriteLine(Author + ": " + Text, C64Colors.LightBlue);
								break;

							case 2:
								WriteLine("<" + Author + " left the room.>", C64Colors.LightGreen);
								break;
						}
					}
				};

				while (true)
				{
					string s = Console.In.ReadLine();
					if (string.IsNullOrEmpty(s))
						break;

					Payload = new BinaryOutput();
					Payload.WriteString(MqttConnection.ClientId);
					Payload.WriteString(Name);
					Payload.WriteByte(1);
					Payload.WriteString(s);

					MqttConnection.PUBLISH("RetroSharp/Examples/Networking/MultiUserChat", MqttQualityOfService.AtLeastOne, false, Payload);
				}

				MqttConnection.UNSUBSCRIBE("RetroSharp/Examples/Networking/MultiUserChat");

				int PacketIdentifier = 0;
				ManualResetEvent Terminated = new ManualResetEvent(false);

				MqttConnection.OnPublished += (sender, e) =>
				{
					if (PacketIdentifier == e)
						Terminated.Set();
				};

				Payload = new BinaryOutput();
				Payload.WriteString(MqttConnection.ClientId);
				Payload.WriteString(Name);
				Payload.WriteByte(2);

				PacketIdentifier = MqttConnection.PUBLISH("RetroSharp/Examples/Networking/MultiUserChat", MqttQualityOfService.AtLeastOne, false, Payload);

				Terminated.WaitOne(5000);
			}

			Terminate();
		}

		private static void WriteLine(string s, Color Fg)
		{
			Color FgBak = ForegroundColor;
			ForegroundColor = Fg;

			Console.Out.WriteLine(s);
			
			ForegroundColor = FgBak;
		}

	}
}