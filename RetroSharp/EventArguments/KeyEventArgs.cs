using System;
using System.Collections.Generic;
using System.Text;

namespace RetroSharp
{
    public delegate void KeyEventHandler(object Sender, KeyEventArgs e);

    /// <summary>
    /// Event arguments for OnKeyDown and OnKeyUp event handlers.
    /// </summary>
    public class KeyEventArgs : EventArgs
    {
		private int scanCode;
		private Key key;
		private bool alt;
		private bool control;
		private bool shift;
		private bool isRepeat;

		/// <summary>
		/// Event arguments for OnKeyDown and OnKeyUp event handlers.
		/// </summary>
		/// <param name="ScanCode">Key scan code.</param>
		/// <param name="Key">Key</param>
		/// <param name="Alt">If the ALT key is pressed.</param>
		/// <param name="Control">If the CONTROL key is pressed.</param>
		/// <param name="Shift">If the SHIFT key is pressed.</param>
		/// <param name="IsRepeat">If the event is a repeated key event.</param>
		public KeyEventArgs(int ScanCode, Key Key, bool Alt, bool Control, bool Shift, bool IsRepeat)
        {
			this.scanCode = ScanCode;
			this.key = Key;
			this.alt = Alt;
			this.control = Control;
			this.shift = Shift;
			this.isRepeat = IsRepeat;
        }

		/// <summary>
		/// Key scan code.
		/// </summary>
		public int ScanCode
		{
			get { return this.scanCode; }
		}

		/// <summary>
		/// Key
		/// </summary>
		public Key Key
		{
			get { return this.key; }
		}

		/// <summary>
		/// if the ALT key is pressed.
		/// </summary>
		public bool Alt
		{
			get { return this.alt; }
		}

		/// <summary>
		/// if the CONTROL key is pressed.
		/// </summary>
		public bool Control
		{
			get { return this.control; }
		}

		/// <summary>
		/// if the SHIFT key is pressed.
		/// </summary>
		public bool Shift
		{
			get { return this.shift; }
		}

		/// <summary>
		/// If the event is a repeated key event.
		/// </summary>
		public bool IsRepeat
		{
			get { return this.isRepeat; }
		}
	}
}
