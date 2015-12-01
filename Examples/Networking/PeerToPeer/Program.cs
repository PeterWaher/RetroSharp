using System;
using System.Collections.Generic;
using System.Net.Sockets;
using RetroSharp;
using RetroSharp.Networking.P2P;

namespace PeerToPeer
{
	[CharacterSet("Consolas", 256)]
	[Characters(120, 43)]
	[ScreenBorder(30, 20)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			Initialize();

			try
			{
				using (PeerToPeerNetwork P2PNetwork = new PeerToPeerNetwork("Retro Peer-to-Peer example"))
				{
					P2PNetwork.OnStateChange += (sender, newstate) => Console.Out.WriteLine(newstate.ToString());
					P2PNetwork.OnPeerConnected += (sender, connection) =>
					{
						Console.Out.WriteLine("Client connected from: " + connection.Tcp.Client.RemoteEndPoint.ToString());
					};

					if (!P2PNetwork.Wait())
						throw new Exception("Unable to configure Internet Gateway NAT traversal.");

					Console.Out.WriteLine("External IP Endpoint: " + P2PNetwork.ExternalEndpoint.ToString());

					Console.Out.WriteLine("Press ENTER to try to connect.");
					Console.In.ReadLine();

					using (PeerConnection Client = P2PNetwork.ConnectToPeer(P2PNetwork.ExternalEndpoint))
					{
						Console.In.ReadLine();
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