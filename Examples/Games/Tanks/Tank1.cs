using System;
using System.Collections.Generic;
using System.Text;
using RetroSharp;

namespace Tanks
{
	public class Tank1 : Tank
	{
		public Tank1(Sprite Sprite)
			: base(Sprite)
		{
		}

		public void Left()
		{
			this.sprite.Angle -= 2;
		}

		public void Right()
		{
			this.sprite.Angle += 2;
		}

		public void Forward()
		{
			this.a = 0.25;
		}

		public void Backward()
		{
			this.a = -0.25;
		}

		public void NoForwardBackword()
		{
			this.a = 0;
		}

		public void Fire()
		{
		}
	}
}
