using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RetroSharp
{
    public delegate void MouseEventHandler(object Sender, MouseEventArgs e);

    /// <summary>
	/// Event arguments for mouse event handlers.
	/// </summary>
    public class MouseEventArgs : EventArgs
    {
		private Point position;
		private bool leftButton;
		private bool middleButton;
		private bool rightButton;

		/// <summary>
		/// Event arguments for mouse event handlers.
		/// </summary>
		/// <param name="Position">Mouse position</param>
		/// <param name="LeftButton">If the left mouse button is pressed or not.</param>
		/// <param name="MiddleButton">If the middle mouse button is pressed or not.</param>
		/// <param name="RightButton">If the right mouse button is pressed or not.</param>
		public MouseEventArgs(Point Position, bool LeftButton, bool MiddleButton, bool RightButton)
        {
			this.position = Position;
			this.leftButton = LeftButton;
			this.middleButton = MiddleButton;
			this.rightButton = RightButton;
        }

		/// <summary>
		/// Current mouse position.
		/// </summary>
		public Point Position
		{
			get { return this.position; }
		}

		/// <summary>
		/// Current mouse x-coordinate.
		/// </summary>
		public int X
		{
			get { return this.position.X; }
		}

		/// <summary>
		/// Current mouse y-coordinate.
		/// </summary>
		public int Y
		{
			get { return this.position.Y; }
		}

		/// <summary>
		/// If the the left mouse button is pressed or not.
		/// </summary>
		public bool LeftButton
		{
			get { return this.leftButton; }
		}

		/// <summary>
		/// If the the middle mouse button is pressed or not.
		/// </summary>
		public bool MiddleButton
		{
			get { return this.middleButton; }
		}

		/// <summary>
		/// If the the right mouse button is pressed or not.
		/// </summary>
		public bool RightButton
		{
			get { return this.rightButton; }
		}

	}
}
