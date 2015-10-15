using System;
using System.Collections.Generic;
using System.Drawing;

namespace RetroSharp
{
    /// <summary>
    /// Definition of a sprite.
    /// </summary>
    public class Sprite : IDisposable
    {
		private double angle;
		private int x;
		private int y;
		private int spriteTexture;
		private LinkedListNode<Sprite> node;

		/// <summary>
		/// Definition of a sprite.
		/// </summary>
		/// <param name="X">X-coordinate.</param>
		/// <param name="Y">Y-coordinate.</param>
		/// <param name="Angle">Angle of the sprite.</param>
		/// <param name="SpriteTexture">Sprite texture to display.</param>
		internal Sprite(int X, int Y, double Angle, int SpriteTexture)
		{
			this.angle = Angle;
			this.x = X;
			this.y = Y;
			this.spriteTexture = SpriteTexture;
			this.node = null;
		}

		internal LinkedListNode<Sprite> Node
		{
			get { return this.node; }
			set { this.node = value; }
		}

		/// <summary>
		/// X-Offset to sprite anchor point.
		/// </summary>
		public int X
		{
			get { return this.x; }
			set { this.x = value; }
		}

		/// <summary>
		/// Y-Offset to sprite anchor point.
		/// </summary>
		public int Y
		{
			get { return this.y; }
			set { this.y = value; }
		}

		/// <summary>
		/// Angle of the sprite.
		/// </summary>
		public double Angle
		{
			get { return this.angle; }
			set { this.angle = value; }
		}

		/// <summary>
		/// Sprite texture to display.
		/// </summary>
		public int SpriteTexture
		{
			get { return this.spriteTexture; }
			set { this.spriteTexture = value; }
		}

		/// <summary>
		/// Disposes of the sprite.
		/// </summary>
		public void Dispose()
		{
			RetroApplication.SpriteDisposed(this.node);
		}

		/// <summary>
		/// Moves the sprite on screen.
		/// </summary>
		/// <param name="DX">Number of pixels to to move the sprite along the x-axis.</param>
		/// <param name="DY">Number of pixels to to move the sprite along the y-axis.</param>
		public void Move(int DX, int DY)
		{
			this.x += DX;
			this.y += DY;
		}

		/// <summary>
		/// Sets the position of the sprite.
		/// </summary>
		/// <param name="X">New x-coordinate.</param>
		/// <param name="Y">New y-coordinate.</param>
		public void SetPosition(int X, int Y)
		{
			this.x = X;
			this.y = Y;
		}

		/// <summary>
		/// Sets the position of the sprite.
		/// </summary>
		/// <param name="NewPosition">New position.</param>
		public void SetPosition(Point NewPosition)
		{
			this.x = NewPosition.X;
			this.y = NewPosition.Y;
		}

		/// <summary>
		/// Moves the sprite on screen.
		/// </summary>
		/// <param name="DX">Number of pixels to to move the sprite along the x-axis.</param>
		/// <param name="DY">Number of pixels to to move the sprite along the y-axis.</param>
		/// <param name="NewSpriteTexture">New sprite texture for the sprite.</param>
		public void Move(int DX, int DY, int NewSpriteTexture)
		{
			this.x += DX;
			this.y += DY;
			this.spriteTexture = NewSpriteTexture;
		}

		/// <summary>
		/// Moves the sprite backwards in z-order one step.
		/// </summary>
		public void MoveBackward()
		{
			RetroApplication.MoveSpriteBackward(this.node);
		}

		/// <summary>
		/// Moves the sprite forwards in z-order one step.
		/// </summary>
		public void MoveForward()
		{
			RetroApplication.MoveSpriteForward(this.node);
		}

		/// <summary>
		/// Moves the sprite along the z-axis to the back.
		/// </summary>
		public void MoveBack()
		{
			RetroApplication.MoveSpriteBack(this.node);
		}

		/// <summary>
		/// Moves the sprite along the z-axis to the top.
		/// </summary>
		public void MoveFront()
		{
			RetroApplication.MoveSpriteFront(this.node);
		}
	}
}
