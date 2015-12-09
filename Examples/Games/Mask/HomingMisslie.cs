using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public class HomingMissile : Shot 
	{
		private Player target;

		public HomingMissile(int X, int Y, int FramesPerPixel, int StepsPerFrame, Color Color, int Power, Player Target)
			: base(X, Y, 0, 0, FramesPerPixel, StepsPerFrame, Color, Power, false)
		{
			this.target = Target;
		}

		public override bool MoveStep()
		{
			this.vx = Math.Sign(this.target.X - this.x);
			this.vy = Math.Sign(this.target.Y - this.y);

			if (this.vx == 0 && this.vy == 0)
			{
				this.Die();
				return true;
			}
			else
				return base.MoveStep();
		}

	}
}
