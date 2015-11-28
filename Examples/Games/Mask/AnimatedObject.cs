using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public abstract class AnimatedObject
	{
		protected int framesPerPixel;
		protected int framesLeft;
		protected int stepsPerFrame;
		protected bool dead;

		public AnimatedObject(int FramesPerPixel, int StepsPerFrame)
		{
			this.framesPerPixel = FramesPerPixel;
			this.framesLeft = FramesPerPixel;
			this.stepsPerFrame = StepsPerFrame;
			this.dead = false;
		}

		public bool Dead { get { return this.dead; } }

		public virtual bool Move()
		{
			int i;

			for (i = 0; i < this.stepsPerFrame; i++)
			{
				if (this.MoveStep())
					return true;
			}

			return false;
		}

		public abstract bool MoveStep();

		public virtual void Die()
		{
			this.dead = true;
		}

	}
}
