using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RetroSharp
{
    /// <summary>
    /// Defines the number of characters available in a character-based screen.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CharactersAttribute : RetroAttribute
    {
        private int width;
        private int height;
        private Color foregroundColor = C64Colors.LightBlue;
        private Color backgroundColor = C64Colors.Blue;

        /// <summary>
        /// Defines the number of characters available in a character-based screen.
        /// </summary>
        /// <param name="Width">Width of default screen, in tiles or characters.</param>
        /// <param name="Height">Height of default screen, in tiles or characters.</param>
        public CharactersAttribute(int Width, int Height)
        {
            this.width = AssertPositive(Width);
            this.height = AssertPositive(Height);
        }

        /// <summary>
        /// Defines the number of characters available in a character-based screen.
        /// </summary>
        /// <param name="Width">Width of default screen, in tiles or characters.</param>
        /// <param name="Height">Height of default screen, in tiles or characters.</param>
        /// <param name="ForegroundColor">Foreground color of characters.</param>
        public CharactersAttribute(int Width, int Height, KnownColor ForegroundColor)
        {
            this.width = AssertPositive(Width);
            this.height = AssertPositive(Height);
            this.foregroundColor = Color.FromKnownColor(ForegroundColor);
        }

        /// <summary>
        /// Defines the number of characters available in a character-based screen.
        /// </summary>
        /// <param name="Width">Width of default screen, in tiles or characters.</param>
        /// <param name="Height">Height of default screen, in tiles or characters.</param>
        /// <param name="ForegroundColor">Foreground color of characters.</param>
        /// <param name="ForegroundColor">Background color of characters.</param>
        public CharactersAttribute(int Width, int Height, KnownColor ForegroundColor, KnownColor BackgroundColor)
        {
            this.width = AssertPositive(Width);
            this.height = AssertPositive(Height);
            this.foregroundColor = Color.FromKnownColor(ForegroundColor);
            this.backgroundColor = Color.FromKnownColor(BackgroundColor);
        }

        /// <summary>
        /// Width of screen, in number of tiles or characters.
        /// </summary>
        public int Width
        {
            get { return this.width; }
        }

        /// <summary>
        /// Height of screen, in number of tiles or characters.
        /// </summary>
        public int Height
        {
            get { return this.height; }
        }

        /// <summary>
        /// Default Foreground Color of characters.
        /// </summary>
        public Color ForegroundColor
        {
            get { return this.foregroundColor; }
        }

        /// <summary>
        /// Default Background Color of characters.
        /// </summary>
        public Color BackgroundColor
        {
            get { return this.backgroundColor; }
        }
    }
}
