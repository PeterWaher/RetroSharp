using System;
using System.Collections.Generic;
using System.Text;
using RetroSharp;
using RetroSharp.Networking;

namespace MultiPlayerSetup
{
	[CharacterSet("Consolas", 256)]
	[Characters(80, 30)]
	[ScreenBorder(30, 20)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			Initialize();

			try
			{
				DateTime Start = DateTime.Now;
				Guid PlayerId = Guid.NewGuid();
				string Name;

				Console.Out.WriteLine("Hello. What is your name?");
				Name = Console.ReadLine();

				using (MultiPlayerEnvironment MPE = new MultiPlayerEnvironment("MultiPlayerSetup",
					"iot.eclipse.org", 1883, false, string.Empty, string.Empty, "RetroSharp/Examples/Networking/MultiPlayerSetup",
					2, PlayerId, new KeyValuePair<string, string>("NAME", Name)))
				{
					MPE.OnStateChange += (sender, newstate) => Console.Out.WriteLine(newstate.ToString());

					MPE.OnPlayerAvailable += (sender, player) =>
					{
						Console.Out.WriteLine("New player available: " + player["NAME"]);
						if (sender.PlayerCount >= 5 || (DateTime.Now - Start).TotalSeconds >= 20)
							MPE.ConnectPlayers();
					};

					MPE.OnPlayerConnected += (sender, player) => Console.Out.WriteLine("Player connected: " + player["NAME"]);

					MPE.OnGameDataReceived += (sender, e) => Console.Out.WriteLine(e.FromPlayer["NAME"] + ": " + e.Data.ReadString());

					MPE.OnPlayerDisconnected += (sender, player) => Console.Out.WriteLine("Player disconnected: " + player["NAME"]);

					if (!MPE.Wait(20000))
					{
						if (MPE.State == MultiPlayerState.FindingPlayers)
							MPE.ConnectPlayers();
						else
							throw new Exception("Unable to setup multi-player environment.");
					}

					Console.Out.WriteLine(MPE.PlayerCount.ToString() + " player(s) now connected.");
					Console.Out.WriteLine("Write anything and send it to the others.");
					Console.Out.WriteLine("An empty row will quit the application.");
					Console.Out.WriteLine();

					string s = Console.In.ReadLine();

					while (!string.IsNullOrEmpty(s))
					{
						BinaryOutput Msg = new BinaryOutput();
						Msg.WriteString(s);
						MPE.SendToAll(Msg.GetPacket());

						s = Console.In.ReadLine();
					}
				}
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine(ex.Message);
				Console.In.ReadLine();
			}

			Terminate();
		}
	}
}