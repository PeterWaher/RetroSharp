using System;
using RetroSharp;
using RetroSharp.Networking;
using RetroSharp.Networking.MQTT;

namespace Latency
{
	[CharacterSet("Consolas", 256)]
	[Characters(81, 43)]
	[ScreenBorder(30, 20)]
	class Program : RetroApplication
	{
		const int NrTestsPerQoS = 10;

		public static void Main(string[] args)
		{
			Initialize();

			Console.Out.Write("Host Name (default iot.eclipse.org): ");
			string Host = Console.In.ReadLine();

			if (string.IsNullOrEmpty(Host))
			{
				Console.Out.WriteLine("Using iot.eclipse.org.");
				Host = "iot.eclipse.org";
			}

			Console.Out.WriteLine();
			Console.Out.Write("Port Number (default 1883): ");
			string s = Console.In.ReadLine();
			int Port;

			if (string.IsNullOrEmpty(s))
			{
				Console.Out.WriteLine("Using port 1883.");
				Port = 1883;
			}
			else
				Port = int.Parse(s);

			Console.Out.WriteLine();

			BinaryOutput Payload;
			int PacketsLeft = NrTestsPerQoS;

			using (MqttConnection MqttConnection = ConnectToMqttServer("iot.eclipse.org", Port, string.Empty, string.Empty))
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

				MqttConnection.OnContentReceived += (sender, Content) =>
				{
					string ClientId = Content.DataInput.ReadString();
					if (ClientId == sender.ClientId)
					{
						DateTime TP = Content.DataInput.ReadDateTime();
						MqttQualityOfService QoS = (MqttQualityOfService)Content.DataInput.ReadByte();
						Console.Out.WriteLine("Latency: " + (DateTime.Now - TP).TotalMilliseconds + " ms (" + QoS.ToString() + ")");

						bool Resend;

						if (--PacketsLeft > 0)
							Resend = true;
						else if (QoS < MqttQualityOfService.ExactlyOne)
						{
							QoS = (MqttQualityOfService)((int)QoS + 1);
							PacketsLeft = NrTestsPerQoS;
							Resend = true;
						}
						else
							Resend = false;

						if (Resend)
						{
							Payload = new BinaryOutput();
							Payload.WriteString(MqttConnection.ClientId);
							Payload.WriteDateTime(DateTime.Now);
							Payload.WriteByte((byte)QoS);

							MqttConnection.PUBLISH("RetroSharp/Examples/Networking/Latency", QoS, false, Payload);
						}
						else
							Console.Out.WriteLine("Press ENTER to continue.");
					}
				};

				MqttConnection.OnStateChanged += (sender, state) =>
				{
					WriteLine("<" + MqttConnection.State.ToString() + ">", C64Colors.LightGreen);

					if (state == MqttState.Connected)
					{
						MqttConnection.SUBSCRIBE("RetroSharp/Examples/Networking/Latency");

						Payload = new BinaryOutput();
						Payload.WriteString(MqttConnection.ClientId);
						Payload.WriteDateTime(DateTime.Now);
						Payload.WriteByte((byte)MqttQualityOfService.AtMostOne);

						MqttConnection.PUBLISH("RetroSharp/Examples/Networking/Latency", MqttQualityOfService.AtMostOne, false, Payload);
					}
				};

				Console.In.ReadLine();
			}

			Terminate();
		}
	}
}