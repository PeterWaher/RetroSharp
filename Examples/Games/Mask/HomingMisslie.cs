using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public class HomingMissile : Shot 
	{
		private Player target;

		public HomingMissile(int X, int Y, int FramesPerPixel, int StepsPerFrame, Color Color, int Power, Player Target)
			: base(X, Y, 0, 0, FramesPerPixel, StepsPerFrame, Color, Power)
		{
			this.target = Target;
		}

		public override bool MoveStep()
		{
			this.vx = Math.Sign(this.target.X - this.x);
			this.vy = Math.Sign(this.target.Y - this.y);

			return base.MoveStep();
		}

	}
}
