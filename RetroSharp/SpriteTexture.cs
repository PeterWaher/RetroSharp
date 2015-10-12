using System;
using System.Collections.Generic;
using System.Drawing;

namespace RetroSharp
{
    /// <summary>
    /// Definition of a sprite texture.
    /// </summary>
    public class SpriteTexture
    {
		private int handle;
		private Size size;
		private Point offset;

		/// <summary>
		/// Definition of a sprite texture.
		/// </summary>
		/// <param name="Handle">OpenGL Texture handle.</param>
		/// <param name="Size">Size of texture.</param>
		/// <param name="Offset">Offset to sprite anchor point.</param>
		internal SpriteTexture(int Handle, Size Size, Point Offset)
		{
			this.handle = Handle;
			this.size = Size;
			this.offset = Offset;
		}

		/// <summary>
		/// OpenGL Texture handle.
		/// </summary>
		public int Handle
		{
			get { return this.handle; }
			internal set { this.handle = value; }
		}

		/// <summary>
		/// Size of texture.
		/// </summary>
		public Size Size
		{
			get { return this.size; }
		}

		/// <summary>
		/// Width of sprite texture.
		/// </summary>
		public int Width
		{
			get { return this.size.Width; }
		}

		/// <summary>
		/// Height of sprite texture.
		/// </summary>
		public int Height
		{
			get { return this.size.Height; }
		}

		/// <summary>
		/// Offset to sprite anchor point.
		/// </summary>
		public Point Offset
		{
			get { return this.offset; }
		}

		/// <summary>
		/// X-Offset to sprite anchor point.
		/// </summary>
		public int X
		{
			get { return this.offset.X; }
		}

		/// <summary>
		/// Y-Offset to sprite anchor point.
		/// </summary>
		public int Y
		{
			get { return this.offset.Y; }
		}
	}
}
