using System;
using System.Drawing;
using RetroSharp;

namespace Mask
{
	public abstract class AnimatedObject
	{
		protected int framesPerPixel;
		protected int framesLeft;
		protected bool dead;

		public AnimatedObject(int FramesPerPixel)
		{
			this.framesPerPixel = FramesPerPixel;
			this.framesLeft = FramesPerPixel;
			this.dead = false;
		}

		public bool Dead { get { return this.dead; } }

		public abstract bool Move();

		public virtual void Die()
		{
			this.dead = true;
		}

	}
}
