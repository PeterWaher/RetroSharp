using System;
using System.Collections.Generic;
using System.Text;

namespace RetroSharp
{
    /// <summary>
    /// Defines the default character set of a retro application.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CharacterSetAttribute : RetroAttribute
    {
        private string fontName;
        private int characterSetSize;

        /// <summary>
        /// Defines the default character set of a retro application.
        /// </summary>
        /// <param name="FontName">Default font name.</param>
        /// <param name="CharacterSetSize">Number of characters in the default character set.</param>
        public CharacterSetAttribute(string FontName, int CharacterSetSize)
        {
            this.fontName = FontName;
            this.characterSetSize = AssertPositive(CharacterSetSize);
        }

        /// <summary>
        /// Font Name
        /// </summary>
        public string FontName
        {
            get { return this.fontName; }
        }

        /// <summary>
        /// Number of characters in the character set.
        /// </summary>
        public int CharacterSetSize
        {
            get { return this.characterSetSize; }
        }
    }
}
