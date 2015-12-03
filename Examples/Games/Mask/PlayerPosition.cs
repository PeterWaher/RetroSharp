using System;
using System.Collections.Generic;
using System.Text;
using RetroSharp.Networking;

namespace Mask
{
	public class PlayerPosition
	{
		private int x, y;
		private int vx, vy;
		private bool dead;

		public PlayerPosition(int X, int Y, int VX, int VY, bool Dead)
		{
			this.x = X;
			this.y = Y;
			this.vx = VX;
			this.vy = VY;
			this.dead = Dead;
		}

		public PlayerPosition(BinaryInput Input)
		{
			this.x = 319 - (int)Input.ReadInt();
			this.y = 207 - (int)Input.ReadInt();
			this.vx = -(int)Input.ReadInt();
			this.vy = -(int)Input.ReadInt();
			this.dead = Input.ReadBool();
		}

		public int X
		{
			get { return this.x; }
		}

		public int Y
		{
			get { return this.y; }
		}

		public int VX
		{
			get { return this.vx; }
		}

		public int VY
		{
			get { return this.vy; }
		}

		public bool Dead
		{
			get { return this.dead; }
		}
	}
}
