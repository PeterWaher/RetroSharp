using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using RetroSharp;
using RetroSharp.Networking;

namespace Mask
{
	[RasterGraphics(320, 200)]
	[Characters(80, 25, KnownColor.White)]
	[ScreenBorder(30, 20, KnownColor.DimGray)]
	[AspectRatio(4, 3)]
	class Program : RetroApplication
	{
		internal static MultiPlayerEnvironment MPE;
		internal static bool LocalMachineIsGameServer = true;
		internal static int NrPlayers = 2;

		public static void Main(string[] args)
		{
			Initialize();

			Guid PlayerId = Guid.NewGuid();
			string Player1Name;
			string Player2Name;

			Console.Out.WriteLine("Welcome to Mask! (Worms/Tron)");
			Console.Out.WriteLine(new string('-', 70));
			Console.Out.WriteLine("You control the work using the cursor keys.");
			Console.Out.WriteLine("Fire, using SPACE.");
			Console.Out.WriteLine("If you die, press ENTER to restart the game.");
			Console.Out.WriteLine("You can chat during the game.");
			Console.Out.WriteLine("Remember to try to fetch the gifts. You do that by moving into them.");
			Console.Out.WriteLine();
			Console.Out.WriteLine("Hello. What is your name?");
			Player1Name = Player2Name = Console.ReadLine();

			using (MPE = new MultiPlayerEnvironment("Mask", false, "iot.eclipse.org", 1883, false, string.Empty, string.Empty,
				"RetroSharp/Examples/Games/Mask", 2, PlayerId, new KeyValuePair<string, string>("NAME", Player1Name)))
			{
				MPE.OnStateChange += (sender, state) =>
				{
					switch (state)
					{
						case MultiPlayerState.SearchingForGateway:
							Console.Out.WriteLine("Searching for Internet Gateway.");
							break;

						case MultiPlayerState.RegisteringApplicationInGateway:
							Console.Out.WriteLine("Registering game in gateway.");
							break;

						case MultiPlayerState.FindingPlayers:
							Console.Out.WriteLine("Waiting for another player to connect.");
							Console.Out.WriteLine("Press ESC to play in single player mode.");
							OnKeyDown += new KeyEventHandler(MPE_Wait_OnKeyDown);
							break;

						case MultiPlayerState.ConnectingPlayers:
							Console.Out.WriteLine("Connecting to players.");
							break;
					}
				};

				MPE.OnPlayerAvailable += (sender, player) =>
				{
					Console.Out.WriteLine("New player available: " + player["NAME"]);
					MPE.ConnectPlayers();
				};

				MPE.OnPlayerConnected += (sender, player) =>
				{
					Player2Name = player["NAME"];
				};

				MPE.OnPlayerDisconnected += (sender, player) =>
				{
					PlayerMsg(2, "Disconnected");
					NrPlayers = 1;
					LocalMachineIsGameServer = true;
				};

				if (MPE.Wait(int.MaxValue))
				{
					NrPlayers = MPE.PlayerCount;
					LocalMachineIsGameServer = MPE.LocalPlayerIsFirst;
				}
				else
				{
					PlayerMsg(2, "Network error");
					NrPlayers = 1;
				}

				OnKeyDown -= new KeyEventHandler(MPE_Wait_OnKeyDown);

				ManualResetEvent Done = new ManualResetEvent(false);
				LinkedList<Shot> Shots = new LinkedList<Shot>();
				LinkedList<Explosion> Explosions = new LinkedList<Explosion>();
				LinkedList<Present> Presents = new LinkedList<Present>();
				LinkedList<PlayerPosition> Player2Positions = new LinkedList<PlayerPosition>();
				Player Player1 = new Player(1, 20, 28, 1, 0, 3, Color.Green, Color.LightGreen, 15);
				Player Player2 = new Player(2, 299, 179, -1, 0, 3, Color.Blue, Color.LightBlue, 15);
				bool Player1Up = false;
				bool Player1Down = false;
				bool Player1Left = false;
				bool Player1Right = false;
				bool Player1Fire = false;
				bool Player2Up = false;
				bool Player2Down = false;
				bool Player2Left = false;
				bool Player2Right = false;
				bool Player2Fire = false;

				Player1.Opponent = Player2;
				Player2.Opponent = Player1;

				Clear();
				FillRectangle(0, 0, 319, 7, Color.FromKnownColor(KnownColor.DimGray));
				SetClipArea(0, 8, 319, 199);

				string s = Player1Name.Length <= 10 ? Player1Name : Player1Name.Substring(0, 10);
				Console.Out.Write(s);

				s = Player2Name.Length <= 10 ? Player2Name : Player2Name.Substring(0, 10);
				GotoXY(ConsoleWidth - s.Length, 0);
				Console.Out.Write(s);

				OnKeyDown += (sender, e) =>
				{
					switch (e.Key)
					{
						case Key.Escape:
							if (MPE.State == MultiPlayerState.FindingPlayers)
								MPE.ConnectPlayers();
							else
								Done.Set();
							break;

						case Key.C:
							if (e.Control)
								Done.Set();
							break;

						case Key.Up:
							if (!Player1.Dead && Player1.VY != 1)
							{
								Player1Up = true;

								if (NrPlayers == 1)
								{
									if (!Player2.Dead)
										Player2Down = true;
								}
								else
									MPE.SendUdpToAll(new byte[] { 0 }, 3);
							}
							break;

						case Key.Down:
							if (!Player1.Dead && Player1.VY != -1)
							{
								Player1Down = true;

								if (NrPlayers == 1)
								{
									if (!Player2.Dead)
										Player2Up = true;
								}
								else
									MPE.SendUdpToAll(new byte[] { 1 }, 3);
							}
							break;

						case Key.Left:
							if (!Player1.Dead && Player1.VX != 1)
							{
								Player1Left = true;

								if (NrPlayers == 1)
								{
									if (!Player2.Dead)
										Player2Right = true;
								}
								else
									MPE.SendUdpToAll(new byte[] { 2 }, 3);
							}
							break;

						case Key.Right:
							if (!Player1.Dead && Player1.VX != -1)
							{
								Player1Right = true;

								if (NrPlayers == 1)
								{
									if (!Player2.Dead)
										Player2Left = true;
								}
								else
									MPE.SendUdpToAll(new byte[] { 3 }, 3);
							}
							break;

						case Key.Space:
							if (!Player1.Dead)
							{
								Player1Fire = true;

								if (NrPlayers == 1)
								{
									if (!Player2.Dead)
										Player2Fire = true;
								}
								else
									MPE.SendUdpToAll(new byte[] { 4 }, 3);
							}
							break;

						case Key.Enter:
							if (Player1.Dead)
							{
								if (NrPlayers > 1)
									MPE.SendUdpToAll(new byte[] { 5 }, 3);
								else
								{
									lock (Player2Positions)
									{
										Player2Positions.Clear();
									}

									Shots.Clear();
									Explosions.Clear();
									Presents.Clear();
									Player1 = new Player(1, 20, 28, 1, 0, 3, Color.Green, Color.LightGreen, 15);
									Player2 = new Player(2, 299, 179, -1, 0, 3, Color.Blue, Color.LightBlue, 15);
									Player1Up = false;
									Player1Down = false;
									Player1Left = false;
									Player1Right = false;
									Player1Fire = false;

									Player1.Opponent = Player2;
									Player2.Opponent = Player1;

									FillRectangle(0, 8, 319, 199, Color.Black);
									PlayerMsg(1, string.Empty);
									PlayerMsg(2, string.Empty);
								}
							}
							break;
					}
				};

				OnKeyPressed += (sender, e) =>
				{
					ChatCharacter(1, e.Character);
					MPE.SendTcpToAll(new byte[] { 10, (byte)(e.Character >> 8), (byte)(e.Character) });
				};

				OnUpdateModel += (sender, e) =>
				{
					if (LocalMachineIsGameServer && Random() < 0.005)
					{
						int x1, y1;

						do
						{
							x1 = Random(30, 285);
							y1 = Random(38, 165);
						}
						while (!Present.CanPlace(x1, y1, x1 + 5, y1 + 5));

						Presents.AddLast(new Present(x1, y1, x1 + 5, y1 + 5));

						if (NrPlayers > 1)
						{
							BinaryOutput Output = new BinaryOutput();

							Output.WriteByte(6);
							Output.WriteInt(x1);
							Output.WriteInt(y1);

							MPE.SendUdpToAll(Output.GetPacket(), 3);
						}
					}

					LinkedListNode<Present> PresentObj, NextPresentObj;

					PresentObj = Presents.First;
					while (PresentObj != null)
					{
						NextPresentObj = PresentObj.Next;
						if (PresentObj.Value.Move())
							Presents.Remove(PresentObj);

						PresentObj = NextPresentObj;
					}

					if (Player1Up)
					{
						Player1.Up();
						Player1Up = false;
					}
					else if (Player1Down)
					{
						Player1.Down();
						Player1Down = false;
					}
					else if (Player1Left)
					{
						Player1.Left();
						Player1Left = false;
					}
					else if (Player1Right)
					{
						Player1.Right();
						Player1Right = false;
					}

					if (Player2Up)
					{
						Player2.Up();
						Player2Up = false;
					}
					else if (Player2Down)
					{
						Player2.Down();
						Player2Down = false;
					}
					else if (Player2Left)
					{
						Player2.Left();
						Player2Left = false;
					}
					else if (Player2Right)
					{
						Player2.Right();
						Player2Right = false;
					}

					if (!Player1.Dead && Player1.Move())
						Explosions.AddLast(new Explosion(Player1.X, Player1.Y, 30, Color.White));

					if (!Player2.Dead)
					{
						if (NrPlayers == 1)
						{
							if (Player2.Move())
								Explosions.AddLast(new Explosion(Player2.X, Player2.Y, 30, Color.White));
						}
						else
						{
							lock (Player2Positions)
							{
								try
								{
									foreach (PlayerPosition P in Player2Positions)
									{
										Player2.BeforeMove();
										Player2.SetPosition(P.X, P.Y, P.VX, P.VY);
										Player2.AfterMove();

										if (P.Dead)
										{
											Player2.Die();
											Explosions.AddLast(new Explosion(Player2.X, Player2.Y, 30, Color.White));
										}
									}
								}
								finally
								{
									Player2Positions.Clear();
								}
							}
						}
					}

					if (Player1Fire)
					{
						Player1.Fire(Shots);
						Player1Fire = false;
					}

					if (Player2Fire)
					{
						Player2.Fire(Shots);
						Player2Fire = false;
					}

					LinkedListNode<Shot> ShotObj, NextShotObj;

					ShotObj = Shots.First;
					while (ShotObj != null)
					{
						NextShotObj = ShotObj.Next;
						if (ShotObj.Value.Move())
						{
							Shots.Remove(ShotObj);
							Explosions.AddLast(new Explosion(ShotObj.Value.X, ShotObj.Value.Y, ShotObj.Value.Power, Color.White));
						}

						ShotObj = NextShotObj;
					}

					LinkedListNode<Explosion> ExplosionObj, NextExplosionObj;

					ExplosionObj = Explosions.First;
					while (ExplosionObj != null)
					{
						NextExplosionObj = ExplosionObj.Next;
						if (ExplosionObj.Value.Move())
							Explosions.Remove(ExplosionObj);

						ExplosionObj = NextExplosionObj;
					}
				};

				MPE.OnGameDataReceived += (sender, e) =>
				{
					byte Command = e.Data.ReadByte();

					switch (Command)
					{
						case 0:	// Remote player presses UP
							if (!Player2.Dead)
								Player2Down = true;
							break;

						case 1:	// Remote player presses DOWN
							if (!Player2.Dead)
								Player2Up = true;
							break;

						case 2:	// Remote player presses LEFT
							if (!Player2.Dead)
								Player2Right = true;
							break;

						case 3:	// Remote player presses RIGHT
							if (!Player2.Dead)
								Player2Left = true;
							break;

						case 4:	// Remote player presses SPACE (Fire)
							if (!Player2.Dead)
								Player2Fire = true;
							break;

						case 5:	// Remote player presses ENTER (Restart)
						case 9:	// Acknowledgement of remote player presses ENTER (Restart)

							if (Command == 5)
								MPE.SendUdpToAll(new byte[] { 9 }, 3);

							lock (Player2Positions)
							{
								Player2Positions.Clear();
							}

							Shots.Clear();
							Explosions.Clear();
							Presents.Clear();
							Player1 = new Player(1, 20, 28, 1, 0, 3, Color.Green, Color.LightGreen, 15);
							Player2 = new Player(2, 299, 179, -1, 0, 3, Color.Blue, Color.LightBlue, 15);
							Player1Up = false;
							Player1Down = false;
							Player1Left = false;
							Player1Right = false;
							Player1Fire = false;

							Player1.Opponent = Player2;
							Player2.Opponent = Player1;

							FillRectangle(0, 8, 319, 199, Color.Black);
							PlayerMsg(1, string.Empty);
							PlayerMsg(2, string.Empty);
							break;

						case 6:	// New Present
							int x1 = 315 - (int)e.Data.ReadInt();
							int y1 = 203 - (int)e.Data.ReadInt();

							Presents.AddLast(new Present(x1, y1, x1 + 5, y1 + 5));
							break;

						case 7:	// Gift
							x1 = (int)e.Data.ReadInt();
							Player2.GetGift(2, x1, null, e.Data);
							break;

						case 8:	// Move player 2
							lock (Player2Positions)
							{
								Player2Positions.AddLast(new PlayerPosition(e.Data));
							}
							break;

						case 10:	// chat character
							char ch = (char)e.Data.ReadUInt16();
							ChatCharacter(2, ch);
							break;
					}
				};

				while (!Done.WaitOne(1000))
					;
			}

			Terminate();
		}

		private static void MPE_Wait_OnKeyDown(object Sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape && MPE.State == MultiPlayerState.FindingPlayers)
				MPE.ConnectPlayers();
		}

		public static void PlayerMsg(int PlayerNr, string s)
		{
			int c = s.Length;
			if (c > 30)
				s = s.Substring(0, 30);
			else if (c < 30)
				s += new string(' ', 30 - c);

			GotoXY(PlayerNr == 1 ? 10 : 40, 0);
			ForegroundColor = Color.LightBlue;
			Console.Out.Write(s);
		}

		public static void ChatCharacter(int PlayerNr, char ch)
		{
			int x = PlayerNr == 1 ? 10 : 40;
			int i;

			for (i = 0; i < 29; i++)
				Screen[x + i, 0] = Screen[x + i + 1, 0];

			Screen[x + 29, 0] = ch;
		}

	}
}