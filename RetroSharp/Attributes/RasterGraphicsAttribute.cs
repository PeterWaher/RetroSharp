using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RetroSharp
{
    /// <summary>
    /// Defines the number of pixels available in a raster graphics-based screen.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RasterGraphicsAttribute : RetroAttribute
    {
        private int width;
        private int height;
        private Color backgroundColor = C64Colors.Black;

        /// <summary>
        /// Defines the number of pixels available in a raster graphics-based screen.
        /// </summary>
        /// <param name="Width">Width of default screen, in pixels.</param>
        /// <param name="Height">Height of default screen, in pixels.</param>
        public RasterGraphicsAttribute(int Width, int Height)
        {
            this.width = AssertPositive(Width);
            this.height = AssertPositive(Height);
        }

        /// <summary>
        /// Defines the number of pixels available in a raster graphics-based screen.
        /// </summary>
        /// <param name="Width">Width of default screen, in pixels.</param>
        /// <param name="Height">Height of default screen, in pixels.</param>
        /// <param name="BackgroundColor">Background color of screen.</param>
        public RasterGraphicsAttribute(int Width, int Height, KnownColor BackgroundColor)
        {
            this.width = AssertPositive(Width);
            this.height = AssertPositive(Height);
            this.backgroundColor = Color.FromKnownColor(BackgroundColor);
        }

        /// <summary>
        /// Width of screen, in pixels.
        /// </summary>
        public int Width
        {
            get { return this.width; }
        }

        /// <summary>
        /// Height of screen, in pixels.
        /// </summary>
        public int Height
        {
            get { return this.height; }
        }

        /// <summary>
        /// Default Background Color of screen.
        /// </summary>
        public Color BackgroundColor
        {
            get { return this.backgroundColor; }
        }
    }
}
