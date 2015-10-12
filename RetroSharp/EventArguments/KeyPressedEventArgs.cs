using System;
using System.Collections.Generic;
using System.Text;

namespace RetroSharp
{
    public delegate void KeyPressedEventHandler(object Sender, KeyPressedEventArgs e);

    /// <summary>
    /// Event arguments for OnKeyPressed event handlers.
    /// </summary>
    public class KeyPressedEventArgs : EventArgs
    {
        private char character;
        private bool supressInput = false;

        /// <summary>
        /// Event arguments for OnKeyPressed event handlers.
        /// </summary>
        /// <param name="Character">Character pressed</param>
        public KeyPressedEventArgs(char Character)
        {
            this.character = Character;
        }

        /// <summary>
        /// Character pressed
        /// </summary>
        public char Character
        {
            get { return this.character; }
        }

        /// <summary>
        /// If input should be supressed. If set to true, the key will not be sent to the current input.
        /// </summary>
        public bool SupressInput
        {
            get { return this.supressInput; }
            set { this.supressInput = value; }
        }
    }
}
