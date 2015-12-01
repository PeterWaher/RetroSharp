using System;
using System.Collections.Generic;
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
			//Initialize();

			try
			{
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
						if (sender.PlayerCount == 2)
							MPE.ConnectPlayers();
					};
					MPE.OnPlayerConnected += (sender, player) => Console.Out.WriteLine("Player connected: " + player["NAME"]);

					if (!MPE.Wait(10000))
						throw new Exception("Unable to setup multi-player environment.");

					Console.Out.WriteLine("All players now connected.");
					Console.Out.WriteLine("Write anything and send it to the others.");
					Console.Out.WriteLine("An empty row will quit the application.");
					Console.Out.WriteLine();
					Console.In.ReadLine();
				}
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine(ex.Message);
				Console.In.ReadLine();
			}

			//Terminate();
		}
	}
}