using System;
using System.Collections.Generic;
using System.Drawing;
using RetroSharp;
using RetroSharp.Networking;

namespace Mask
{
	public enum Gun
	{
		Normal,
		WurstScheibe,
		Homing,
		Diagonal
	}

	public class Player : MovingObject
	{
		private Player opponent = null;
		private Color color;
		private Color headColor;
		private int playerNr;
		private int shotPower;
		private int shotSpeed = 1;
		private bool tail = true;
		private Gun gun = Gun.Normal;
		private bool invisible = false;

		public Player(int PlayerNr, int X, int Y, int VX, int VY, int FramesPerPixel, Color Color, Color HeadColor, int Power)
			: base(X, Y, VX, VY, FramesPerPixel, 1, true)
		{
			this.playerNr = PlayerNr;
			this.color = Color;
			this.headColor = HeadColor;
			this.shotPower = Power;

			if (this.playerNr == 1 || Program.NrPlayers == 1)
				RetroApplication.Raster[X, Y] = HeadColor;
		}

		public Player Opponent
		{
			get { return this.opponent; }
			internal set { this.opponent = value; }
		}

		public override void BeforeMove()
		{
			if (!this.tail || (this.invisible && this.playerNr == 2))
				RetroApplication.Raster[this.x, this.y] = Color.Black;
			else
				RetroApplication.Raster[this.x, this.y] = this.color;
		}

		public override bool MoveStep()
		{
			if (Program.NrPlayers == 1)
				return base.MoveStep();
			else if (this.playerNr == 1)
			{
				int X = this.x;
				int Y = this.y;

				bool Result = base.MoveStep();

				if (this.x != X || this.y != Y || Result)
				{
					BinaryOutput Output = new BinaryOutput();
					Output.WriteByte(8);
					Output.WriteInt(this.x);
					Output.WriteInt(this.y);
					Output.WriteInt(this.vx);
					Output.WriteInt(this.vy);
					Output.WriteBool(Result);

					Program.MPE.SendUdpToAll(Output.GetPacket(), 3);
				}

				return Result;
			}
			else
				return false;
		}

		public override void AfterMove()
		{
			Color DestPixel = RetroApplication.Raster[this.x, this.y];

			if (DestPixel.ToArgb() != Color.Black.ToArgb())
			{
				if (DestPixel.R == 255 && DestPixel.B == 0)
				{
					RetroApplication.FillFlood(this.x, this.y, Color.Black);

					if (this.playerNr == 1)
					{
						BinaryOutput Output = new BinaryOutput();
						int Gift = RetroApplication.Random(0, 26);

						Output.WriteByte(7);
						Output.WriteInt(Gift);

						this.GetGift(1, Gift, Output, null);

						if (Program.NrPlayers > 1)
							Program.MPE.SendUdpToAll(Output.GetPacket(), 3);
					}
				}
				else
				{
					this.Die();
					return;
				}
			}

			if (!this.invisible || this.playerNr == 2)
				RetroApplication.Raster[this.x, this.y] = this.headColor;
		}

		public void GetGift(int PlayerIndex, int GiftIndex, BinaryOutput Output, BinaryInput Input)
		{
			switch (GiftIndex)
			{
				case 0:
					this.framesPerPixel = 4;
					this.stepsPerFrame = 1;
					Program.PlayerMsg(this.playerNr, "Slow speed");
					break;

				case 1:
					this.framesPerPixel = 5;
					this.stepsPerFrame = 1;
					Program.PlayerMsg(this.playerNr, "Slower speed");
					break;

				case 2:
					this.framesPerPixel = 6;
					this.stepsPerFrame = 1;
					Program.PlayerMsg(this.playerNr, "Slowest speed");
					break;

				case 3:
					this.framesPerPixel = 2;
					this.stepsPerFrame = 1;
					Program.PlayerMsg(this.playerNr, "Fast speed");
					break;

				case 4:
					this.framesPerPixel = 1;
					this.stepsPerFrame = 1;
					Program.PlayerMsg(this.playerNr, "Faster speed");
					break;

				case 5:
					this.framesPerPixel = 1;
					this.stepsPerFrame = 2;
					if (this.shotSpeed < 2)
						this.shotSpeed = 2;
					Program.PlayerMsg(this.playerNr, "Fastest speed");
					break;

				case 6:
					this.framesPerPixel = 3;
					this.stepsPerFrame = 1;
					Program.PlayerMsg(this.playerNr, "Normal speed");
					break;

				case 7:
					this.shotPower = 5;
					Program.PlayerMsg(this.playerNr, "Tiny bullets");
					break;

				case 8:
					this.shotPower = 10;
					Program.PlayerMsg(this.playerNr, "Small bullets");
					break;

				case 9:
					this.shotPower = 15;
					Program.PlayerMsg(this.playerNr, "Normal bullets");
					break;

				case 10:
					this.shotPower = 20;
					Program.PlayerMsg(this.playerNr, "Large bullets");
					break;

				case 11:
					this.shotPower = 25;
					Program.PlayerMsg(this.playerNr, "Huge bullets");
					break;

				case 12:
					this.shotPower = 30;
					Program.PlayerMsg(this.playerNr, "Humongous bullets");
					break;

				case 13:
					this.shotPower = 50;
					Program.PlayerMsg(this.playerNr, "Peace-maker bullets");
					break;

				case 14:
					this.tail = false;
					Program.PlayerMsg(this.playerNr, "No tail");
					break;

				case 15:
					this.tail = true;
					Program.PlayerMsg(this.playerNr, "Tail");
					break;

				case 16:
					this.shotSpeed = Math.Max(1, this.stepsPerFrame);
					Program.PlayerMsg(this.playerNr, "Slow bullets");
					break;

				case 17:
					this.shotSpeed = Math.Max(2, this.stepsPerFrame);
					Program.PlayerMsg(this.playerNr, "Fast bullets");
					break;

				case 18:
					this.shotSpeed = Math.Max(3, this.stepsPerFrame);
					Program.PlayerMsg(this.playerNr, "Faster bullets");
					break;

				case 19:
					this.shotSpeed = Math.Max(4, this.stepsPerFrame);
					Program.PlayerMsg(this.playerNr, "Fastest bullets");
					break;

				case 20:
					int X, Y;
					int dx, dy;
					bool Ok;
					int TriesLeft = 100;
					int Black = Color.Black.ToArgb();

					if (Input != null)
					{
						this.x = 319 - (int)Input.ReadInt();
						this.y = 207 - (int)Input.ReadInt();
					}
					else
					{
						Ok = false;

						while (TriesLeft-- > 0 && !Ok)
						{
							X = RetroApplication.Random(30, 290);
							Y = RetroApplication.Random(38, 170);
							Ok = true;
							for (dy = -5; dy < 5; dy++)
							{
								for (dx = -5; dx < 5; dx++)
								{
									if (RetroApplication.Raster[X + dx, Y + dy].ToArgb() != Black)
									{
										Ok = false;
										dy = 5;
										break;
									}
								}
							}

							if (Ok)
							{
								Output.WriteInt(X);
								Output.WriteInt(Y);

								this.x = X;
								this.y = Y;
							}
						}

						if (!Ok && Output != null)
						{
							Output.WriteInt(this.x);
							Output.WriteInt(this.y);
						}
					}

					Program.PlayerMsg(this.playerNr, "Teleport");
					break;

				case 21:
					this.gun = Gun.WurstScheibe;
					Program.PlayerMsg(this.playerNr, "Wurst scheibe");
					break;

				case 22:
					this.gun = Gun.Homing;
					Program.PlayerMsg(this.playerNr, "Homing Missile");
					break;

				case 23:
					this.shotPower = 15;
					this.shotSpeed = 1;
					this.tail = true;
					this.gun = Gun.Normal;
					this.invisible = false;
					Program.PlayerMsg(this.playerNr, "Reset");
					break;

				case 24:
					int X1, Y1, X2, Y2;
					bool Inside;

					if (Input != null)
					{
						X2 = 319 - (int)Input.ReadInt();
						Y2 = 207 - (int)Input.ReadInt();
						X1 = 319 - (int)Input.ReadInt();
						Y1 = 207 - (int)Input.ReadInt();
					}
					else
					{
						do
						{
							X1 = RetroApplication.Random(30, 290);
							X2 = RetroApplication.Random(30, 290);
							Y1 = RetroApplication.Random(38, 170);
							Y2 = RetroApplication.Random(38, 170);

							if (X2 < X1)
							{
								X = X1;
								X1 = X2;
								X2 = X;
							}

							if (Y2 < Y1)
							{
								Y = Y1;
								Y1 = Y2;
								Y2 = Y;
							}

							Inside =
								(this.x >= X1 - 20 && this.x <= X2 + 20 && this.y >= Y1 - 20 && this.y <= Y2 + 20) ||
								(this.opponent.x >= X1 - 20 && this.opponent.x <= X2 + 20 && this.opponent.y >= Y1 - 20 && this.opponent.y <= Y2 + 20);
						}
						while (Inside || Math.Abs(X1 - X2) < 20 || Math.Abs(Y1 - Y2) < 20);

						if (Output != null)
						{
							Output.WriteInt(X1);
							Output.WriteInt(Y1);
							Output.WriteInt(X2);
							Output.WriteInt(Y2);
						}
					}

					RetroApplication.FillRoundedRectangle(X1, Y1, X2, Y2, 8, 8, (x, y, DestinationColor) =>
					{
						int i = ((x - X1) + (y - Y1)) % 6;
						if (i < 3)
							return Color.Cyan;
						else
							return RetroApplication.Blend(Color.Cyan, DestinationColor, 0.5);
					});
					RetroApplication.DrawRoundedRectangle(X1, Y1, X2, Y2, 8, 8, Color.Cyan);
					Program.PlayerMsg(this.playerNr, "Obstacle");
					break;

				case 25:
					this.invisible = true;
					Program.PlayerMsg(this.playerNr, "Invisibility");
					break;

				case 26:
					this.gun = Gun.Diagonal;
					Program.PlayerMsg(this.playerNr, "Diagonal shots");
					break;
			}
		}

		public override void Die()
		{
			base.Die();
			Program.PlayerMsg(this.playerNr, "Argh! Press ENTER to retry.");
		}

		public void Up()
		{
			this.vx = 0;
			this.vy = -1;
		}

		public void Down()
		{
			this.vx = 0;
			this.vy = 1;
		}

		public void Left()
		{
			this.vx = -1;
			this.vy = 0;
		}

		public void Right()
		{
			this.vx = 1;
			this.vy = 0;
		}

		public void Fire(LinkedList<Shot> Shots)
		{
			switch (this.gun)
			{
				case Gun.Normal:
					Shots.AddLast(new Shot(this.x + this.vx, this.y + this.vy, this.vx, this.vy, 1, this.shotSpeed, Color.White, this.shotPower));
					break;

				case Gun.Homing:
					Shots.AddLast(new HomingMissile(this.x + this.vx, this.y + this.vy, 1, this.shotSpeed, Color.White, this.shotPower, this.opponent));
					break;

				case Gun.WurstScheibe:
					int i;

					for (i = -20; i <= 20; i += 10)
						Shots.AddLast(new Shot(this.x + this.vx - i * this.vy, this.y + this.vy - i * this.vx, this.vx, this.vy, 1, this.shotSpeed, Color.White, this.shotPower));

					break;

				case Gun.Diagonal:
					if (this.vy == 0)
					{
						Shots.AddLast(new Shot(this.x + this.vx, this.y + this.vy, this.vx, 1, 1, this.shotSpeed, Color.White, this.shotPower));
						Shots.AddLast(new Shot(this.x + this.vx, this.y + this.vy, this.vx, -1, 1, this.shotSpeed, Color.White, this.shotPower));
					}
					else
					{
						Shots.AddLast(new Shot(this.x + this.vx, this.y + this.vy, 1, this.vy, 1, this.shotSpeed, Color.White, this.shotPower));
						Shots.AddLast(new Shot(this.x + this.vx, this.y + this.vy, -1, this.vy, 1, this.shotSpeed, Color.White, this.shotPower));
					}
					break;
			}
		}

	}
}
